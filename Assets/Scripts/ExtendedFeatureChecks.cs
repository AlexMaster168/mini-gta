using System;
using System.Collections;
using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        IEnumerator CheckExtendedFeatures(Action<bool> result)
        {
            try
            {
                for(int role=0;role<Roles.Length;role++)
                {
                    SelectRole(role);FemaleChoices[role]=true;SkinChoices[role]=5;Player.SetColor(Roles[role].Color);
                    var identity=Player.Visual.GetComponent<CharacterAppearance>();Require(identity && identity.Female && identity.AssetKey.Contains("Female") && Player.Visual.GetComponentInChildren<SkinnedMeshRenderer>(),"Female role must use an animated female asset: "+role);
                    Require(CharacterModels.GeometryBounds(Player.Visual.gameObject).size.y>1.4f,"Female model must keep human size");
                }
                Require(Accessories.Length>=28,"Inventory must offer at least twenty-seven accessories");
                Require(FactionDistricts.Features.Exists(x=>x.Name=="Казарма") && FactionDistricts.Features.Exists(x=>x.Name=="Колонна мэрии") && FactionDistricts.Features.Exists(x=>x.Name=="Спортивный двор"),"Army, city hall and prison require distinctive grounds");
                var route=RoadNavigation.Route(Vector3.zero,StateBuilder.SanFierro);Require(route.Count>4 && Vector3.Distance(route[route.Count-1],StateBuilder.SanFierro)<1,"Map route must use intercity road nodes");
                SelectRole(0);BuildingEntrance shop=null;foreach(var entry in InteriorSystem.Entrances)if(entry.ShopId==0){shop=entry;break;}Require(shop!=null,"Shop entrance required");EnterBuilding(shop);Money=10000;OwnedGunMask=0;Purchases=new int[6];
                Require(BuyGood(6) && Money==9200 && Armed && CanUseWeapon(0) && !CanUseWeapon(4),"Civilian purchase must unlock only bought gun and charge price");int wallet=Money;Require(!BuyGood(6) && Money==wallet,"Duplicate gun purchase must not charge");
                Require(BuyGood(0) && BuyGood(2) && Purchases[0]==1 && Purchases[2]==1,"Store must sell provisions and alcohol");Player.Health=50;UsePurchase(0);Require(Player.Health==70 && Purchases[0]==0,"Food purchase must heal and consume item");
                UsePurchase(2);Require(Drunk && Purchases[2]==0,"Alcohol purchase must have temporary effect");drunkUntil=0;
                BuyGood(3);Save();OwnedGunMask=0;Purchases=new int[6];Load();Require(OwnedGunMask==1 && Purchases[3]==1 && FemaleChoices[0] && SkinChoices[0]==5,"Purchases, owned guns, gender and outfit must persist");ExitBuilding();
            }
            catch(Exception exception){Debug.LogError("SUNSET_SMOKE_FAILED "+exception);result(false);}
            try
            {
                SelectRole(11);BuildingEntrance center=null;foreach(var entry in InteriorSystem.Entrances)if(entry.Role==11){center=entry;break;}EnterBuilding(center);Money=5000;PersonalLicenses=0;
                Require(PurchaseLicense(0) && PersonalLicenses==1 && Money==4750,"License center must issue paid personal license");int wallet=Money;Require(!PurchaseLicense(0) && Money==wallet,"License must not be charged twice");ExitBuilding();
                StartRoadServiceJob();Require(roadRepairTarget && ActiveMission!=null,"Licenser must receive real road repair job");Player.Teleport(roadRepairTarget.transform.position+Vector3.right*2);ToolIndex=1;toolCooldown=0;UseServiceTool();Require(!roadRepairTarget.Broken,"Licenser tool must repair road sign or road patch");CancelMission();
                foreach(int role in new[]{1,2,3,4,5,6,10,11,12}){SelectRole(role);Require(System.Array.Exists(Player.Visual.GetComponentsInChildren<TextMesh>(),x=>x.text.Contains(UniformDetails.Codes[role])),"Working uniform must visibly identify faction: "+role);}
            }
            catch(Exception exception){Debug.LogError("SUNSET_SMOKE_FAILED "+exception);result(false);}
            SelectRole(1);Panel="";Player.Teleport(new Vector3(4100,2,-1800));
            foreach(int kind in new[]{0,1,2,3})
            {
                GameObject actor;
                if(kind==0){actor=HostileAgent.Spawn(this,Player.Position+Vector3.forward*38,"Long range taser target",false).gameObject;}
                else
                {
                    actor=new GameObject("Civilian taser target");actor.transform.position=Player.Position+Vector3.forward*38;
                    if(kind==1)actor.AddComponent<Pedestrian>();else if(kind==2)actor.AddComponent<BaseNPC>();else actor.AddComponent<NeutralActor>();
                    if(kind==3){var capsule=actor.AddComponent<CapsuleCollider>();capsule.height=1.8f;capsule.radius=.3f;capsule.center=Vector3.up*.9f;}
                    else actor.AddComponent<NpcGroundMotor>().Initialize();CharacterModels.Create(actor.transform,0,Color.gray);
                }
                yield return new WaitForFixedUpdate();
                var camera=Camera.main;var follow=camera.GetComponent<FollowCamera>();follow.enabled=false;camera.transform.position=Player.Position+new Vector3(0,1.3f,-4);camera.transform.LookAt(actor.transform.position+Vector3.up*1.05f);
                try
                {
                    WeaponIndex=7;ServiceSelected=false;nextShot=0;magazines[7]=1;Fire();
                    var enemy=actor.GetComponent<HostileAgent>();var fall=actor.GetComponent<CivilianKnockdown>();Require(enemy?enemy.Downed && enemy.Health==100 && enemy.StunnedUntil-Time.time>11.8f:fall && fall.Until-Time.time>11.8f,"Taser must stun every NPC category at thirty-eight metres: "+kind);
                    Player.Teleport(actor.transform.position-Vector3.forward*2);UseBaton();Require(enemy?enemy.Downed:actor.GetComponent<CivilianKnockdown>(),"Baton must knock down any NPC category");
                }
                catch(Exception exception){Debug.LogError("SUNSET_SMOKE_FAILED "+exception);result(false);}
                follow.enabled=true;var hostile=actor.GetComponent<HostileAgent>();if(hostile)Enemies.Remove(hostile);Destroy(actor);Player.Teleport(new Vector3(4100,2,-1800));
            }
            foreach(VehicleKind kind in Enum.GetValues(typeof(VehicleKind)))
            {
                var vehicle=WorldBuilder.SpawnCar(this,new Vector3(4150,2,-1800),"Universal collision check",Color.gray,false,0,kind);
                var victim=HostileAgent.Spawn(this,new Vector3(4200,1,-1800),"Run over target",false);
                try{vehicle.GetComponent<VehicleImpact>().HitPerson(victim.gameObject,12);Require(victim.Health<100 && victim.Downed,"Every transport must knock down actors on impact: "+kind);}catch(Exception exception){Debug.LogError("SUNSET_SMOKE_FAILED "+exception);result(false);}
                Enemies.Remove(victim);Destroy(victim.gameObject);Cars.Remove(vehicle);Destroy(vehicle.gameObject);
            }
            foreach(int faction in new[]{1,2,4,5})foreach(var kind in new[]{VehicleKind.Sedan,VehicleKind.Motorcycle,VehicleKind.Helicopter,VehicleKind.Bicycle})
            {
                var vehicle=WorldBuilder.SpawnCar(this,new Vector3(4150,2,-1800),"Emergency light check",Color.gray,false,0,kind);WorldBuilder.AddServiceDetails(vehicle,faction);
                try{var rig=vehicle.GetComponent<EmergencyLights>();Require(rig && rig.LampCount==4,"Emergency fleet needs strobes including bikes and air");vehicle.SetEmergency(true);rig.UpdatePattern(0);Require(vehicle.Beacons[0].enabled && !vehicle.Beacons[1].enabled,"First strobe phase");rig.UpdatePattern(4f/12);Require(!vehicle.Beacons[0].enabled && vehicle.Beacons[1].enabled,"Alternating strobe phase");vehicle.SetEmergency(false);rig.UpdatePattern(0);Require(!vehicle.Beacons[0].enabled,"Strobes switch off");}catch(Exception exception){Debug.LogError("SUNSET_SMOKE_FAILED "+exception);result(false);}Cars.Remove(vehicle);Destroy(vehicle.gameObject);
            }
            var bulletCar=WorldBuilder.SpawnCar(this,new Vector3(4220,2,-1800),"Bullet damage regression",Color.gray,false,0);HostileAgent.AttachToPatrol(this,bulletCar);
            Player.Teleport(new Vector3(4220,2,-1812));var aimCamera=Camera.main;var aimFollow=aimCamera.GetComponent<FollowCamera>();aimFollow.enabled=false;yield return new WaitForFixedUpdate();aimCamera.transform.position=Player.Position+new Vector3(0,1.3f,-4);aimCamera.transform.LookAt(bulletCar.transform.position+Vector3.up*1.05f);
            try{WeaponIndex=4;ServiceSelected=false;nextShot=0;magazines[4]=30;Fire();Require(bulletCar.Durability<bulletCar.MaxDurability,"Shooting a patrol vehicle must damage vehicle, not only its occupant");bulletCar.Durability=10;}
            catch(Exception exception){Debug.LogError("SUNSET_SMOKE_FAILED "+exception);result(false);}
            yield return null;
            try{Require(bulletCar.GetComponent<VehicleDestruction>().Smoking && bulletCar.GetComponent<VehicleDestruction>().Burning,"Critically damaged vehicle must smoke and burn before explosion");}
            catch(Exception exception){Debug.LogError("SUNSET_SMOKE_FAILED "+exception);result(false);}
            aimFollow.enabled=true;var patrol=bulletCar.GetComponent<HostileAgent>();Enemies.Remove(patrol);Cars.Remove(bulletCar);Destroy(bulletCar.gameObject);Player.Teleport(new Vector3(4100,2,-1800));
            var first=WorldBuilder.SpawnCar(this,new Vector3(4250,2,-1800),"Explosion source",Color.gray,false,0);var second=WorldBuilder.SpawnCar(this,new Vector3(4255,2,-1800),"Chain explosion",Color.gray,false,0);second.Durability=20;
            var cover=WorldBuilder.Box("Outpost cover",new Vector3(4250,1,-1795),new Vector3(3,2,1),Color.gray);Physics.SyncTransforms();first.Durability=0;
            yield return null;yield return null;
            try
            {
                Require(first.GetComponent<VehicleDestruction>().Destroyed && second.GetComponent<VehicleDestruction>().Destroyed,"Destroyed vehicles must explode and cause nearby chain explosion");
                TankWeapon.Blast(cover.transform.position,null);Require(cover.GetComponent<WorldDestructible>().Destroyed && !cover.GetComponent<Collider>().enabled,"Tank explosion must destroy cover collision");
            }
            catch(Exception exception){Debug.LogError("SUNSET_SMOKE_FAILED "+exception);result(false);}
            Cars.Remove(first);Cars.Remove(second);Destroy(first.gameObject);Destroy(second.gameObject);Destroy(cover);OwnedGunMask=0;Purchases=new int[6];LoadOutfits(null,null,null);SelectRole(0);Debug.Log("SUNSET_SHOPS_FEMALE_MODELS_STUN_ALL_RUNOVER_EXPLOSIONS_STROBES_MAP_OK");
        }
    }
}
