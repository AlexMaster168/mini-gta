using UnityEngine;
namespace SunsetState
{
    public sealed class VehicleRecovery:MonoBehaviour
    {
        VehicleMotor car;Vector3 lastSafe;bool hasSafe;float nextCheck,blockedTime,unsupportedTime;
        public int Recoveries {get;private set;}
        void Awake(){car=GetComponent<VehicleMotor>();}
        public bool Embedded()
        {
            foreach(var shape in car.GetComponentsInChildren<BoxCollider>())
            {
                if(!shape.enabled || shape.isTrigger || shape.attachedRigidbody!=car.Body)continue;
                var bounds=shape.bounds;
                foreach(var other in Physics.OverlapBox(bounds.center,bounds.extents+Vector3.one*.02f,Quaternion.identity,~(1<<2),QueryTriggerInteraction.Ignore))
                {
                    if(other.transform.IsChildOf(transform) || other.attachedRigidbody || other is CharacterController)continue;
                    if(Physics.ComputePenetration(shape,shape.transform.position,shape.transform.rotation,other,other.transform.position,other.transform.rotation,out var direction,out float depth) && depth>.16f && (Mathf.Abs(direction.y)<.8f || depth>.6f))return true;
                }
            }
            return false;
        }
        public void ValidateSpawn()
        {
            if(!car)car=GetComponent<VehicleMotor>();if(car.OnWater)return;
            Vector3 requested=transform.position;requested.y=requested.y>500?600.5f:Mathf.Min(2,requested.y);
            var safe=SpawnSafety.VehiclePosition(requested,car.Dimensions,transform.eulerAngles.y,car);
            car.transform.position=safe+Vector3.up*.4f;car.Body.position=car.transform.position;car.Body.linearVelocity=Vector3.zero;car.Body.angularVelocity=Vector3.zero;lastSafe=safe;hasSafe=true;nextCheck=Time.time+2;
        }
        public bool Recover()
        {
            if(WaterBody.At(transform.position)!=null&&transform.position.y<WaterBody.At(transform.position).Surface){if(car.Driver)GameRoot.Instance.Notify("Транспорт в воде. F7 — эвакуация.");return false;}
            if(car.OnWater || car.Flying && car.Driver && transform.position.y>5)return false;
            Vector3 requested=hasSafe && Vector3.Distance(lastSafe,transform.position)<120?lastSafe:transform.position;
            if(!hasSafe)requested.y=requested.y>500?600.5f:2;
            float yaw=transform.eulerAngles.y;
            if(!SpawnSafety.TryVehiclePosition(requested,car.Dimensions,yaw,out var safe,car))return false;
            car.transform.SetPositionAndRotation(safe+Vector3.up*.4f,Quaternion.Euler(0,yaw,0));car.Body.position=car.transform.position;car.Body.rotation=car.transform.rotation;car.Body.linearVelocity=Vector3.zero;car.Body.angularVelocity=Vector3.zero;blockedTime=unsupportedTime=0;lastSafe=safe;hasSafe=true;Recoveries++;Physics.SyncTransforms();
            if(car.Driver)GameRoot.Instance.Notify("Транспорт восстановлен на свободном участке.");return true;
        }
        void FixedUpdate()
        {
            var game=GameRoot.Instance;if(!game || !game.Playing || game.Panel!="" || WaterBody.At(transform.position)!=null || car.OnWater || car.Flying && car.Driver || Time.time<nextCheck)return;
            float interval=car.Driver || Vector3.Distance(transform.position,game.Player.Position)<200?.4f:2;nextCheck=Time.time+interval;
            bool embedded=Embedded();blockedTime=embedded?blockedTime+interval:0;
            bool supported=car.Wheels==null; if(car.Wheels!=null)foreach(var wheel in car.Wheels)if(wheel.isGrounded){supported=true;break;}
            unsupportedTime=!supported && car.Speed<3?unsupportedTime+interval:0;
            if(blockedTime>=.7f || unsupportedTime>=4 || transform.position.y<-.7f || !WorldLimits.Contains(transform.position)){Recover();return;}
            if(!embedded && supported && transform.up.y>.9f && SpawnSafety.VehicleSpace(transform.position,car.Dimensions,Quaternion.Euler(0,transform.eulerAngles.y,0),car))
            {lastSafe=transform.position;hasSafe=true;}
        }
    }
}
