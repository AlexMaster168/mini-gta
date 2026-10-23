using System;
using System.Collections;
using UnityEngine;

namespace SunsetState
{
    public sealed partial class GameRoot
    {
        IEnumerator Start()
        {
            if(PreviewMode){yield return CaptureGamePreviews();Application.Quit(0);yield break;}
            if(!SmokeMode)yield break;
            yield return null;
            bool success=true;
            yield return CheckStructureAndMissions(value=>success &= value);
            if(!success){Application.Quit(1);yield break;}
            SelectRole(0);StartGame(false);Panel="";
            Player.Teleport(new Vector3(200,3,0));
            yield return new WaitForSecondsRealtime(1);
            try{Require(Player.Controller.isGrounded,"Player must land on road");Require(Player.Health>0,"Player must remain alive");var avatarBounds=CharacterModels.GeometryBounds(Player.Visual.gameObject);Require(avatarBounds.size.y>1.4f && avatarBounds.size.y<2.2f,"Animated human must keep human scale after animation starts: "+avatarBounds.size);Require(Mathf.Abs(avatarBounds.min.y-Player.transform.position.y)<.35f,"Animated human feet must stay near ground");}catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);success=false;}
            var helicopter=WorldBuilder.SpawnCar(this,new Vector3(0,4,200),"Flight check",Color.white,false,0,VehicleKind.Helicopter);
            Player.Enter(helicopter);helicopter.Body.linearVelocity=Vector3.up*9;float initial=helicopter.transform.position.y;
            yield return new WaitForSecondsRealtime(.4f);
            try{Debug.Log("FLIGHT_CHECK initial="+initial+" now="+helicopter.transform.position.y+" body="+helicopter.Body.position.y+" driver="+(bool)helicopter.Driver);Require(!helicopter.Body.useGravity && helicopter.transform.position.y>initial+.4f,"Helicopter must lift and hover with pilot");}catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);success=false;}
            Player.Teleport(new Vector3(0,2,0));
            yield return CheckPhysicsRecovery(value=>success &= value);
            yield return CheckExtendedFeatures(value=>success &= value);
            yield return CheckNewSystems(value=>success &= value);
            yield return CheckWaterAndTraffic(value=>success &= value);
            yield return CheckReadability(value=>success &= value);
            yield return CheckBlackMarket(value=>success &= value);
            try{CheckDifficulty();CheckRadioLibrary();}catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);success=false;}
            yield return CheckDriving(value=>success &= value);
            Debug.Log(success?"SUNSET_SMOKE_OK: 18 roles, 180 jobs, 900 story chapters with branches, fleet permissions, prison escape, save, combat, services, helicopter, suspension, acceleration, braking, reverse, steering, handbrake":"SUNSET_SMOKE_FAILED");
            Application.Quit(success?0:1);
        }
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        IEnumerator CheckDriving(Action<bool> result)
        {
            var car=WorldBuilder.SpawnCar(this,new Vector3(3400,2,-900),"Driving check",Color.white,false,0,VehicleKind.Sedan);
            car.TestControls=Vector2.zero;Player.Teleport(new Vector3(3390,2,-900));
            yield return new WaitForSeconds(1.5f);
            try{Require(car.Wheels.Length==4 && car.Wheels[0].isGrounded && car.Wheels[3].isGrounded,"Suspension must contact ground");Require(car.transform.up.y>.95f,"Car must settle upright");}catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);result(false);}
            car.TestControls=new Vector2(0,1);yield return new WaitForSeconds(5);
            float speed=car.Speed;Debug.Log("DRIVE_ACCELERATION "+speed+" km/h");
            try{Require(speed>25 && speed<190,"Throttle must accelerate within sane range");Require(car.Gear>=1 && car.RPM>900,"Transmission must respond to speed");}catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);result(false);}
            car.TestControls=new Vector2(.6f,1);float yaw=car.transform.eulerAngles.y;yield return new WaitForSeconds(1);
            try{Require(Mathf.Abs(Mathf.DeltaAngle(yaw,car.transform.eulerAngles.y))>3,"Steering must turn the vehicle");Require(car.transform.up.y>.7f,"Turning must not roll vehicle");}catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);result(false);}
            car.TestControls=new Vector2(0,-1);yield return new WaitForSeconds(2);
            try{Require(car.Speed<speed*.85f,"S must brake a forward-moving vehicle");}catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);result(false);}
            yield return new WaitForSeconds(3);
            try{Require(Vector3.Dot(car.Body.linearVelocity,car.transform.forward)<-1,"S must engage reverse after stopping");}catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);result(false);}
            car.TestHandbrake=true;car.TestControls=Vector2.zero;float beforeHandbrake=car.Speed;yield return new WaitForSeconds(1);
            try{Require(car.Handbraking && car.Wheels[2].brakeTorque>1000 && car.Wheels[2].sidewaysFriction.stiffness<1,"Handbrake must lock rear wheels and reduce rear grip");Require(car.Speed<beforeHandbrake,"Handbrake must slow vehicle");}catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);result(false);}
            yield return new WaitForSeconds(3);
            try{Require(car.Speed<6,"Handbrake must stop vehicle");}catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);result(false);}
            car.TestControls=null;car.TestHandbrake=false;Cars.Remove(car);Destroy(car.gameObject);
            foreach(var kind in new[]{VehicleKind.Motorcycle,VehicleKind.Bicycle,VehicleKind.Supercar,VehicleKind.Electric,VehicleKind.Truck,VehicleKind.Sport,VehicleKind.SUV,VehicleKind.Tank})
            {
                var sample=WorldBuilder.SpawnCar(this,new Vector3(3400,2,-900),"Handling check",Color.white,false,0,kind);sample.TestControls=Vector2.zero;
                yield return new WaitForSeconds(1.3f);sample.TestControls=new Vector2(0,1);yield return new WaitForSeconds(2);
                try{if(kind==VehicleKind.Supercar||kind==VehicleKind.Electric)Require(sample.GetComponentInChildren<ModernCarStyle>()&&sample.GetComponentInChildren<ModernCarStyle>().DeformedMeshes>0,"Modern vehicle must have actual changed geometry");Require(sample.Speed>8 && sample.transform.up.y>.85f,"Vehicle class must accelerate upright: "+kind);if(sample.WheelVisuals!=null && sample.WheelVisuals[0]){Require(Quaternion.Angle(sample.WheelVisuals[0].rotation,sample.transform.rotation)>1,"Visible wheel must rotate: "+kind);}}catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);result(false);}
                Cars.Remove(sample);Destroy(sample.gameObject);
            }
            var crashCar=WorldBuilder.SpawnCar(this,new Vector3(3000,2,-1200),"Collision health check",Color.white,false,0);
            yield return new WaitForSeconds(1.5f);
            var wall=WorldBuilder.Box("Collision test wall",new Vector3(3000,2,-1190),new Vector3(20,4,1),Color.gray);
            Player.Enter(crashCar);float healthBefore=Player.Health,armorBefore=Armor;
            crashCar.Body.linearVelocity=Vector3.forward*22;
            yield return new WaitForSeconds(1.5f);
            try{Require(crashCar.Durability<100,"Collision test must actually hit an obstacle");Require(Player.Health==healthBefore && Armor==armorBefore,"Crashes must not damage player health or armor");}catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);result(false);}
            Player.Teleport(new Vector3(0,2,0));Cars.Remove(crashCar);Destroy(crashCar.gameObject);Destroy(wall);
        }
        IEnumerator CheckStructureAndMissions(Action<bool> result)
        {
            try{StructureAndMissionChecks();}
            catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);result(false);yield break;}
            // Physics colliders of freshly spawned agents register after one fixed step.
            yield return new WaitForFixedUpdate();
            yield return CombatAndServiceChecks();
            yield return CheckAutoAim(result);
            try{InteriorAndSafetyChecks();NewTransportChecks();InventoryChecks();}
            catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);result(false);yield break;}
            Debug.Log("SUNSET_STRUCTURE_CHECKS_OK");
        }
        void StructureAndMissionChecks()
        {
            Require(Roles.Length==18 && WorldBuilder.Places.Count==18 && StoryBook.Stories.Length==18,"Every role needs headquarters and story");
            Require(Cars.Count>70,"World must contain traffic and faction fleets");
            Require(StateBuilder.City(StateBuilder.SanFierro)=="САН-ФИЕРРО" && StateBuilder.City(StateBuilder.LasVenturas)=="ЛАС-ВЕНТУРАС","City locations");
            StartGame(false);
            for(int role=0;role<Roles.Length;role++)
            {
                RoleIndex=role;
                for(int v=0;v<JobsPerRole;v++)
                {
                    Mission mission=MakeMission(v);
                    Require(mission.Points.Length==mission.Steps.Length && mission.Reward>0,"Invalid mission definition: "+role+"/"+v);
                    Require(mission.Points.Length>=2,"Mission requires multiple stages");
                    foreach(var point in mission.Points)Require(WorldLimits.Contains(point),"Mission outside world");
                }
                for(int v=0;v<StoryBook.ChapterCount;v++)for(int choice=0;choice<2;choice++)
                    {
                        var storyMission=MakeStoryMission(role,v,choice);Require(storyMission.Points.Length==storyMission.Steps.Length,"Story mission stages");
                        Require(!string.IsNullOrEmpty(storyMission.Description),"Story needs narrative");
                    }
            }
            RoleIndex=1;var patrol=WorldBuilder.SpawnCar(this,new Vector3(0,2,0),"Permission check",Color.blue,false,0);patrol.Faction=2;
            Player.Enter(patrol);Require(!Player.Vehicle,"Wrong faction must be denied access");
            RoleIndex=2;Player.Enter(patrol);Require(Player.Vehicle==patrol,"Correct faction must have access");Player.Teleport(new Vector3(0,2,0));
            foreach(VehicleKind kind in Enum.GetValues(typeof(VehicleKind)))
            {
                var vehicle=WorldBuilder.SpawnCar(this,new Vector3(0,2,600),"Class check "+kind,Color.white,false,0,kind);
                Require(vehicle.Body && vehicle.GetComponentsInChildren<Collider>().Length>0,"Vehicle requires collision and physics: "+kind);
                Require(kind!=VehicleKind.Helicopter || vehicle.Rotor,"Helicopter requires rotor");
                Cars.Remove(vehicle);Destroy(vehicle.gameObject);
            }
            RoleIndex=0;int previous=Money;AcceptMission(0);
            int missionSteps=0;while(ActiveMission!=null && missionSteps++<30){Player.Teleport(ActiveMission.Points[Step]);hold=3.01f;UpdateMission();}
            Require(Money==previous+350 && Completed==1,"Mission must pay exactly once");
            SelectRole(13);Require(!Escaped,"Prison role starts confined");Player.Enter(patrol);Require(!Player.Vehicle,"Prisoner cannot use vehicles");
            AcceptMission(0);Step=2;OnPrisonMissionStep();Require(Escaped && Wanted==4 && Player.Position.x>1190,"Escape must move outside prison and trigger wanted level");
            StoryProgress[1]=2;StoryChoices[1]=1;CareerXP[1]=550;Armor=45;Money=4321;Save();Money=0;RoleIndex=0;Escaped=false;StoryProgress[1]=0;Load();Require(Money==4321 && RoleIndex==13 && Escaped && Wanted==4 && StoryProgress[1]==2 && StoryChoices[1]==1 && CareerXP[1]==550 && Mathf.Approximately(Armor,45),"Save round trip including story, rank and armor");CancelMission();Wanted=0;
        }
        IEnumerator CombatAndServiceChecks()
        {
            for(int role=0;role<Roles.Length;role++)
            {
                SelectRole(role);Player.Health=65;int moneyBefore=Money;personalMedkitReadyAt=0;UsePersonalMedkit();
                var human=Player.Visual.GetComponentInChildren<SkinnedMeshRenderer>();Require(human && human.bones.Length>15,"Every role must have a rigged human model: "+role);
                var humanBounds=CharacterModels.GeometryBounds(Player.Visual.gameObject);Require(humanBounds.size.y>1.4f && humanBounds.size.y<2.2f,"Every role must have human proportions: "+role+" "+humanBounds.size);
                Require(Player.Health==95 && Money==moneyBefore,"Personal medkit must be free for every role: "+role);
                personalMedkitReadyAt=0;UsePersonalMedkit();Require(Player.Health==100,"Personal medkit must cap health at 100");
                personalMedkitReadyAt=0;UsePersonalMedkit();Require(Player.Health==100 && Money==moneyBefore,"Full-health medkit must not charge money or over-heal");
            }
            SelectRole(1);Player.Health=100;Armor=100;DamagePlayer(40);Require(Mathf.Approximately(Armor,70) && Mathf.Approximately(Player.Health,90),"Armor absorbs 75 percent");
            Player.Teleport(new Vector3(3000,2,-1000));var enemy=HostileAgent.Spawn(this,Player.Position+Vector3.forward*8,"Combat check",false);Physics.SyncTransforms();
            // A freshly spawned CharacterController registers its collision shape on the next simulation step.
            yield return new WaitForFixedUpdate();
            Camera.main.transform.position=Player.Position+new Vector3(0,1.3f,-7);Camera.main.transform.LookAt(enemy.transform.position+Vector3.up*1.1f);
            WeaponIndex=4;ServiceSelected=false;nextShot=0;int ammo=magazines[4];Fire();
            if(enemy.Health>=100 || magazines[4]!=ammo-1)
            {
                var cc=enemy.GetComponent<CharacterController>();
                Vector3 origin=Camera.main.transform.position;
                Vector3 center=cc.bounds.center;
                Debug.Log("SHOT_FAIL cam="+origin.ToString("F2")+" dir="+Camera.main.transform.forward.ToString("F2")+" player="+Player.transform.position+" ccCenter="+center+" ccExtents="+cc.bounds.extents);
                foreach(var hit in Physics.RaycastAll(origin,(center-origin).normalized,60,~0))Debug.Log("SHOT_FAIL_ALLDIR "+hit.collider.name+" layer="+hit.collider.gameObject.layer+" "+hit.distance.ToString("F2"));
                Debug.Log("SHOT_FAIL_OVERLAP "+Physics.OverlapBox(center,cc.bounds.extents,Quaternion.identity,~0,QueryTriggerInteraction.Collide).Length);
                Physics.SyncTransforms();
                foreach(var hit in Physics.RaycastAll(origin,(center-origin).normalized,60,~0))Debug.Log("SHOT_FAIL_SYNCED "+hit.collider.name+" "+hit.distance.ToString("F2"));
            }
            Require(enemy.Health<100 && magazines[4]==ammo-1,"M4 hits a target and consumes ammunition");
            WeaponIndex=7;nextShot=0;Fire();Require(enemy.Stunned && enemy.Downed && enemy.StunnedUntil-Time.time>11.9f,"Taser immediately knocks target down without killing");
            Player.Teleport(enemy.transform.position-Vector3.forward*2);Require(TryUseCuffs() && enemy.Neutralized,"Cuffs arrest a stunned enemy");Enemies.Remove(enemy);Destroy(enemy.gameObject);
            var batonTarget=HostileAgent.Spawn(this,Player.Position+Player.transform.forward*2,"Baton check",false);UseBaton();Require(batonTarget.Downed && batonTarget.Stunned,"Baton immediately knocks target to ground");Enemies.Remove(batonTarget);Destroy(batonTarget.gameObject);
            var recoveryTarget=HostileAgent.Spawn(this,Player.Position+Vector3.right*5,"Recovery check",false);recoveryTarget.Stun(8);Require(recoveryTarget.Downed && recoveryTarget.StunnedUntil-Time.time>7.9f,"Baton duration must be eight seconds");recoveryTarget.StunnedUntil=Time.time-1;
            yield return null;Require(!recoveryTarget.Downed,"Target must stand up after stun expires");Enemies.Remove(recoveryTarget);Destroy(recoveryTarget.gameObject);
            for(int weapon=10;weapon<Weapons.Length;weapon++)
            {
                Player.Teleport(new Vector3(3100,2,-1000));var rifleTarget=HostileAgent.Spawn(this,Player.Position+Vector3.forward*9,"Rifle hit check",false);yield return new WaitForFixedUpdate();
                Camera.main.transform.position=Player.Position+new Vector3(0,1.3f,-7);Camera.main.transform.LookAt(rifleTarget.transform.position+Vector3.up*1.1f);WeaponIndex=weapon;nextShot=0;int rounds=magazines[weapon];Fire();Require(rifleTarget.Health<100 && magazines[weapon]==rounds-1,"New rifle must hit and consume ammo: "+weapon);
                Enemies.Remove(rifleTarget);Destroy(rifleTarget.gameObject);magazines[weapon]=0;ReloadWeapon();reloadingUntil=Time.time-1;UpdateCombat();Require(magazines[weapon]==Weapons[weapon].Capacity,"New rifle reload: "+weapon);
            }
            WeaponIndex=4;magazines[4]=0;ReloadWeapon();Require(reloading,"Reload starts");reloadingUntil=Time.time-1;UpdateCombat();Require(magazines[4]==30 && !reloading,"Reload transfers ammunition");
            CareerXP[1]=190;AwardXP(10);Require(RankFor(1)==1 && MissionPay(1000)==1100,"Promotion affects mission salary");
            SelectRole(4);AcceptMission(0);Player.Teleport(serviceSubject.transform.position+Vector3.right*2);ToolIndex=2;toolCooldown=0;UseServiceTool();Require(serviceHealth==100,"Paramedic stabilizes patient");
            ToolIndex=1;toolCooldown=0;UseServiceTool();Require(patientCarried && serviceDone,"Stretcher picks up patient and unlocks delivery");CancelMission();
            SelectRole(5);AcceptMission(0);Player.Teleport(serviceSubject.transform.position+Vector3.right*7);ToolIndex=0;
            for(int i=0;i<3;i++){toolCooldown=0;UseServiceTool();}Require(fireStrength==0 && serviceDone,"Extinguisher puts out mission fire");CancelMission();
            SelectRole(8);AcceptMission(0);Player.Teleport(serviceSubject.transform.position+Vector3.right*3);ToolIndex=0;toolCooldown=0;UseServiceTool();Require(serviceDone && serviceSubject.GetComponent<VehicleMotor>().Durability==100,"Mechanic repairs damaged vehicle");CancelMission();
            SelectRole(0);StoryProgress[0]=0;BeginStory(0);int missionSteps=0;while(ActiveMission!=null && missionSteps++<30){Player.Teleport(ActiveMission.Points[Step]);hold=3.01f;UpdateMission();}
            Require(StoryProgress[0]==1,"Story completion advances only selected role");SelectRole(1);Require(StoryProgress[0]==1,"Changing role preserves story progress");
            DutyChecks();
        }
        void DutyChecks()
        {
            for(int role=0;role<Roles.Length;role++)
            {
                if(role==13)continue;
                RoleIndex=role;
                for(int v=0;v<JobsPerRole;v++)
                {
                    var mission=MakeMission(v);
                    Require(mission!=null && mission.Points.Length==mission.Steps.Length && mission.Reward>0,"Duty mission consistency: "+role+"/"+v);
                    Require(mission.Points.Length>=2,"Duty mission needs stages: "+role+"/"+v);
                }
            }
            RoleIndex=1;
            Require(InterrogationTruth(918273645)==Mathf.Abs(918273645)%3,"Interrogation truth indexed deterministically");
            StartCall();Require(Call!=null && !string.IsNullOrEmpty(Call.Code) && Call.Reward>0,"Dispatch creates a coded call");EndCall(false);Require(Call==null,"Dispatch call cleared");
            SelectRole(11);var ped=new GameObject("Тестовый клиент");ped.transform.position=Player.Position+Player.transform.forward*4;ped.AddComponent<Pedestrian>();
            client=ped.GetComponent<Pedestrian>();int wallet=Money;SellLicense(0,true);Require(Money==wallet+250 && client==null,"License sale pays out");Destroy(ped);
            SelectRole(1);var suspect=HostileAgent.Spawn(this,Player.Position+Vector3.forward*3+Vector3.up,"Jail test",false);
            transportSuspect=suspect.gameObject;int custody=Money;
            Player.Teleport(new Vector3(930,2,-400));UpdateTransport();
            Require(Money==custody+600 && transportSuspect==null,"Delivering a suspect to prison must pay and clear");
            Enemies.Remove(suspect);
            SelectRole(3);StartCall();Require(Call!=null&&Call.Code=="10-23"&&Call.Wave==1&&Call.Threats.Count==5,"Siege starts with wave one");
            NextSiegeWave();Require(Call!=null&&Call.Wave==2&&Call.Threats.Count==7,"Second wave escalates");EndCall(false);
            Require(BaseNPC.Scientists>=4 && BaseNPC.Medics>=2,"Army base must deploy scientists and medics");
            Player.Teleport(new Vector3(0,2,0));
        }
        void InteriorAndSafetyChecks()
        {
            Require(InteriorSystem.Entrances.Count>500,"City buildings must have accessible interiors");
            for(int role=0;role<Roles.Length;role++)
            {
                SelectRole(role);BuildingEntrance headquarters=null;foreach(var entry in InteriorSystem.Entrances)if(entry.Role==role){headquarters=entry;break;}
                Require(headquarters!=null,"Every faction needs headquarters entrance: "+role);EnterBuilding(headquarters);Physics.SyncTransforms();
                Require(InInterior && Player.Position.y>590 && SpawnSafety.ClearPlayer(Player.Position),"Interior spawn must be clear: "+role);
                Require(Vector3.Distance(SavePosition,headquarters.Door)<.1f,"Saving inside must retain exterior return position");
                Player.Teleport(interiorOrigin+new Vector3(-8,1,2));TryInteriorAction();Require(CasualClothes,"Locker must allow civilian clothes: "+role);TryInteriorAction();Require(!CasualClothes,"Locker must equip work uniform: "+role);
                Player.Teleport(interiorOrigin+new Vector3(8,1,2));TryInteriorAction();Require(Panel=="jobs","Faction interior must open story board: "+role);Panel="";Time.timeScale=1;
                ExitBuilding();Require(!InInterior && Player.Position.y<10 && SpawnSafety.ClearPlayer(Player.Position),"Interior exit must be safe: "+role);
            }
            SelectRole(2);Require(Armor==150 && WeaponIndex==4,"FBI must start with elite armor and M4");SelectRole(0);
            foreach(var viewer in new[]{Vector3.forward*5,Vector3.back*5,Vector3.left*5,Vector3.right*5}){var rotation=WorldTextFacing.ReadableRotation(Vector3.zero,viewer);Require(Vector3.Dot(rotation*Vector3.back,viewer.normalized)>.99f,"World text must show front face to viewer");}
            var obstacle=WorldBuilder.Box("Embedding test",new Vector3(3200,2,-1300),new Vector3(10,4,10),Color.gray);Physics.SyncTransforms();Player.Teleport(new Vector3(3200,1,-1300));Require(SpawnSafety.ClearPlayer(Player.Position),"Player must recover from placement inside obstacle");Destroy(obstacle);Player.Teleport(new Vector3(0,2,0));
            Debug.Log("SUNSET_INTERIORS_UNIFORMS_TEXT_SAFETY_OK");
        }
    }
}
