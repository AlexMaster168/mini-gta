using System;
using System.Collections;
using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        IEnumerator CheckWaterAndTraffic(Action<bool> result)
        {
            SelectRole(0);Money=5000;Player.Health=100;Panel="";Time.timeScale=1;
            var car=WorldBuilder.SpawnCar(this,new Vector3(3400,2,-2100),"Water and traffic test",Color.white,false,0);Player.Enter(car);
            car.transform.position=new Vector3(-1080,-2,300);car.Body.position=car.transform.position;car.Body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
            yield return new WaitForSeconds(.5f);
            try{Require(car.GetComponent<VehicleWaterState>().Flooded&&car.transform.position.y<-.8f,"Vehicle must sink rather than auto-recover from water");breath=.01f;}catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);result(false);}
            yield return new WaitForSeconds(.4f);
            try
            {
                Require(Player.Health<100,"Submerged occupant must lose health after oxygen expires");Player.Exit();Require(!Player.Vehicle&&Swimming,"Player must be able to exit submerged vehicle and swim");int wallet=Money;EvacuateWater(true);Require(Money==wallet-300&&Player.Position.y>0&&car.transform.position.y>0,"Rescue must return player and tow car onto dry land");
                Player.Enter(car);car.Body.linearVelocity=car.transform.forward*25;speedingFor=3;nextSpeedFine=0;wallet=Money;UpdateTrafficRules();Require(Money==wallet-250,"Speeding in personal car must charge fine");
                SelectRole(2);Player.Enter(car);wallet=Money;TrafficFine(350,"test");Require(ServiceFineExempt&&Money==wallet,"Government roles must be exempt from traffic fines");
                foreach(int role in new[]{1,2,3,4,5,9,10,11,12,14,15,16,17}){SelectRole(role);Armor=0;wallet=Money;EquipFreeArmor();Require(Armor==MaxArmor&&Money==wallet,"Government and criminal armour must be free: "+role);}
                foreach(int role in new[]{9,14,16}){SelectRole(role);Require(Armed&&CanUseWeapon(4)&&CanUseWeapon(5)&&CanUseWeapon(10),"Criminal role must have free guns: "+role);}
                SelectRole(0);Player.Health=100;breath=15;Cars.Remove(car);Destroy(car.gameObject);Debug.Log("SUNSET_WATER_SINKING_OXYGEN_SWIMMING_EVACUATION_FINES_EXEMPTIONS_FREE_LOADOUTS_OK");
            }
            catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);result(false);}
        }
    }
}
