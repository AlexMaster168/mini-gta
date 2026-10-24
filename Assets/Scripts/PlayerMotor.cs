using UnityEngine;

namespace SunsetState
{
    public sealed partial class PlayerMotor : MonoBehaviour
    {
        public CharacterController Controller { get; private set; }
        public VehicleMotor Vehicle { get; private set; }
        public float Health = 100;
        public Transform Visual;
        float vertical;
        float shotCooldown;
        float embeddedTime;
        public Vector3 Position => Vehicle ? Vehicle.transform.position : transform.position;

        public void Initialize()
        {
            gameObject.layer = 2; // Ignore Raycast: aiming and camera casts must not hit the avatar.
            Controller = gameObject.AddComponent<CharacterController>();
            Controller.height = 1.8f; Controller.radius = .34f; Controller.center = Vector3.up * .9f;
            Controller.skinWidth=.035f;Controller.stepOffset=.4f;Controller.slopeLimit=48;Controller.minMoveDistance=0;
            Visual=CharacterModels.Create(transform,0,new Color(.22f,.55f,.44f));
        }
        public void SetColor(Color color) { if(Visual)Destroy(Visual.gameObject);Visual=CharacterModels.Create(transform,GameRoot.Instance.CasualClothes?0:GameRoot.Instance.RoleIndex,color,GameRoot.Instance.FemaleChoices[GameRoot.Instance.RoleIndex],GameRoot.Instance.SkinChoices[GameRoot.Instance.RoleIndex]);GameRoot.Instance.ApplyPlayerOutfit();if(Vehicle)Visual.gameObject.SetActive(false); }
        void Update()
        {
            var game = GameRoot.Instance;
            if (!game || !game.Playing || game.Panel != "") return;
            shotCooldown -= Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (Vehicle) Exit();
                else if (!game.TrySpecialAction())
                {
                    var nearest = game.NearestVehicle(Position, 5);
                    if (nearest) Enter(nearest);
                }
            }
            if (Vehicle){if(game.CanDriveBy&&Input.GetMouseButton(0)&&shotCooldown<=0){shotCooldown=game.FireInterval;game.Fire();}return;}
            if(Input.GetKeyDown(KeyCode.Backspace)){Teleport(Position);game.Notify("Персонаж перемещён на свободное место.");}
            var camera = Camera.main.transform;
            Vector3 forward = Vector3.ProjectOnPlane(camera.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(camera.right, Vector3.up).normalized;
            Vector3 move = Vector3.ClampMagnitude(forward * Input.GetAxisRaw("Vertical") + right * Input.GetAxisRaw("Horizontal"), 1);
            if (Controller.isGrounded) { vertical = -2; if (!BoardRiding && Input.GetKeyDown(KeyCode.Space)) vertical = 6; }
            vertical += Physics.gravity.y * Time.deltaTime;
            if(game.Swimming){var water=WaterBody.At(Position);vertical=Mathf.Clamp((water.Surface-(Position.y+1.25f))*3,-2,2)+(Input.GetKey(KeyCode.Space)?2:0);}
            Vector3 motion=BoardRiding?BoardMotion(move):move*(Input.GetKey(KeyCode.LeftShift)?9*game.RunMultiplier:5);
            Controller.Move((motion+Vector3.up*vertical)*Time.deltaTime);
            embeddedTime=SpawnSafety.ClearPlayer(transform.position)?0:embeddedTime+Time.deltaTime;
            if(embeddedTime>.4f){embeddedTime=0;Teleport(transform.position);}
            if (move.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), Time.deltaTime * 12);
            if (Input.GetMouseButton(0) && shotCooldown <= 0) { shotCooldown = game.FireInterval; game.Fire(); }
        }
        public void Enter(VehicleMotor vehicle)
        {
            vehicle.WakeSimulation();
            if(vehicle.Durability<=0){GameRoot.Instance.Notify("Транспорт уничтожен. Получи другой через H / G.");return;}
            if (vehicle.Driver) return;
            if (GameRoot.Instance.RoleIndex==13 && !GameRoot.Instance.Escaped) { GameRoot.Instance.Notify("Сначала нужно выбраться из тюрьмы."); return; }
            if (vehicle.Faction >= 0 && vehicle.Faction != GameRoot.Instance.RoleIndex) { GameRoot.Instance.Notify("Служебный транспорт: требуется роль «"+GameRoot.Instance.Roles[vehicle.Faction].Name+"»"); return; }
            StopBoard();Vehicle = vehicle; vehicle.Driver = this; vehicle.Traffic = false;
            Controller.enabled = false; Visual.gameObject.SetActive(false);
            GameRoot.Instance.Notify(vehicle.Kind==VehicleKind.Tank?"Танк · Z/X — башня · PageUp/Down — ствол · ПКМ — прицел · ЛКМ — огонь":vehicle.Flying ? "Вертолёт · W/S — вперёд/назад · A/D — поворот · Пробел — вверх · Ctrl — вниз" : "W — газ · S — тормоз / задний ход · A/D — руль · Пробел — ручник · V — перевернуть · E — выйти");
        }
        public void Exit()
        {
            if (!Vehicle) return;
            var car = Vehicle;
            if (car.Flying && car.transform.position.y > 4) { GameRoot.Instance.Notify("Сначала посади вертолёт: Ctrl — снижение."); return; }
            if(WaterBody.At(car.transform.position)!=null && !car.OnWater && car.transform.position.y<WaterBody.At(car.transform.position).Surface){car.Driver=null;Vehicle=null;Controller.enabled=false;transform.position=car.transform.position+car.transform.right*1.8f+Vector3.up*.7f;vertical=1;Visual.gameObject.SetActive(true);Controller.enabled=true;GameRoot.Instance.Notify("Плыви к берегу: WASD, Пробел — всплыть. F7 — спасение.");return;}
            if (car.OnWater) { car.Driver = null; Vehicle = null; Teleport(new Vector3(-936,2,200)); GameRoot.Instance.Notify("Ты вернулся на причал."); return; }
            Vector3 position = car.transform.position + car.transform.right * Mathf.Max(2,car.Dimensions.x*.5f+1);
            if(!SpawnSafety.TryPlayerPosition(position,out var safe) && !SpawnSafety.TryPlayerPosition(car.transform.position-car.transform.right*(car.Dimensions.x*.5f+1),out safe)){GameRoot.Instance.Notify("Рядом нет безопасного выхода. Отъедь от препятствия.");return;}
            car.Driver = null; Vehicle = null;
            transform.position = safe;vertical=0;
            Visual.gameObject.SetActive(true); Controller.enabled = true;
        }
        public void Teleport(Vector3 position)
        {
            StopBoard();GameRoot.Instance.ClearInteriorOnTeleport(position);
            if (Vehicle) { Vehicle.Driver = null; Vehicle = null; }
            Physics.SyncTransforms();if(!SpawnSafety.TryPlayerPosition(position,out var safe) && !SpawnSafety.TryPlayerPosition(transform.position,out safe)){GameRoot.Instance.Notify("Не найдено свободное место для перемещения.");return;}Controller.enabled=false;transform.position=safe;vertical=0;
            Visual.gameObject.SetActive(true); Controller.enabled = true;
        }
    }

    public sealed class FollowCamera : MonoBehaviour
    {
        public PlayerMotor Target;
        float yaw = 0, pitch = 19, distance = 7;
        float mouseIdle;VehicleMotor previousVehicle;
        public float Pitch=>pitch;
        public float Distance=>distance;
        public void Look(float horizontal,float vertical){float sensitivity=RuntimeSettings.Data.MouseSensitivity;yaw+=horizontal*2.3f*sensitivity;pitch=Mathf.Clamp(pitch-vertical*1.7f*sensitivity*(RuntimeSettings.Data.InvertY?-1:1),-75,80);}
        public void Zoom(float amount){distance=Mathf.Clamp(distance-amount,2,35);}
        void LateUpdate()
        {
            if (!Target) return;
            var game = GameRoot.Instance;
            if (game.Playing && game.Panel == "")
            {
                float mouseX=Input.GetAxis("Mouse X"),mouseY=Input.GetAxis("Mouse Y");
                mouseIdle=Mathf.Abs(mouseX)+Mathf.Abs(mouseY)>.04f?0:mouseIdle+Time.deltaTime;
                if(Target.Vehicle!=previousVehicle){previousVehicle=Target.Vehicle;if(previousVehicle){yaw=previousVehicle.transform.eulerAngles.y;mouseIdle=0;}}
                Look(mouseX,mouseY);
                if(Target.Vehicle && !Target.Vehicle.Flying && Target.Vehicle.Speed>12 && mouseIdle>1.2f && !Input.GetMouseButton(1) && !Input.GetMouseButton(0))
                {
                    float heading=Target.Vehicle.transform.eulerAngles.y;
                    if(Vector3.Dot(Target.Vehicle.Body.linearVelocity,Target.Vehicle.transform.forward)<-1)heading+=180;
                    yaw=Mathf.LerpAngle(yaw,heading,1-Mathf.Exp(-Time.deltaTime*2.1f));
                }
                bool tank=Target.Vehicle&&Target.Vehicle.Kind==VehicleKind.Tank;
                float keyZoom=(Input.GetKey(KeyCode.KeypadPlus)||!tank&&Input.GetKey(KeyCode.PageUp)?1:0)-(Input.GetKey(KeyCode.KeypadMinus)||!tank&&Input.GetKey(KeyCode.PageDown)?1:0);
                Zoom(Input.mouseScrollDelta.y+keyZoom*Time.unscaledDeltaTime*10);
            }
            var focus = Target.Position + Vector3.up * (Target.Vehicle?Mathf.Max(1.25f,Target.Vehicle.Dimensions.y*.6f):1.5f);
            // Shift the aim pivot ahead of the hero so the crosshair leads the view instead of sitting on the character's head.
            if(!Target.Vehicle)focus += Quaternion.Euler(0,yaw,0) * Vector3.forward * 1.7f;
            if(!Target.Vehicle && game.Armed && !game.ServiceSelected)focus+=Quaternion.Euler(0,yaw,0)*Vector3.right*.65f;
            if(Target.Vehicle && Target.Vehicle.Body)focus+=Vector3.ClampMagnitude(Target.Vehicle.Body.linearVelocity*.075f,2.5f);
            var direction = Quaternion.Euler(pitch, yaw, 0) * Vector3.back;
            float desired = Target.Vehicle ? distance + (Target.Vehicle.Flying?7:2) : distance;
            if(game.InInterior)desired=3.5f;
            if(Target.Vehicle)desired=Mathf.Max(desired,Target.Vehicle.Dimensions.z*.8f+3);
            foreach(var hit in Physics.SphereCastAll(focus,.25f,direction,desired,~(1<<2),QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(Target.transform) && (!Target.Vehicle || !hit.transform.IsChildOf(Target.Vehicle.transform)))desired=Mathf.Min(desired,Mathf.Max(.4f,hit.distance-.25f));
            var position=focus+direction*desired;
            transform.position=Vector3.Distance(transform.position,position)>35?position:Vector3.Lerp(transform.position,position,1-Mathf.Exp(-Time.unscaledDeltaTime*12));
            transform.LookAt(focus);
            if(game.Drunk)transform.Rotate(0,0,Mathf.Sin(Time.time*1.7f)*2.2f,Space.Self);
            GetComponent<Camera>().fieldOfView=Mathf.Lerp(GetComponent<Camera>().fieldOfView,Target.Vehicle?Mathf.Lerp(60,74,Target.Vehicle.Speed/200):60,Time.unscaledDeltaTime*3);
        }
    }
}
