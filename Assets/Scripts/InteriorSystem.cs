using System.Collections.Generic;
using UnityEngine;
namespace SunsetState
{
    public sealed class BuildingEntrance
    {
        public GameObject Shell;public string Name;public int Role=-1,Style,ShopId=-1;public Vector3 Door;public Vector3? StreetMarker;public Color Color;
    }
    public static class InteriorSystem
    {
        public static readonly List<BuildingEntrance> Entrances=new List<BuildingEntrance>();
        public static void Register(GameObject shell,Vector3 size,string name,int role=-1,Vector3? entry=null)
        {
            Vector3 floor=shell.transform.position-Vector3.up*size.y*.5f;
            var entrance=new BuildingEntrance{Shell=shell,Name=name,Role=role,Color=role>=0?GameRoot.Instance.Roles[role].Color:new Color(.5f,.67f,.65f),Style=Entrances.Count%4,Door=floor+new Vector3(0,.5f,-size.z*.5f-1.5f),StreetMarker=entry};Entrances.Add(entrance);
            Vector3 visualDoor=floor+new Vector3(0,1.3f,-size.z*.5f-.12f);
            WorldBuilder.Box("Вход",visualDoor,new Vector3(2.1f,2.6f,.15f),new Color(.16f,.22f,.24f),shell.transform.parent);
            Label(visualDoor+new Vector3(0,1.9f,-.1f),role>=0?name+" · E":"ВХОД · E",shell.transform.parent,.055f);
        }
        public static void Label(Vector3 position,string text,Transform parent,float size=.075f)
        {var go=new GameObject("Readable sign");go.transform.SetParent(parent,false);go.transform.position=position;go.transform.rotation=Quaternion.identity;var label=go.AddComponent<TextMesh>();label.text=text;label.fontSize=48;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;label.color=Color.white;WorldBuilder.SetFont(label);var surface=parent.GetComponent<Collider>();if(surface)WorldLabelFit.Attach(label,Mathf.Clamp(surface.bounds.size.x*.85f,.5f,4),.55f);else WorldLabelFit.Attach(label,Mathf.Clamp(text.Length*.10f,1,3.8f),.55f);}
        public static BuildingEntrance Near(Vector3 position)
        {BuildingEntrance best=null;float distance=5;foreach(var e in Entrances){float d=Vector3.Distance(position,e.Door);if(e.StreetMarker.HasValue)d=Mathf.Min(d,Vector3.Distance(position,e.StreetMarker.Value));if(d<distance){distance=d;best=e;}}return best;}
    }
    public sealed partial class GameRoot
    {
        public bool CasualClothes;
        public bool InInterior=>interiorRoot;
        GameObject interiorRoot;BuildingEntrance insideBuilding;Vector3 interiorOrigin,exteriorReturn;
        public Vector3 SavePosition=>InInterior?exteriorReturn:Player.Position;
        static readonly string[] UniformNames={"Повседневная одежда","Полицейская форма","Костюм федерального агента","Армейская форма","Медицинский халат","Пожарная спецодежда","Форма таксиста","Рабочая одежда перевозчика","Комбинезон механика","Одежда Grove","Деловой костюм мэрии","Форма инструктора","Форма охраны тюрьмы","Тюремная роба","Уличная одежда","Костюм адвоката","Костюм мафии","Костюм губернатора"};
        public string UniformName=>CasualClothes?"Гражданская одежда":UniformNames[RoleIndex];
        public void ClearInteriorOnTeleport(Vector3 position)
        {if(InInterior && position.y<500){Destroy(interiorRoot);interiorRoot=null;insideBuilding=null;}}
        public void EnterBuilding(BuildingEntrance entrance)
        {
            if(Player.Vehicle){Notify("Сначала выйди из транспорта.");return;}
            exteriorReturn=entrance.Door;insideBuilding=entrance;interiorOrigin=new Vector3(entrance.Door.x,600,entrance.Door.z);
            interiorRoot=new GameObject("Interior · "+entrance.Name);BuildInterior(entrance);
            Player.Teleport(interiorOrigin+new Vector3(0,1,-7));Notify(entrance.Name+" · E — взаимодействие · выход у зелёной двери");
        }
        public void ExitBuilding()
        {if(!InInterior)return;Vector3 outside=exteriorReturn;Destroy(interiorRoot);interiorRoot=null;insideBuilding=null;Player.Teleport(outside+Vector3.back*1.5f);}
        string InteriorPrompt()
        {
            if(!InInterior){var door=InteriorSystem.Near(Player.Position);return door!=null?"E — войти: "+door.Name:"";}
            Vector3 local=Player.Position-interiorOrigin;
            if(Vector3.Distance(local,new Vector3(0,1,-8))<3)return "E — выйти на улицу";
            if(Vector3.Distance(local,new Vector3(-8,1,2))<3)return "E — раздевалка: "+UniformNames[RoleIndex];
            if(Vector3.Distance(local,new Vector3(8,1,2))<3)return "E — доска сюжетных заданий";
            if(Vector3.Distance(local,new Vector3(0,1,7))<3)return insideBuilding.Role==4?"E — получить медицинскую помощь":"E — контакт фракции / сюжет";
            if(Vector3.Distance(local,new Vector3(-6,1,-3))<3)return "E — бесплатное снаряжение и аптечка";
            return "Внутри: раздевалка слева · задания справа · выход сзади";
        }
        bool TryInteriorAction()
        {
            if(!InInterior){var entrance=InteriorSystem.Near(Player.Position);if(entrance==null)return false;EnterBuilding(entrance);return true;}
            Vector3 local=Player.Position-interiorOrigin;
            if(InBank && Vector3.Distance(local,new Vector3(0,1,7))<3){Toggle("bank");return true;}
            if(InBank && ActiveMission==bankHeist && local.z>2)return true;
            if(Vector3.Distance(local,new Vector3(0,1,-8))<3){ExitBuilding();return true;}
            if((insideBuilding.Role==1||insideBuilding.Role==2)&&local.x>16){if(transportSuspect){if(local.z>0){Money+=200;ClearCustody();Notify("Задержанный помещён в камеру. +$200");}else TryInterrogate();}else Notify("Камеры заключения и допросная. Доставь задержанного в наручниках.");return true;}
            if(Vector3.Distance(local,new Vector3(-8,1,2))<3)
            {
                if(insideBuilding.Role>=0 && insideBuilding.Role!=RoleIndex){Notify("Эта раздевалка для сотрудников «"+Roles[insideBuilding.Role].Name+"». Tab — выбрать роль.");return true;}
                CasualClothes=!CasualClothes;Player.SetColor(Roles[RoleIndex].Color);Save();Notify("Надето: "+UniformName);return true;
            }
            if(insideBuilding.Role==11 && (Vector3.Distance(local,new Vector3(0,1,7))<3 || Vector3.Distance(local,new Vector3(-6,1,-3))<3)){Toggle("license-center");return true;}
            if(InStore && (Vector3.Distance(local,new Vector3(-6,1,-3))<3 || Vector3.Distance(local,new Vector3(0,1,7))<3)){Toggle("shop");return true;}
            if(Vector3.Distance(local,new Vector3(-6,1,-3))<3){Toggle((insideBuilding.Role==14 || insideBuilding.Role==9) && insideBuilding.Role==RoleIndex?"hideout":"equipment");return true;}
            if((insideBuilding.Role==10||insideBuilding.Role==17)&&(RoleIndex==10||RoleIndex==17)&&Vector3.Distance(local,new Vector3(0,1,7))<3){Toggle("laws");return true;}
            if(Vector3.Distance(local,new Vector3(8,1,2))<3 || Vector3.Distance(local,new Vector3(0,1,7))<3)
            {
                if(insideBuilding.Role==4 && Vector3.Distance(local,new Vector3(0,1,7))<3){Player.Health=100;Notify("Врач восстановил здоровье.");return true;}
                if(insideBuilding.Role>=0 && insideBuilding.Role!=RoleIndex){Notify("Для этой сюжетной линии выбери роль «"+Roles[insideBuilding.Role].Name+"» через Tab.");return true;}
                storyTab=true;Toggle("jobs");return true;
            }
            Notify(InteriorPrompt());return true;
        }
        void BuildInterior(BuildingEntrance entrance)
        {
            var root=interiorRoot.transform;Color wall=Color.Lerp(entrance.Color,Color.white,.8f),wood=new Color(.31f,.23f,.18f),steel=new Color(.22f,.29f,.32f);
            System.Action<string,Vector3,Vector3,Color> box=(n,p,s,c)=>WorldBuilder.Box(n,interiorOrigin+p,s,c,root);
            System.Action<Vector3,string> sign=(p,t)=>InteriorSystem.Label(interiorOrigin+p,t,root);
            box("Floor",new Vector3(0,-.15f,0),new Vector3(24,.3f,20),new Color(.34f,.36f,.37f));box("Ceiling",new Vector3(0,4.5f,0),new Vector3(24,.25f,20),new Color(.76f,.77f,.73f));
            box("North wall",new Vector3(0,2.1f,10),new Vector3(24,4.4f,.3f),wall);box("South wall",new Vector3(0,2.1f,-10),new Vector3(24,4.4f,.3f),wall);
            foreach(int side in new[]{-1,1})if(!(side==1 && (entrance.Role==1||entrance.Role==2)))box("Side wall",new Vector3(side*12,2.1f,0),new Vector3(.3f,4.4f,20),wall);
            for(int i=-1;i<=1;i++){var light=new GameObject("Ceiling light").AddComponent<Light>();light.transform.SetParent(root,false);light.transform.position=interiorOrigin+new Vector3(i*7,3.8f,0);light.type=LightType.Point;light.range=18;light.intensity=1.6f;light.color=new Color(1,.94f,.84f);}
            box("Exit door",new Vector3(0,1.4f,-9.75f),new Vector3(2.2f,2.8f,.1f),new Color(.2f,.55f,.35f));sign(new Vector3(0,3,-9.3f),"ВЫХОД · E");
            for(int i=0;i<4;i++)box("Uniform locker",new Vector3(-9.7f+i*.75f,1.2f,3.5f),new Vector3(.7f,2.4f,.8f),steel);
            sign(new Vector3(-8,3,3.5f),"РАЗДЕВАЛКА · E");box("Mission board",new Vector3(9,1.9f,3.5f),new Vector3(3.8f,2.3f,.2f),entrance.Color*.6f);sign(new Vector3(9,2,3.1f),"СЮЖЕТ / ПОРУЧЕНИЯ\nE — поговорить");
            box("Supply counter",new Vector3(-6,.8f,-4.2f),new Vector3(4,1.6f,1),wood);sign(new Vector3(-6,2.1f,-4.2f),"СНАРЯЖЕНИЕ · АПТЕЧКА · E");
            box("Reception desk",new Vector3(0,.8f,8.5f),new Vector3(4,1.6f,1.2f),wood);sign(new Vector3(0,3.4f,9.4f),entrance.Name);
            if(entrance.Role==1||entrance.Role==2)BuildCustodyAnnex(box,sign);
            if(entrance.Role==15)BuildCourtRoom(box,sign);
            if(entrance==StateBank.Entrance){box("Bank vault",new Vector3(8,1.6f,6),new Vector3(4,3.2f,4),steel);box("Vault door",new Vector3(7,1.5f,3.9f),new Vector3(2,2.7f,.2f),Color.gray);sign(new Vector3(7,3.8f,4),"ХРАНИЛИЩЕ · E");}
            int role=entrance.Role>=0?entrance.Role:0;var contact=new GameObject("Faction contact");contact.transform.SetParent(root,false);contact.transform.position=interiorOrigin+new Vector3(0,0,7.7f);contact.transform.rotation=Quaternion.Euler(0,180,0);CharacterModels.Create(contact.transform,role,entrance.Color,role%2==0);contact.AddComponent<NeutralActor>();var contactCollider=contact.AddComponent<CapsuleCollider>();contactCollider.height=1.8f;contactCollider.radius=.3f;contactCollider.center=Vector3.up*.9f;sign(new Vector3(0,2.5f,7.7f),entrance.Role>=0?StoryBook.Stories[role].Contact:"Администратор");
            if(entrance.ShopId>=0){for(int i=0;i<3;i++){box("Store shelf",new Vector3(-6+i*6,1,0),new Vector3(2,2,4),steel);for(int j=0;j<5;j++)box("Store products",new Vector3(-6+i*6,2.15f,-1.5f+j*.65f),new Vector3(.5f,.35f,.45f),Color.Lerp(entrance.Color,Color.white,j*.16f));}sign(new Vector3(-6,2.5f,-4.2f),"24/7 · КУПИТЬ / ПРОДАВЕЦ · E");}
            else if(role==4){for(int i=0;i<3;i++){box("Hospital bed",new Vector3(-7+i*6,.65f,-.3f),new Vector3(2.4f,.35f,3.5f),Color.white);box("Bed pillow",new Vector3(-7+i*6,.9f,.8f),new Vector3(1.8f,.18f,.65f),new Color(.65f,.82f,.85f));}sign(new Vector3(0,3,0),"ПРИЁМНОЕ ОТДЕЛЕНИЕ");}
            else if(role==2)
            {
                box("Operations table",new Vector3(0,.8f,0),new Vector3(5,1.6f,2.5f),steel);
                for(int i=-1;i<=1;i++){box("Operations monitor",new Vector3(i*1.5f,1.75f,1),new Vector3(1.2f,.8f,.15f),new Color(.12f,.32f,.42f));}
                box("Interview partition",new Vector3(5,1.3f,-5),new Vector3(.2f,2.6f,5),new Color(.3f,.4f,.45f));box("Interview desk",new Vector3(8,.7f,-6),new Vector3(2.4f,1.4f,1.6f),wood);
                box("Lab bench",new Vector3(-9,.9f,7),new Vector3(4,1.8f,1.2f),Color.white);sign(new Vector3(-8,3,7),"ЛАБОРАТОРИЯ УЛИК");sign(new Vector3(8,3,-6),"ДОПРОСНАЯ");sign(new Vector3(0,3.1f,1),"ФБР · ЦЕНТР ОПЕРАЦИЙ");
            }
            else if(role==14 || role==9)
            {
                for(int i=0;i<3;i++){box("Dorm bed",new Vector3(-7+i*5,.5f,1),new Vector3(2.1f,.5f,3.5f),steel);box("Mattress",new Vector3(-7+i*5,.82f,1),new Vector3(2,.2f,3.3f),new Color(.45f,.5f,.38f));}
                box("Sofa",new Vector3(7,.6f,-5),new Vector3(3,1.2f,1.3f),new Color(.3f,.2f,.15f));
                for(int i=0;i<4;i++){box("Stash crate",new Vector3(-9+i*1.1f,.6f,7),new Vector3(1,1.2f,1),wood);box("Gun rack",new Vector3(7+i*.6f,1.6f,7),new Vector3(.15f,1.4f,.25f),steel);}
                sign(new Vector3(-6,2.8f,-4.2f),"СХРОН · ОРУЖИЕ · ПРИПАСЫ · E");sign(new Vector3(0,3,0),"ОБЩАГ / ЖИЛАЯ КОМНАТА");
            }
            else if(role==12 || role==13){for(int i=0;i<5;i++)box("Cell bars",new Vector3(-3+i*.5f,1.4f,-1),new Vector3(.08f,2.8f,.08f),steel);box("Cell bed",new Vector3(-7,.55f,-1),new Vector3(2.3f,.3f,3.4f),steel);box("Guard desk",new Vector3(5,.7f,-2),new Vector3(3,1.4f,1.5f),wood);sign(new Vector3(0,3,0),"ПОСТ / ЖИЛАЯ СЕКЦИЯ");}
            else if(role==3 || role==1 || role==5){box("Briefing table",new Vector3(0,.8f,0),new Vector3(5,1.6f,2.5f),wood);for(int i=-1;i<=1;i++)box("Equipment rack",new Vector3(5+i*1.4f,1.25f,7),new Vector3(1,2.5f,.7f),steel);sign(new Vector3(5,3.1f,7),role==5?"СПАСАТЕЛЬНОЕ СНАРЯЖЕНИЕ":"АРСЕНАЛ / ИНСТРУКТАЖ");}
            else if(role==8 || role==7 || role==6){for(int i=0;i<3;i++)box("Tool cabinet",new Vector3(-7+i*6,.7f,0),new Vector3(2.8f,1.4f,1),entrance.Color);box("Service platform",new Vector3(0,.1f,-4),new Vector3(4,.2f,5),steel);sign(new Vector3(0,3,0),role==8?"МАСТЕРСКАЯ":"ДИСПЕТЧЕРСКАЯ");}
            else if(entrance.Role<0 && entrance.Style==0){box("Apartment sofa",new Vector3(-5,.5f,0),new Vector3(4,1,1.5f),new Color(.38f,.25f,.2f));box("Apartment bed",new Vector3(5,.5f,6),new Vector3(3,.6f,4),Color.white);box("Kitchen counter",new Vector3(-8,1,7),new Vector3(4,2,1),wood);}
            else if(entrance.Role<0 && entrance.Style==1){for(int i=0;i<4;i++)box("Shop shelving",new Vector3(-7+i*4,.9f,0),new Vector3(2,1.8f,4),steel);sign(new Vector3(0,3,0),"МАГАЗИН / ТОРГОВЫЙ ЗАЛ");}
            else{for(int i=-1;i<=1;i++){box("Office desk",new Vector3(i*6,.7f,0),new Vector3(3,1.4f,1.5f),wood);box("Office monitor",new Vector3(i*6,1.7f,.4f),new Vector3(1.1f,.7f,.15f),steel);}sign(new Vector3(0,3,0),role==10?"ЗАЛ ЗАСЕДАНИЙ":role==11?"ЭКЗАМЕНАЦИОННЫЙ КЛАСС":"РАБОЧИЙ ЗАЛ");}
        }
    }
}
