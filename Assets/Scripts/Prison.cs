using UnityEngine;

namespace SunsetState
{
    public sealed partial class GameRoot
    {
        public bool Escaped;
        public GameObject PrisonGate;
        bool gateOpen;
        bool TryPrisonAction()
        {
            if(Vector3.Distance(Player.Position,new Vector3(970,1,-400))>12)return false;
            if(RoleIndex==13 && !Escaped){Notify("Ворота заперты. J — подготовить побег или выполнить поручение для освобождения.");return true;}
            gateOpen=!gateOpen;if(PrisonGate)PrisonGate.SetActive(!gateOpen);Notify(gateOpen?"Ворота тюрьмы открыты":"Ворота тюрьмы закрыты");return true;
        }
        Mission PrisonMission(int variant)
        {
            if(variant==0)return new Mission("Побег из San Oro","Найди инструменты во дворе, доберись до туннеля у южной стены и скройся в районе Grove. После туннеля тебя объявят в розыск.",2000,false,new[]{new Vector3(1030,1,-470),new Vector3(1140,1,-535),new Vector3(-600,1,-600)},"Забери инструменты во дворе","Открой туннель у южной стены","Доберись до укрытия Grove. G — транспорт"){Escape=true};
            return new Mission(variant==1?"Тюремные работы":"Документы на освобождение","Выполни три поручения внутри тюрьмы, чтобы законно получить освобождение.",250,false,new[]{new Vector3(1040,1,-350),new Vector3(1130,1,-450),new Vector3(1010,1,-410)},"Получи поручение у склада","Выполни работу во дворе","Передай отметки в администрацию"){Release=true};
        }
        void OnPrisonMissionStep()
        {
            if(RoleIndex!=13 || !ActiveMission.Escape || Step!=2)return;
            Escaped=true;Wanted=4;wantedTimer=0;Player.Teleport(new Vector3(1200,2,-535));
            Notify("Ты выбрался через туннель! Розыск 4. Рядом машина для побега, G — гараж.");
        }
    }
    public static class PrisonBuilder
    {
        public static void Build(GameRoot game,Transform parent)
        {
            Color wall=new Color(.48f,.48f,.43f),steel=new Color(.24f,.28f,.28f);
            WorldBuilder.Box("Prison courtyard",new Vector3(1080,.02f,-430),new Vector3(220,.1f,260),new Color(.42f,.4f,.32f),parent);
            WorldBuilder.Box("Prison east wall",new Vector3(1190,5,-430),new Vector3(3,10,260),wall,parent);
            WorldBuilder.Box("Prison south wall",new Vector3(1080,5,-560),new Vector3(220,10,3),wall,parent);
            WorldBuilder.Box("Prison north wall",new Vector3(1080,5,-300),new Vector3(220,10,3),wall,parent);
            WorldBuilder.Box("Prison west wall A",new Vector3(970,5,-485),new Vector3(3,10,150),wall,parent);
            WorldBuilder.Box("Prison west wall B",new Vector3(970,5,-345),new Vector3(3,10,90),wall,parent);
            game.PrisonGate=WorldBuilder.Box("Prison controlled gate",new Vector3(970,3,-400),new Vector3(1,6,20),steel,parent);
            WorldBuilder.Box("Prison access road",new Vector3(930,.05f,-400),new Vector3(260,.1f,22),new Color(.18f,.2f,.21f),parent);
            for(int i=0;i<3;i++)
            {
                Vector3 p=new Vector3(1020+i*57,0,-335);
                WorldBuilder.Box("Cell block",p+Vector3.up*5,new Vector3(40,10,32),wall,parent);
                for(int j=0;j<5;j++)WorldBuilder.Box("Barred cell window",p+new Vector3(-15+j*7,6,16.1f),new Vector3(2,2,.1f),steel,parent,false);
            }
            foreach(Vector3 p in new[]{new Vector3(980,0,-550),new Vector3(1180,0,-550),new Vector3(1180,0,-310),new Vector3(980,0,-310)})
            {
                WorldBuilder.Box("Guard tower",p+Vector3.up*6,new Vector3(5,12,5),wall,parent);
                WorldBuilder.Box("Watch cabin",p+Vector3.up*13,new Vector3(9,3,9),steel,parent);
            }
            WorldBuilder.Box("Escape tunnel entrance",new Vector3(1140,.3f,-535),new Vector3(4,.5f,4),new Color(.12f,.12f,.11f),parent);
            WorldBuilder.SpawnCar(game,new Vector3(1206,1,-520),"Машина для побега",new Color(.4f,.25f,.17f),false,180);
        }
    }
}
