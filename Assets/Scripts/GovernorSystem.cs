using System.Collections.Generic;
using UnityEngine;
namespace SunsetState
{
    public static class GovernorCampaign
    {
        static readonly string[] Arcs={"Первый кабинет","Бюджет больниц","Дорожный фонд","Безопасные школы","Реформа полиции","Открытая мэрия","Встречи жителей","Кризис доверия","Защита свидетелей","Новый мандат"};
        static readonly string[] Stages={"Обращение жителей","Инспекция объекта","Встреча с ведомством","Проект закона","Отчёт кабинета"};
        public static string Text(int chapter)=>Arcs[Mathf.Clamp(chapter/5,0,9)]+" · "+Stages[chapter%5]+"|Проверь исполнение решений штата, собери обращения жителей и передай материалы кабинету. L — законы с действующими игровыми эффектами. Мафия пытается сорвать реформы.";
    }
    public sealed partial class GameRoot
    {
        public int StateLaws;float nextGovernorKidnap=180,rescueShot;Vector3 governorReturn;bool governorCaptured;
        readonly List<HostileAgent> kidnapGuards=new List<HostileAgent>();readonly List<GameObject> rescueObjects=new List<GameObject>();GameObject captiveGate;
        Mission MakeGovernorMission(int chapter){var home=WorldBuilder.Places[17].Position;return new Mission(GovernorCampaign.Text(chapter).Split('|')[0],GovernorCampaign.Text(chapter).Split('|')[1],1800+chapter*30,false,new[]{WorldBuilder.Places[new[]{10,4,11,1,15}[chapter%5]].Position,home},"Посети ведомство и собери материалы","Сдай отчёт кабинету губернатора");}
        void DrawLawsPanel(float left,float h)
        {
            Label(left,85,1040,50,"МЭРИЯ / ГУБЕРНАТОР · ЗАКОНЫ ШТАТА",heading);
            string[] names={"Медицинская субсидия · лечение после поражения бесплатно","Поддержка занятости · выплаты за задания +10%","Усиленный патруль · звезда розыска исчезает через 18 секунд"};
            for(int i=0;i<3;i++){int bit=1<<i;if(Button(new Rect(left,190+i*100,1040,80),((StateLaws&bit)!=0?"ДЕЙСТВУЕТ · ":"ПРИНЯТЬ · ")+names[i])){if(RoleIndex!=17&&RoleIndex!=10)return;StateLaws^=bit;Save();Notify((StateLaws&bit)!=0?"Закон подписан и вступил в силу.":"Закон отменён.");}}
            Label(left,545,1040,100,"Законы сохраняются вместе с миром и действуют после смены роли. В свободное время губернатору угрожает мафия: ФБР приходит на помощь при похищении.",body);
        }
        void StartGovernorKidnap()
        {
            if(RoleIndex!=17||governorCaptured)return;governorReturn=Player.Position;governorCaptured=true;Vector3 point=Supported(WorldBuilder.Places[16].Position+new Vector3(-20,0,-35));Player.Teleport(point);
            captiveGate=WorldBuilder.Box("Дверь удержания губернатора",point+new Vector3(0,1.5f,2),new Vector3(5,3,.15f),Color.gray);rescueObjects.Add(captiveGate);
            foreach(int side in new[]{-1,1})rescueObjects.Add(WorldBuilder.Box("Стена удержания",point+new Vector3(side*2.5f,1.5f,0),new Vector3(.2f,3,4),Color.gray));
            rescueObjects.Add(WorldBuilder.Box("Стена удержания",point+new Vector3(0,1.5f,-2),new Vector3(5,3,.2f),Color.gray));
            for(int i=0;i<3;i++){var enemy=HostileAgent.Spawn(this,point+new Vector3(-7+i*7,0,10),"Мафия · похититель",false,16);enemy.AttackDamage=2;kidnapGuards.Add(enemy);}
            for(int i=0;i<3;i++){var agent=new GameObject("ФБР SWAT · спасение губернатора");agent.transform.position=Supported(point+new Vector3(-8+i*8,0,23));var visual=CharacterModels.Create(agent.transform,2,Color.black);rescueObjects.Add(agent);}
            rescueShot=Time.time+8;Notify("Мафия похитила губернатора. Укрывайся: группа ФБР готовит штурм!");
        }
        void UpdateGovernor()
        {
            if(governorCaptured)
            {
                if(RoleIndex!=17){ClearGovernorRescue();return;}if(Time.time<rescueShot)return;rescueShot=Time.time+1.6f;
                HostileAgent target=null;foreach(var enemy in kidnapGuards)if(enemy&&!enemy.Neutralized){target=enemy;break;}
                if(target){Vector3 from=rescueObjects[rescueObjects.Count-1].transform.position+Vector3.up*1.4f;DrawShot(from,target.transform.position+Vector3.up,Color.cyan);target.TakeDamage(45);return;}
                ClearGovernorRescue();Player.Teleport(governorReturn);nextGovernorKidnap=Time.time+210;Notify("ФБР освободило губернатора и вернуло под охрану. Можно продолжать работу.");return;
            }
            if(RoleIndex==17&&!InInterior&&ActiveMission==null&&Time.time>=nextGovernorKidnap)StartGovernorKidnap();
        }
        void ClearGovernorRescue(){foreach(var enemy in kidnapGuards)if(enemy)Destroy(enemy.gameObject);kidnapGuards.Clear();foreach(var item in rescueObjects)if(item)Destroy(item);rescueObjects.Clear();governorCaptured=false;nextGovernorKidnap=Time.time+180;}
    }
}
