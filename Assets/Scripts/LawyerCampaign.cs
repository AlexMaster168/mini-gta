using UnityEngine;
namespace SunsetState
{
    public static class LawyerCampaign
    {
        static readonly string[] Cases={"Потерянный кошелёк","Неверный номер","Свидетель с рынка","Ночной налёт","Чужая машина","След на складе","Спорная экспертиза","Подставной курьер","Дело информатора","Последняя апелляция"};
        static readonly string[] Stages={"Встреча в изоляторе","Проверка протокола","Свидетель защиты","Независимое заключение","Судебное заседание"};
        static readonly string[] Facts={"Камера магазина показывает клиента в другом месте в момент кражи.","Номер задержанной машины отличается от номера в заявлении на одну цифру.","Свидетель изменил показания после разговора с обвинителем.","Клиент признаёт участие, но добровольно возместил ущерб и помог следствию.","Автомобиль был передан клиенту после времени совершения преступления.","Протокол изъятия не содержит подписи понятых и перечня упаковок.","Независимая экспертиза опровергла совпадение отпечатков.","Курьер не знал содержимого запечатанной посылки: переписка это подтверждает.","Клиент сотрудничал со следствием, но нарушил условия соглашения; есть смягчающие обстоятельства.","Сохранившаяся запись подтверждает, что единственный свидетель ошибся в опознании."};
        public static string Text(int chapter){int c=Mathf.Clamp(chapter/5,0,9),s=chapter%5;return "Дело "+(c+1)+": "+Cases[c]+" · "+Stages[s]+"|"+Facts[c]+" "+(s==4?"Представь доводы в суде и выбери линию защиты. Решение повлияет на судьбу клиента.":"Собери материалы для защиты: "+Stages[s].ToLower()+". Вернись в коллегию с документами.");}
        public static string Fact(int chapter)=>Facts[Mathf.Clamp(chapter/5,0,9)];
        public static bool Mitigation(int chapter)=>chapter/5==3||chapter/5==8;
    }
    public sealed partial class GameRoot
    {
        public int[] CourtOutcomes=new int[10];bool courtResolved;string verdict="";
        Mission MakeLawyerMission(int chapter,int choice)
        {
            var home=WorldBuilder.Places[15].Position;int c=chapter/5,s=chapter%5;
            Vector3 point=s==0?WorldBuilder.Places[12].Position:s==1?WorldBuilder.Places[c%2==0?1:2].Position:s==2?new Vector3(-200+c*70,1,200):s==3?WorldBuilder.Places[4].Position:home;
            return new Mission(LawyerCampaign.Text(chapter).Split('|')[0],LawyerCampaign.Text(chapter).Split('|')[1],1100+c*130,false,new[]{point,home},s==4?"Прибудь в суд: подготовь выступление":"Получи материалы и опроси участника дела","Передай результат в коллегию");
        }
        bool LawyerStageBlocked(bool close,bool stopped)
        {
            if(RoleIndex!=15||ActiveMission==null||activeCampaignRole!=15||StoryProgress[15]%5!=4||Step!=0||courtResolved)return false;
            if(close&&stopped){Panel="court";Time.timeScale=0;SetCursor(false);}return true;
        }
        void ResolveCourt(int argument)
        {
            if(RoleIndex!=15||ActiveMission==null||activeCampaignRole!=15||StoryProgress[15]%5!=4||courtResolved)return;
            bool success=(argument==1)==LawyerCampaign.Mitigation(StoryProgress[15]);courtResolved=true;
            verdict=success?(argument==1?"Суд учёл возмещение ущерба: наказание смягчено, назначена реабилитация.":"Суд принял доказательства защиты: оправдание и освобождение клиента."):"Суд отклонил выбранный довод. Обвинительный приговор; дело направлено на апелляцию.";
            CourtOutcomes[Mathf.Clamp(StoryProgress[15]/5,0,9)]=success?(argument==1?2:1):3;PhoneMessage("Суд штата",verdict);if(success){Money+=900;AwardXP(120);}Panel="";Time.timeScale=1;SetCursor(true);Notify(verdict);Save();
        }
        void DrawCourtPanel(float left,float h)
        {
            Label(left,90,1050,50,"СУД · ЗАЩИТА ПОДСУДИМОГО",heading);Label(left,160,1030,130,LawyerCampaign.Fact(StoryProgress[15]),body);
            Label(left,300,1030,90,"Обвинение просит лишить клиента свободы. Выбери довод, подтверждённый собранными материалами. Оправдание требует доказательств; признание вины и помощь следствию могут смягчить наказание.",body);
            if(Button(new Rect(left,420,1040,65),"ОСПОРИТЬ ОБВИНЕНИЕ · алиби / ошибки доказательств"))ResolveCourt(0);
            if(Button(new Rect(left,505,1040,65),"ПРОСИТЬ СМЯГЧЕНИЕ · возмещение ущерба / сотрудничество"))ResolveCourt(1);
            Label(left,600,1040,90,verdict,body);
        }
    }
}
