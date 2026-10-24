using System.Collections.Generic;
using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        readonly List<string> phoneMessages=new List<string>();int phoneTab;Vector2 phoneScroll;Vector3? gpsDestination;string phoneDraft="";float nextPhoneEvent=35;
        void PhoneMessage(string sender,string message){phoneMessages.Insert(0,sender+"\n"+message);while(phoneMessages.Count>30)phoneMessages.RemoveAt(phoneMessages.Count-1);}
        void UpdatePhone(){if(Time.time<nextPhoneEvent)return;nextPhoneEvent=Time.time+100;PhoneMessage(StoryBook.Stories[RoleIndex].Contact,ActiveMission!=null?ActiveMission.Steps[Step]:RoleIndex==6?"Есть новые поездки. Открой телефон → Такси и выбери заказ.":"Есть работа. Открой телефон → Контакты или J → Сюжет.");}
        void SetGps(Vector3 point,string title){gpsDestination=point;Panel="";Time.timeScale=1;SetCursor(true);Notify("GPS: "+title+" · маршрут на миникарте и M");}
        void DrawPhone(float left,float h)
        {
            Label(left,85,1040,45,"SUNSET PHONE · "+Roles[RoleIndex].Name+" · P — убрать",heading);
            string[] tabs={"Сообщения","Контакты / звонки","GPS","Радио / звук","Такси"};
            for(int i=0;i<5;i++){int id=i;if(Button(new Rect(left+i*212,145,205,42),tabs[i])){phoneTab=id;phoneScroll=Vector2.zero;}}
            if(phoneTab==0)
            {
                phoneDraft=GUI.TextField(new Rect(left,205,800,40),phoneDraft,180);
                if(Button(new Rect(left+820,205,230,40),"ОТПРАВИТЬ КОНТАКТУ")){if(phoneDraft.Trim().Length>0){PhoneMessage("Ты → "+StoryBook.Stories[RoleIndex].Contact,phoneDraft);PhoneMessage(StoryBook.Stories[RoleIndex].Contact,"Принято. Материалы следующего этапа: "+(ActiveMission!=null?ActiveMission.Steps[Step]:"Зайди в раздел Контакты и получи поручение."));phoneDraft="";}}
                phoneScroll=GUI.BeginScrollView(new Rect(left,265,1060,h-320),phoneScroll,new Rect(0,0,1030,Mathf.Max(300,phoneMessages.Count*110)));
                if(phoneMessages.Count==0)Label(10,10,1000,80,"Сообщений пока нет. Контакты присылают адреса и поручения во время игры.",body);
                for(int i=0;i<phoneMessages.Count;i++){RectBox(new Rect(0,i*110,1020,100),new Color(.1f,.18f,.15f));Label(16,i*110+12,990,86,phoneMessages[i],body);}GUI.EndScrollView();
            }
            if(phoneTab==1)
            {
                Label(left,220,1040,80,StoryBook.Stories[RoleIndex].Contact+"\nИгровой контакт: текстовый звонок и поручения твоей роли.",body);
                if(Button(new Rect(left,330,1040,65),"ПОЗВОНИТЬ · УЗНАТЬ ТЕКУЩУЮ ЗАДАЧУ")){PhoneMessage(StoryBook.Stories[RoleIndex].Contact,ActiveMission!=null?ActiveMission.Description+"\nДалее: "+ActiveMission.Steps[Step]:"Наша история: "+StoryBook.Stories[RoleIndex].Name+". Я прислал доступ к следующей главе.");phoneTab=0;}
                if(Button(new Rect(left,420,1040,65),"ПРИНЯТЬ СЛЕДУЮЩУЮ ГЛАВУ ПО ТЕЛЕФОНУ"))BeginStory(0);
                if(Button(new Rect(left,510,1040,65),"ПОЛУЧИТЬ РАБОЧЕЕ ПОРУЧЕНИЕ"))AcceptMission(Random.Range(0,JobsPerRole));
            }
            if(phoneTab==2)
            {
                if(Button(new Rect(left,210,510,50),"GPS · ТЕКУЩИЙ ЭТАП ЗАДАНИЯ")){gpsDestination=null;Toggle("map");}
                if(Button(new Rect(left+530,210,510,50),"СБРОСИТЬ ЛИЧНУЮ МЕТКУ")){gpsDestination=null;Notify("Личная GPS-метка убрана.");}
                phoneScroll=GUI.BeginScrollView(new Rect(left,280,1060,h-325),phoneScroll,new Rect(0,0,1030,(Roles.Length+6)*53));
                for(int i=0;i<Roles.Length;i++){int role=i;if(Button(new Rect(0,i*53,1010,46),Roles[i].Name+" · отделение"))SetGps(RoleHome(role),Roles[role].Name);}
                float y=Roles.Length*53;if(Button(new Rect(0,y,1010,46),"АЭРОПОРТ · VISA / VICE CITY"))SetGps(OnVice?ViceCityIsland.IslandAirport:ViceCityIsland.Airport,"Аэропорт");
                if(Button(new Rect(0,y+53,1010,46),"ГОСУДАРСТВЕННЫЙ БАНК"))SetGps(StateBank.Entrance.Door,"Банк");if(Button(new Rect(0,y+106,1010,46),"МАГАЗИН 24/7"))SetGps(ConvenienceStores.Locations[0]+Vector3.back*15,"24/7");if(Button(new Rect(0,y+159,1010,46),"ЧЁРНЫЙ РЫНОК · ГУРРАМ / СНЯТЬ РОЗЫСК"))SetGps(BlackMarket.Points[OnVice?1:0],"Чёрный рынок · Гуррам");if(Button(new Rect(0,y+212,1010,46),"ОБЩИЙ ТИР · ТРЕНИРОВКА СТРЕЛЬБЫ"))SetGps(ShootingRanges.Terminals[0],"Общий тир");if(Button(new Rect(0,y+265,1010,46),"АРМЕЙСКИЙ ТИР"))SetGps(ShootingRanges.Terminals[1],"Армейский тир");GUI.EndScrollView();
            }
            if(phoneTab==3)
            {
                Label(left,210,1000,35,"Общая громкость",body);AudioListener.volume=GUI.HorizontalSlider(new Rect(left,250,1000,25),AudioListener.volume,0,1);
                Label(left,285,1000,35,"Двигатель · автоматически тише при включённом радио",body);VehicleAudio.EngineVolume=GUI.HorizontalSlider(new Rect(left,325,1000,25),VehicleAudio.EngineVolume,0,.3f);
                Label(left,360,1000,35,"Громкость радио",body);StateRadio.Volume=GUI.HorizontalSlider(new Rect(left,400,1000,25),StateRadio.Volume,0,.6f);
                if(Button(new Rect(left,450,1040,60),"РАДИО · ВКЛЮЧИТЬ / ВЫКЛЮЧИТЬ"))StateRadio.Instance?.PhoneRadio(-1);
                phoneScroll=GUI.BeginScrollView(new Rect(left,530,1060,h-585),phoneScroll,new Rect(0,0,1030,StateRadio.TrackNames.Length*51));
                for(int i=0;i<StateRadio.TrackNames.Length;i++)if(Button(new Rect(0,i*51,1010,44),(i+1)+" · "+StateRadio.TrackNames[i]))StateRadio.Instance?.PhoneRadio(i);
                GUI.EndScrollView();
            }
            if(phoneTab==4)
            {
                if(RoleIndex!=6){Label(left,230,1040,90,"Заказы доступны в роли Таксист. Tab — выбрать профессию.",body);return;}
                for(int i=0;i<5;i++){int id=i;Vector3 pickup=OnVice?ViceCityIsland.Center+new Vector3(-320+i*160,0,160):Point(-2+i,1);Vector3 drop=OnVice?ViceCityIsland.Center+new Vector3(320,0,-320+i*160):Point(2,-2+i);
                    if(Button(new Rect(left,215+i*79,1040,67),new[]{"Анна · в больницу","Мария · на вокзал","Алекс · деловой центр","Елена · ночная смена","Даниил · домой"}[i]+" · $"+(600+i*150)+" · подача "+Mathf.RoundToInt(Vector3.Distance(Player.Position,pickup))+" м"))
                    {ActivateMission(new Mission("Телефон · заказ такси №"+(id+1),"Забери пассажира у GPS-метки. Остановись на три секунды, затем отвези по адресу.",600+id*150,true,new[]{pickup,drop},"Подача: остановись у пассажира","Отвези пассажира по адресу"));PhoneMessage("Диспетчер такси","Заказ принят. Подача и адрес назначения отмечены на GPS.");}}
            }
        }
    }
}
