using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        void NewTransportChecks()
        {
            SelectRole(0);Panel="";Player.Teleport(new Vector3(3300,2,-1200));
            SelectedBoard=PersonalBoard.Skateboard;Player.ToggleBoard();Require(Player.BoardRiding,"U must summon personal skateboard");Player.ToggleBoard();Require(!Player.BoardRiding,"U must stow personal skateboard");
            SelectedBoard=PersonalBoard.Snowboard;Player.ToggleBoard();Require(Player.BoardRiding,"Snowboard must be rideable");Player.Teleport(Player.Position);Require(!Player.BoardRiding,"Teleport must stow board");
            for(int role=1;role<=12;role++)
            {
                var fleet=FleetCatalog.ForRole(role);Require(System.Array.Exists(fleet,e=>e.Kind==VehicleKind.Bicycle),"Faction bicycle: "+role);Require(System.Array.Exists(fleet,e=>e.Kind==VehicleKind.Motorcycle),"Faction motorcycle: "+role);
            }
            var bike=WorldBuilder.SpawnCar(this,new Vector3(3300,2,-1200),"Bicycle check",Color.green,false,0,VehicleKind.Bicycle);
            Require(bike.Wheels.Length==2 && bike.Body.mass==110,"Bicycle needs two wheels and pedal vehicle mass");Require(Mathf.Abs(bike.Wheels[0].transform.localPosition.x)<.001f,"Bike wheels must align with frame");
            Player.Enter(bike);Require(Player.Vehicle==bike,"Personal bicycle must be accessible");Player.Teleport(new Vector3(3320,2,-1200));Cars.Remove(bike);Destroy(bike.gameObject);
            var tank=WorldBuilder.SpawnCar(this,new Vector3(3340,2,-1200),"Tank cannon check",Color.gray,false,0,VehicleKind.Tank);var gun=tank.GetComponent<TankWeapon>();gun.Aim(75,40);Require(Mathf.Abs(Mathf.DeltaAngle(gun.Turret.localEulerAngles.y,75))<.01f && gun.Elevation==30,"Tank turret rotation and elevation clamp");Require(gun.Fire() && !gun.Fire(),"Tank fires with reload cooldown");
            var victim=WorldBuilder.SpawnCar(this,new Vector3(3350,2,-1200),"Ram check",Color.white,false,0);float hp=Player.Health;tank.GetComponent<VehicleImpact>().Ram(victim,12);Require(victim.Durability<100 && Player.Health==hp,"Tank ram damages target vehicle without crash damage to player");
            var enemy=HostileAgent.Spawn(this,new Vector3(3360,2,-1200),"Run over check",false);tank.GetComponent<VehicleImpact>().HitPerson(enemy.gameObject,30);Require(enemy.Neutralized,"Tank must run over hostile targets");Enemies.Remove(enemy);Destroy(enemy.gameObject);
            Cars.Remove(victim);Destroy(victim.gameObject);Cars.Remove(tank);Destroy(tank.gameObject);
            Require(PatrolDrone.Active.Count>=3,"Police and FBI must deploy drones");
            var drone=PatrolDrone.Spawn(new Vector3(3400,20,-1200),2);drone.TakeDamage(110);Require(drone.Destroyed,"Patrol drone must be destructible");Destroy(drone.gameObject);
            SelectRole(14);BuildingEntrance dorm=null;foreach(var entry in InteriorSystem.Entrances)if(entry.Role==14){dorm=entry;break;}EnterBuilding(dorm);int wallet=Money;Player.Health=20;Armor=0;Contraband=0;magazines[4]=0;
            for(int item=0;item<4;item++)TakeHideoutSupplies(item);
            Require(Player.Health==100 && Armor==MaxArmor && magazines[4]==30 && Contraband==5 && Money==wallet,"Bandit dorm supplies must be free");Save();Contraband=0;SelectedBoard=PersonalBoard.None;Load();Require(Contraband==5 && SelectedBoard==PersonalBoard.Snowboard,"Contraband and accessory choice must survive save/load");
            ExitBuilding();SelectRole(14);Player.Teleport(new Vector3(3300,2,-1200));Wanted=4;nextResponse=0;UpdateWantedResponse();Require(ResponseOfficers==5 && responseCars.Count==2,"Wanted level four must deploy officers and FBI backup");Wanted=0;UpdateWantedResponse();Require(ResponseOfficers==0,"Response must clear after wanted level ends");
            SelectRole(0);Player.Teleport(Vector3.up*2);Debug.Log("SUNSET_BOARDS_BIKES_TANK_RAM_DRONES_HIDEOUT_WANTED_OK");
        }
    }
}
