using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        public bool CanDriveBy=>Player.Vehicle&&Player.Vehicle.Kind!=VehicleKind.Tank&&WeaponIndex>=0&&WeaponIndex<4&&CanUseWeapon(WeaponIndex)&&!ServiceSelected;
        Vector3 WeaponOrigin()
        {
            if(!Player.Vehicle)return Player.transform.position+Vector3.up*1.3f;
            var car=Player.Vehicle;float side=Vector3.Dot(Camera.main.transform.forward,car.transform.right)>=0?1:-1;
            return car.transform.position+Vector3.up*Mathf.Max(.9f,car.Dimensions.y*.62f)+car.transform.right*side*(car.Dimensions.x*.52f+.18f)+car.transform.forward*car.Dimensions.z*.1f;
        }
        bool WeaponRaycast(Vector3 origin,Vector3 direction,out RaycastHit closest,float range)
        {
            closest=default;bool found=false;float nearest=range;
            foreach(var hit in Physics.RaycastAll(origin,direction,range,~(1<<2),QueryTriggerInteraction.Ignore))
            {if(hit.transform.IsChildOf(Player.transform)||Player.Vehicle&&hit.transform.IsChildOf(Player.Vehicle.transform))continue;if(hit.distance<nearest){closest=hit;nearest=hit.distance;found=true;}}return found;
        }
        void CheckDriveBy()
        {
            SelectRole(1);Player.Teleport(new Vector3(3300,2,-1700));var car=WorldBuilder.SpawnCar(this,Player.Position+Vector3.right*8,"Drive-by check",Color.gray,false,0);Player.Enter(car);
            Physics.SyncTransforms();float initial=car.Durability;
            foreach(int weapon in new[]{0,1,2,3})
            {
                var target=HostileAgent.Spawn(this,car.transform.position+new Vector3(12,0,12),"Drive-by target",false);target.Health=1000;Physics.SyncTransforms();
                Camera.main.transform.position=car.transform.position+new Vector3(-3,3,-6);Camera.main.transform.LookAt(target.transform.position+Vector3.up);WeaponIndex=weapon;ServiceSelected=false;nextShot=0;int ammo=magazines[weapon];
                Require(CanDriveBy,"Compact weapon must allow shooting from vehicle");Fire();Require(target.Health<1000&&magazines[weapon]==ammo-1,"Drive-by shot must hit and consume ammunition: "+weapon+" hp="+target.Health+" ammo="+magazines[weapon]+" before="+ammo);Require(car.Durability==initial,"Drive-by shot must ignore own car colliders");
                Enemies.Remove(target);target.gameObject.SetActive(false);Destroy(target.gameObject);
            }
            WeaponIndex=3;magazines[3]=0;ReloadWeapon();reloadingUntil=Time.time-1;UpdateCombat();Require(magazines[3]==30,"Reload must work inside car");
            WeaponIndex=MinigunIndex;nextShot=0;int denied=magazines[MinigunIndex];Require(!CanDriveBy,"Minigun must be on foot only");Fire();Require(magazines[MinigunIndex]==denied,"Vehicle must block heavy weapon fire");
            var wall=WorldBuilder.Box("Drive-by cover",car.transform.position+Vector3.up+Vector3.forward*6,new Vector3(20,5,1),Color.gray);var hidden=HostileAgent.Spawn(this,car.transform.position+Vector3.forward*12,"Drive-by behind wall",false);Physics.SyncTransforms();Camera.main.transform.position=car.transform.position+new Vector3(-3,3,-6);Camera.main.transform.LookAt(hidden.transform.position+Vector3.up);WeaponIndex=0;nextShot=0;Fire();Require(hidden.Health==100,"Drive-by must not shoot through external cover");Enemies.Remove(hidden);Destroy(hidden.gameObject);Destroy(wall);
            Player.Teleport(car.transform.position+Vector3.left*8);Cars.Remove(car);Destroy(car.gameObject);SelectRole(0);Debug.Log("SUNSET_DRIVE_BY_COMPACT_GUNS_HITS_RELOAD_OWN_CAR_COVER_HEAVY_BLOCK_OK");
        }
    }
}
