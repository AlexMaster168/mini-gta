using UnityEngine;
namespace SunsetState
{
    public static class MafiaCampaign
    {
        static readonly string[] Arcs={"Чужой долг","Пропавший курьер","Разговор с семьёй","Свидетель порта","Враждебный квартал","Цена информации","Заложник сделки","Нарушенное обещание","Раскол семьи","Последний выбор"};
        static readonly string[] Stages={"Наводка","Захват","Перевозка","Переговоры","Развязка"};
        public static string Text(int chapter)=>Arcs[Mathf.Clamp(chapter/5,0,9)]+" · "+Stages[chapter%5]+"|Семья требует найти участника сделки. Захвати отмеченного NPC, доставь в укрытие и реши его судьбу: освободить или завершить переговоры. Полиция будет преследовать похитителя.";
    }
    public sealed partial class GameRoot
    {
        GameObject hostage;bool hostageFollowing,hostageDelivered,hostageRescued;bool mafiaHostage;
        Mission MakeMafiaMission(int chapter,int choice)
        {
            var home=WorldBuilder.Places[16].Position;
            return new Mission(MafiaCampaign.Text(chapter).Split('|')[0],MafiaCampaign.Text(chapter).Split('|')[1],1900+chapter*30,false,new[]{new Vector3(-400+(chapter%5)*180,1,-200),home,home+Vector3.right*14},"Найди человека · 8 затем F — захватить","Доставь пленника в особняк","F — освободить / завершить переговоры");
        }
        Vector3 Supported(Vector3 requested){return SpawnSafety.TryPlayerPosition(requested,out Vector3 safe)?safe:Player.Position+Vector3.right*2;}
        void SpawnHostage(Vector3 position,bool mafia)
        {
            ClearHostage();hostage=new GameObject(mafia?"Цель похищения · F":"Заложник · E");hostage.transform.position=Supported(position);mafiaHostage=mafia;
            CharacterModels.Create(hostage.transform,0,new Color(.65f,.6f,.5f));hostage.AddComponent<NeutralActor>();var collider=hostage.AddComponent<CapsuleCollider>();collider.center=Vector3.up*.9f;collider.height=1.8f;collider.radius=.3f;
            hostage.AddComponent<NpcGroundMotor>();
        }
        void PrepareMafiaHostage(){if(RoleIndex==16&&!OnVice&&ActiveMission!=null)SpawnHostage(ActiveMission.Points[0]+Vector3.right*3,true);}
        void ClearHostage(){if(hostage)Destroy(hostage);hostage=null;hostageFollowing=false;hostageDelivered=false;hostageRescued=false;mafiaHostage=false;}
        bool HostageStageBlocked=>mafiaHostage&&RoleIndex==16&&ActiveMission!=null&&((Step==0&&!hostageFollowing)||(Step==1&&!hostageDelivered)||(Step==2&&hostageFollowing));
        void UpdateHostage()
        {
            UpdateOperation();if(!hostage)return;
            if(!NpcEffects.Alive(hostage)){Notify("Заложник погиб. Операция провалена.");if(Call!=null&&Call.Code=="10-91")EndCall(false);if(RoleIndex==16)CancelMission();return;}
            float distance=Vector3.Distance(Player.Position,hostage.transform.position);
            if(!hostageFollowing && distance<4 && ((mafiaHostage&&Input.GetKeyDown(KeyCode.F))||(!mafiaHostage&&Input.GetKeyDown(KeyCode.E))))
            {bool safe=true;if(!mafiaHostage&&Call!=null)foreach(var enemy in Call.Threats)if(enemy&&!enemy.Neutralized&&!enemy.Stunned)safe=false;
                if(!safe){Notify("Сначала обезвредь охрану заложника.");return;}hostageFollowing=true;if(mafiaHostage){Wanted=4;wantedTimer=0;}Notify(mafiaHostage?"Пленник захвачен. Садись в автомобиль и езжай в особняк.":"Заложник освобождён. Доставь в участок на любом автомобиле.");}
            if(!hostageFollowing)return;
            bool riding=Player.Vehicle&&Player.Vehicle.Kind!=VehicleKind.Motorcycle&&Player.Vehicle.Kind!=VehicleKind.Bicycle&&Player.Vehicle.Kind!=VehicleKind.Tank;
            var motor=hostage.GetComponent<NpcGroundMotor>();foreach(var collider in hostage.GetComponents<Collider>())collider.enabled=!riding;
            if(riding){if(hostage.transform.parent!=Player.Vehicle.transform){hostage.transform.SetParent(Player.Vehicle.transform,false);CharacterModels.FreezePose(hostage.transform.GetChild(0),true);}hostage.transform.localPosition=new Vector3(.4f,.18f,-.6f);hostage.transform.localRotation=Quaternion.identity;motor.enabled=false;}
            else{if(hostage.transform.parent){hostage.transform.SetParent(null,true);if(hostage.transform.childCount>0)Destroy(hostage.transform.GetChild(0).gameObject);CharacterModels.Create(hostage.transform,0,Color.white);hostage.transform.position=Supported(Player.Position+Player.transform.right*2);motor.enabled=true;}var delta=Player.Position+Player.transform.right*1.5f-hostage.transform.position;delta.y=0;motor.Move(Vector3.ClampMagnitude(delta,1)*4);}
            Vector3 destination=RoleHome(mafiaHostage?16:1);
            if(Vector3.Distance(Player.Position,destination)<20 && (!Player.Vehicle||Player.Vehicle.Speed<5))
            {
                if(!mafiaHostage){hostageRescued=true;hostageFollowing=false;Destroy(hostage);hostage=null;Notify("Заложник в безопасности, передан медикам участка.");return;}
                hostageDelivered=true;if(Input.GetKeyDown(KeyCode.F)&&Step>=2){hostageFollowing=false;Destroy(hostage);hostage=null;Notify("Переговоры завершены. Пленник освобождён.");}
            }
        }
    }
}
