using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        Transform custodyVisual;VehicleMotor custodyCar;
        public static bool CustodySeats(VehicleMotor car)=>car && car.Durability>0 && (car.Faction==1||car.Faction==2||car.Faction==12) && car.Kind!=VehicleKind.Motorcycle && car.Kind!=VehicleKind.Bicycle && car.Kind!=VehicleKind.Tank && car.Kind!=VehicleKind.Boat;
        void ClearCustody(){if(custodyVisual)Destroy(custodyVisual.gameObject);custodyVisual=null;custodyCar=null;if(transportSuspect)Destroy(transportSuspect);transportSuspect=null;interrogatable=null;}
        void UpdateTransport()
        {
            if(!transportSuspect){if(custodyVisual)Destroy(custodyVisual.gameObject);custodyVisual=null;custodyCar=null;return;}
            if(!custodyVisual){var actor=new GameObject("Задержанный · наручники");custodyVisual=actor.transform;CharacterModels.Create(actor.transform,14,new Color(.3f,.35f,.4f));}
            var car=Player.Vehicle;
            if(car && CustodySeats(car))
            {
                if(custodyCar!=car){custodyCar=car;custodyVisual.SetParent(car.transform,false);CharacterModels.FreezePose(custodyVisual.GetChild(0),true);Notify("Задержанный пристёгнут на заднем сиденье. Доставь к администрации тюрьмы.");}
                custodyVisual.localPosition=new Vector3(.43f,.18f,-.65f);custodyVisual.localRotation=Quaternion.identity;
            }
            else
            {
                if(custodyCar){custodyCar=null;custodyVisual.SetParent(null,true);Destroy(custodyVisual.gameObject);custodyVisual=null;return;}
                Vector3 wanted=Player.Position+Player.transform.right*1.4f;
                if(SpawnSafety.TryPlayerPosition(wanted,out Vector3 safe))custodyVisual.position=safe;
                custodyVisual.rotation=Player.transform.rotation;
            }
            transportSuspect.transform.position=custodyVisual.position;
            bool atPrison=Vector3.Distance(Player.Position,RoleHome(12))<24;
            if(!atPrison || car && (!CustodySeats(car)||car.Speed>6))return;
            Money+=600;AwardXP(70);ClearCustody();Notify("Задержанный передан дежурному тюрьмы: +$600");Save();
        }
    }
}
