using System.Collections.Generic;
using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        readonly List<HostileAgent> responseOfficers=new List<HostileAgent>();readonly List<VehicleMotor> responseCars=new List<VehicleMotor>();float nextResponse,footArrest;
        public int ResponseOfficers=>responseOfficers.Count;
        void UpdateWantedResponse()
        {
            if(Wanted<=0){ClearWantedResponse();return;}if(InInterior)return;
            float nearest=100;foreach(var officer in responseOfficers)if(officer && !officer.Neutralized && !officer.Stunned)nearest=Mathf.Min(nearest,Vector3.Distance(officer.transform.position,Player.Position));
            footArrest=nearest<3.5f && (!Player.Vehicle || Player.Vehicle.Speed<5)?footArrest+Time.deltaTime:Mathf.Max(0,footArrest-Time.deltaTime);
            if(footArrest>=3){CapturePlayer();return;}
            foreach(var car in responseCars)if(car){Vector3 delta=car.transform.InverseTransformDirection(Player.Position-car.transform.position);float angle=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;car.AIControls=new Vector2(Mathf.Clamp(angle/35,-1,1),delta.magnitude>10?(car.Speed<65?.8f:0):car.Speed>4?-1:0);}
            if(Time.time<nextResponse)return;nextResponse=Time.time+8;
            for(int i=responseOfficers.Count-1;i>=0;i--){var officer=responseOfficers[i];if(!officer || officer.Neutralized || Vector3.Distance(officer.transform.position,Player.Position)>260){if(officer){Enemies.Remove(officer);Destroy(officer.gameObject,officer.Neutralized?15:0);}responseOfficers.RemoveAt(i);}}
            for(int i=responseCars.Count-1;i>=0;i--){var car=responseCars[i];if(!car || car.Durability<=0 || Vector3.Distance(car.transform.position,Player.Position)>300){if(car){var agent=car.GetComponent<HostileAgent>();if(agent)Enemies.Remove(agent);Cars.Remove(car);Destroy(car.gameObject,10);}responseCars.RemoveAt(i);}}
            int count=Mathf.Min(8,Wanted+1);while(responseOfficers.Count<count)
            {
                float angle=(responseOfficers.Count*137+Time.time*7)*Mathf.Deg2Rad;Vector3 position=Player.Position+new Vector3(Mathf.Sin(angle)*55,1,Mathf.Cos(angle)*55);
                if(SpawnSafety.TryPlayerPosition(position,out var safe))position=safe;
                bool federal=Wanted>=4 && responseOfficers.Count%2==0;var officer=HostileAgent.Spawn(this,position,federal?"Агент ФБР · задержание":"Полицейский · задержание",false,federal?2:1);officer.AttackDamage=federal?9:6;responseOfficers.Add(officer);
            }
            int carsWanted=Wanted>=3?Wanted-2:0;while(responseCars.Count<carsWanted)
            {
                var car=WorldBuilder.SpawnCar(this,Player.Position+new Vector3(45+responseCars.Count*12,0,-70),Wanted>=4?"FBI Tactical Response":"Police Backup",Color.blue,false,0,Wanted>=4?VehicleKind.SUV:VehicleKind.Sedan);car.Faction=Wanted>=4?2:1;WorldBuilder.AddServiceDetails(car,car.Faction);car.SetEmergency(true);HostileAgent.AttachToPatrol(this,car);responseCars.Add(car);
            }
        }
        void CapturePlayer()
        {
            Money=Mathf.Max(0,Money-500);CancelMission();EndCall(false);RoleIndex=13;CasualClothes=false;Escaped=false;Wanted=0;footArrest=0;ClearWantedResponse();EquipForRole();Player.Teleport(WorldBuilder.Places[13].Position);Player.SetColor(Roles[13].Color);Notify("Полиция задержала тебя. Ты в тюрьме: J — поручения или побег.");Save();
        }
        void ClearWantedResponse(){if(responseOfficers.Count==0 && responseCars.Count==0)return;foreach(var officer in responseOfficers)if(officer){Enemies.Remove(officer);Destroy(officer.gameObject);}foreach(var car in responseCars)if(car){var agent=car.GetComponent<HostileAgent>();if(agent)Enemies.Remove(agent);Cars.Remove(car);Destroy(car.gameObject);}responseOfficers.Clear();responseCars.Clear();footArrest=0;}
    }
}
