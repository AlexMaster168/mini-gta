using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        float speedingFor,nextSpeedFine;float[] stopWait=new float[3],stopCooldown=new float[3];
        public bool ServiceFineExempt=>RoleIndex>=1&&RoleIndex<=5||RoleIndex==10||RoleIndex==11||RoleIndex==12||RoleIndex==15||RoleIndex==17||Player.Vehicle&&Player.Vehicle.Faction>=0&&Player.Vehicle.Faction!=9&&Player.Vehicle.Faction!=14&&Player.Vehicle.Faction!=16;
        public static readonly Vector3[] StopLines={new Vector3(-200,1,0),new Vector3(200,1,200),new Vector3(0,1,-200)};
        void UpdateTrafficRules()
        {
            var car=Player.Vehicle;if(!car||car.Flying||car.OnWater||ServiceFineExempt){speedingFor=0;return;}
            bool urban=OnVice||Vector3.Distance(Player.Position,Vector3.zero)<1000||Vector3.Distance(Player.Position,StateBuilder.SanFierro)<850||Vector3.Distance(Player.Position,StateBuilder.LasVenturas)<850;
            speedingFor=urban&&car.Speed>70?speedingFor+Time.deltaTime:0;
            if(speedingFor>=3&&Time.time>=nextSpeedFine){TrafficFine(250,"Превышение городского ограничения 60 км/ч");speedingFor=0;nextSpeedFine=Time.time+45;}
            for(int i=0;i<StopLines.Length;i++){float distance=Vector3.Distance(Player.Position,StopLines[i]);if(distance<22&&car.Speed<3)stopWait[i]+=Time.deltaTime;if(distance>40)stopWait[i]=0;if(distance<8&&car.Speed>12&&stopWait[i]<1&&Time.time>=stopCooldown[i]){TrafficFine(350,"STOP без полной остановки");stopCooldown[i]=Time.time+45;}}
        }
        void TrafficFine(int amount,string reason){if(ServiceFineExempt)return;int paid=Mathf.Min(Money,amount);Money-=paid;PhoneMessage("Дорожная служба",reason+". Штраф $"+amount+", списано $"+paid+".");Notify("Штраф: "+reason+" · −$"+paid);Save();}
        public static void BuildStopSigns(Transform root)
        {foreach(var point in StopLines){WorldBuilder.Box("STOP post",point+new Vector3(14,.5f,0),new Vector3(.15f,3,.15f),Color.gray,root);var sign=WorldBuilder.Box("STOP sign",point+new Vector3(14,2,0),new Vector3(1.3f,1.3f,.1f),Color.red,root);InteriorSystem.Label(point+new Vector3(14,2,-.1f),"STOP",sign.transform,.045f);}}
        bool FreeRoleArmor=>RoleIndex!=0&&RoleIndex!=6&&RoleIndex!=7&&RoleIndex!=8&&RoleIndex!=13;
        void EquipFreeArmor(){if(!FreeRoleArmor)return;Armor=MaxArmor;Save();Notify("Бесплатный бронежилет надет · "+Mathf.RoundToInt(Armor)+" брони");}
    }
}
