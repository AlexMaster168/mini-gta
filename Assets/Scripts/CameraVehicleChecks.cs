using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        IEnumerator CheckCameraVehicles(Action<bool> result)
        {
            try
            {
                var follow=Camera.main.GetComponent<FollowCamera>();follow.Look(0,1000);Require(follow.Pitch==-75,"Camera must look upward past horizon");follow.Look(0,-1000);Require(follow.Pitch==80,"Camera must look downward");follow.Zoom(1000);Require(follow.Distance==2,"Camera near zoom limit");follow.Zoom(-1000);Require(follow.Distance==35,"Camera far zoom limit");follow.Zoom(28);follow.Look(0,(80-19)/1.7f);
                Require(CameraClearSmoke.Visibility(Vector3.forward,Vector3.zero,Vector3.forward*10)==0,"Smoke next to camera must disappear");Require(CameraClearSmoke.Visibility(Vector3.forward*7,Vector3.zero,Vector3.forward*10)<.1f,"Smoke between camera and player must fade");
                SelectRole(0);Player.Teleport(new Vector3(3300,2,-1900));
                var car=WorldBuilder.SpawnCar(this,new Vector3(3400,2,-1900),"Damage panels test",Color.gray,false,0);var other=WorldBuilder.SpawnCar(this,new Vector3(3450,2,-1900),"Intact panels test",Color.gray,false,0);
                Require(car.MaxDurability==600&&car.Durability==600,"Ordinary car must start with 600 HP");
                var intact=new HashSet<Mesh>();foreach(var filter in car.GetComponentsInChildren<MeshFilter>())intact.Add(filter.sharedMesh);
                car.GetComponent<VehicleDestruction>().BulletHit(32,car.transform.position+Vector3.up);Require(car.Durability>580,"One rifle hit must not critically damage fresh car");
                car.Durability=car.MaxDurability*.3f;var body=car.GetComponent<VehicleBodyDamage>();body.Refresh();bool changed=false;foreach(var filter in car.GetComponentsInChildren<MeshFilter>())if(!intact.Contains(filter.sharedMesh))changed=true;
                Require(body.WindowsBroken&&body.MissingDoors==2&&changed,"Damage must remove glazing and cut two real door openings");
                Require(!other.GetComponent<VehicleBodyDamage>().WindowsBroken&&other.GetComponent<VehicleBodyDamage>().MissingDoors==0,"Shared prefabs must stay intact on another vehicle");
                car.Durability=car.MaxDurability;body.Refresh();Require(!body.WindowsBroken&&body.MissingDoors==0,"Full repair must restore glass and doors");
                foreach(var filter in car.GetComponentsInChildren<MeshFilter>())Require(intact.Contains(filter.sharedMesh),"Repair must restore original mesh references");
                Cars.Remove(car);Destroy(car.gameObject);Cars.Remove(other);Destroy(other.gameObject);
                for(int role=0;role<Roles.Length;role++){RoleIndex=role;Require(CanUseWeapon(MinigunIndex)==(role==1||role==2),"Minigun must be restricted to police and FBI: "+role);}
                foreach(int role in new[]{1,2})
                {
                    SelectRole(role);Player.Teleport(new Vector3(3100,2,-2000));var target=HostileAgent.Spawn(this,Player.Position+Vector3.forward*9,"Minigun target",false);target.Health=1000;Physics.SyncTransforms();Camera.main.transform.position=Player.Position+new Vector3(0,1.4f,-7);Camera.main.transform.LookAt(target.transform.position+Vector3.up);
                    WeaponIndex=MinigunIndex;ServiceSelected=false;int ammo=magazines[MinigunIndex];for(int shot=0;shot<3;shot++){nextShot=0;Fire();}Require(magazines[MinigunIndex]==ammo-3&&target.Health<1000,"Minigun must fire and consume ammo for allowed role");
                    magazines[MinigunIndex]=0;ReloadWeapon();reloadingUntil=Time.time-1;UpdateCombat();Require(magazines[MinigunIndex]==300,"Minigun must reload its ammo belt");Enemies.Remove(target);Destroy(target.gameObject);
                }
                SelectRole(3);WeaponIndex=MinigunIndex;ServiceSelected=false;int denied=magazines[MinigunIndex];nextShot=0;Fire();Require(magazines[MinigunIndex]==denied,"Army must not fire minigun even with forced weapon index");SelectRole(0);CheckDriveBy();
            }
            catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);result(false);yield break;}
            foreach(var kind in new[]{VehicleKind.Sedan,VehicleKind.Sport,VehicleKind.Supercar,VehicleKind.Electric,VehicleKind.SUV,VehicleKind.Truck})
            {
                var car=WorldBuilder.SpawnCar(this,new Vector3(3400,2,-2100),"High speed handling",Color.white,false,0,kind);car.TestControls=Vector2.zero;yield return new WaitForSeconds(1.5f);
                foreach(int side in new[]{-1,1})
                {
                    car.Body.rotation=Quaternion.identity;car.transform.rotation=Quaternion.identity;car.Body.angularVelocity=Vector3.zero;car.Body.linearVelocity=Vector3.forward*42;car.TestControls=new Vector2(side*.7f,1);float yaw=car.transform.eulerAngles.y;yield return new WaitForSeconds(1);
                    try{float turn=Mathf.DeltaAngle(yaw,car.transform.eulerAngles.y);Require(turn*side>5&&car.transform.up.y>.85f&&car.Speed>65,"High-speed steering must turn in requested direction while upright: "+kind+" / "+side+" yaw="+turn+" speed="+car.Speed);}
                    catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);result(false);}
                }
                Cars.Remove(car);Destroy(car.gameObject);yield return null;
            }
            Debug.Log("SUNSET_CAMERA_PITCH_KEY_ZOOM_SMOKE_CLEARANCE_600HP_DOORS_REPAIR_POLICE_FBI_MINIGUN_HIGH_SPEED_STEERING_OK");
        }
    }
}
