using UnityEngine;
namespace SunsetState
{
    public static class ViceCityIsland
    {
        public static readonly Vector3 Center=new Vector3(4300,1,5400),Airport=new Vector3(-400,1,-800),IslandAirport=Center+new Vector3(0,0,600);
        public static readonly Vector3[] Homes=new Vector3[18];
        public static bool Contains(Vector3 p)=>Mathf.Abs(p.x-Center.x)<950&&Mathf.Abs(p.z-Center.z)<1100;
        public static void Build(GameRoot game,Transform root)
        {
            WorldBuilder.Box("Vice City ocean",Center+Vector3.down*.95f,new Vector3(2300,.15f,2600),new Color(.1f,.52f,.66f),root,false);
            WorldBuilder.Box("Vice City island",Center+Vector3.down*.9f,new Vector3(1600,.3f,1800),new Color(.77f,.72f,.49f),root);
            for(int i=-4;i<=4;i++)foreach(bool horizontal in new[]{false,true})
            {var offset=horizontal?new Vector3(0,-.65f,i*160):new Vector3(i*160,-.65f,0);WorldBuilder.Box("Vice boulevard",Center+offset,horizontal?new Vector3(1420,.12f,24):new Vector3(24,.12f,1420),new Color(.21f,.24f,.28f),root);}
            for(int role=0;role<18;role++)
            {
                Vector3 home=Center+new Vector3(-400+role%6*160,0,-320+role/6*160);Homes[role]=home;
                var shell=WorldBuilder.Box("Vice HQ · "+game.Roles[role].Name,home+new Vector3(0,5,38),new Vector3(38,10,32),Color.Lerp(game.Roles[role].Color,Color.white,.5f),root);
                InteriorSystem.Register(shell,new Vector3(38,10,32),"Vice City · "+game.Roles[role].Name,role,home);
                InteriorSystem.Label(home+new Vector3(0,12,20),"VICE CITY · "+UniformDetails.Codes[role],root,.1f);
            }
            for(int i=0;i<24;i++)
            {
                float x=-650+i%12*115,z=i<12?350:470;float h=12+i%5*7;var color=i%2==0?new Color(.9f,.61f,.69f):new Color(.49f,.78f,.81f);
                var hotel=WorldBuilder.Box("Vice pastel hotel",Center+new Vector3(x,h*.5f-1,z),new Vector3(54,h,55),color,root);InteriorSystem.Register(hotel,new Vector3(54,h,55),"Vice City · отель");
            }
            for(int i=0;i<36;i++)
            {Vector3 p=Center+new Vector3(i%2==0?-730:730,2,-750+i/2*85);WorldBuilder.Box("Palm trunk",p,new Vector3(.7f,7,.7f),new Color(.4f,.28f,.14f),root);for(int leaf=0;leaf<4;leaf++){var obj=WorldBuilder.Box("Palm leaves",p+Vector3.up*4,new Vector3(9,.3f,1.5f),new Color(.15f,.42f,.24f),root,false);obj.transform.rotation=Quaternion.Euler(0,leaf*45,12);}}
            BuildAirport(root,Airport,"LOS SANTOS AIRPORT · ВИЗА / РЕЙС · E");BuildAirport(root,IslandAirport,"VICE CITY AIRPORT · ОБРАТНЫЙ РЕЙС · E");
            for(int i=0;i<8;i++)WorldBuilder.SpawnCar(game,Center+new Vector3(-480+i*130,0,170),"Vice City rental",Color.HSVToRGB(i*.12f,.55f,.8f),false,90,i%2==0?VehicleKind.Electric:VehicleKind.Supercar);
        }
        static void BuildAirport(Transform root,Vector3 at,string title)
        {WorldBuilder.Box("Airport terminal",at+new Vector3(0,4,35),new Vector3(65,8,24),new Color(.52f,.71f,.75f),root);WorldBuilder.Box("Airport runway",at+new Vector3(0,-.8f,85),new Vector3(240,.1f,30),new Color(.22f,.23f,.25f),root);InteriorSystem.Label(at+new Vector3(0,10,20),title,root,.13f);}
    }
    public sealed partial class GameRoot
    {
        float visaUntil;public float VisaRemaining=>Mathf.Max(0,visaUntil-Time.realtimeSinceStartup);public bool OnVice=>Player&&ViceCityIsland.Contains(Player.Position);
        Vector3 RoleHome(int role)=>OnVice?ViceCityIsland.Homes[role]:WorldBuilder.Places[role].Position;
        bool TryTravelAction(){if(Vector3.Distance(Player.Position,ViceCityIsland.Airport)>28&&Vector3.Distance(Player.Position,ViceCityIsland.IslandAirport)>28)return false;Toggle("travel");return true;}
        bool BuyVisa(){if(Money<500||Wanted>0||RoleIndex==13&&!Escaped){Notify("Для визы нужны $500 и отсутствие розыска / заключения.");return false;}Money-=500;visaUntil=Time.realtimeSinceStartup+1200;Save();Notify("Виза Vice City: 20 минут работы. Посадка доступна в аэропорту.");return true;}
        void FlyVice(bool outward)
        {if(outward&&VisaRemaining<=0){Notify("Сначала купи визу.");return;}CancelMission();ClearCustody();if(Call!=null)EndCall(false);Player.Teleport(outward?ViceCityIsland.IslandAirport:ViceCityIsland.Airport);Panel="";Time.timeScale=1;SetCursor(true);Save();Notify(outward?"Добро пожаловать в Vice City! Роль сохранена. J — работа, G — транспорт.":"Ты вернулся в Лос-Сантос.");}
        void UpdateTravel(){if(OnVice&&VisaRemaining<=0&&!SmokeMode&&!PreviewMode){FlyVice(false);Notify("Виза истекла: возвращение в Лос-Сантос.");}}
        Mission MakeViceMission(int role,int variant)
        {Vector3 home=ViceCityIsland.Homes[role];return new Mission("Vice City · "+Roles[role].Name+" · смена "+(variant+1),"Временная работа на острове по твоей профессии. Заверши два выезда и вернись в отделение. Виза ограничена по времени.",Roles[role].Pay,false,new[]{ViceCityIsland.Center+new Vector3(-480+variant%5*160,0,160),ViceCityIsland.Center+new Vector3(480,0,-480+variant%5*160),home},role==1||role==2?"Проверь вызов на острове":"Выполни выезд по профессии","Отработай второй адрес","Вернись в островное отделение");}
        void DrawTravel(float left,float h)
        {Label(left,90,1040,50,"АЭРОПОРТ · VICE CITY",heading);Label(left,160,1040,100,"Виза на 20 минут: $500. Осталось: "+Mathf.CeilToInt(VisaRemaining/60)+" мин.\nТвоя роль: "+Roles[RoleIndex].Name+". Островные отделения и задания доступны после прилёта.",body);
            if(Button(new Rect(left,300,1040,65),"КУПИТЬ / ПРОДЛИТЬ ВИЗУ · $500"))BuyVisa();if(Button(new Rect(left,390,1040,65),OnVice?"ОБРАТНЫЙ РЕЙС В ЛОС-САНТОС":"ВЫЛЕТЕТЬ В VICE CITY"))FlyVice(!OnVice);}
    }
}
