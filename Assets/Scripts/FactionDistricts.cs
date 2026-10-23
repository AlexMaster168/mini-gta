using System.Collections.Generic;
using UnityEngine;
namespace SunsetState
{
    public sealed class FacilityFeature
    {public string Name;public Vector3 Position;public Vector2 Size;public Color Color;}
    public static class FactionDistricts
    {
        public static readonly List<FacilityFeature> Features=new List<FacilityFeature>();
        static GameObject Feature(Transform parent,string name,Vector3 p,Vector3 size,Color color,bool solid=true,bool map=true)
        {var obj=WorldBuilder.Box(name,p,size,color,parent,solid);if(map)Features.Add(new FacilityFeature{Name=name,Position=p,Size=new Vector2(size.x,size.z),Color=color});return obj;}
        static void ClearOldBlocks(Transform parent,Vector3 center,float width,float depth)
        {
            foreach(var renderer in parent.GetComponentsInChildren<MeshRenderer>())
            {
                string name=renderer.name;if(name!="City building" && name!="Flat roof" && name!="Windows" && name!="Tree trunk" && name!="Tree crown")continue;
                Vector3 p=renderer.bounds.center;if(Mathf.Abs(p.x-center.x)>width*.5f || Mathf.Abs(p.z-center.z)>depth*.5f)continue;
                InteriorSystem.Entrances.RemoveAll(e=>e.Shell==renderer.gameObject);renderer.gameObject.SetActive(false);
            }
        }
        public static readonly Vector3 ArmyGate=new Vector3(630,0,755),ArmyRoad=new Vector3(630,0,600);
        public static bool InArmy(Vector3 p)=>p.x>550&&p.x<710&&p.z>755&&p.z<915;
        static void BuildArmyAccess(Transform parent,Vector3 home,float south)
        {
            float end=600;ClearOldBlocks(parent,new Vector3(home.x,0,(end+south)*.5f),26,south-end+20);
            Feature(parent,"Подъезд к военной базе",new Vector3(home.x,.10f,(end+south)*.5f),new Vector3(18,.16f,south-end+30),new Color(.19f,.23f,.25f),true);
            Feature(parent,"Связь подъезда с дорогой",new Vector3(615,.10f,600),new Vector3(48,.16f,18),new Color(.19f,.23f,.25f),true);
            Feature(parent,"Проезд КПП",new Vector3(home.x,.12f,south+25),new Vector3(14,.14f,60),new Color(.20f,.25f,.27f),true);
            for(float z=end+20;z<south+45;z+=15)Feature(parent,"Направление движения",new Vector3(home.x,.225f,z),new Vector3(.2f,.02f,5),new Color(.95f,.8f,.2f),false,false);
            foreach(int side in new[]{-1,1}){Feature(parent,"Опора въездного знака",new Vector3(home.x+side*10,2.5f,south),new Vector3(.25f,5,.25f),Color.gray);}
            Feature(parent,"Табло КПП",new Vector3(home.x,5,south),new Vector3(20,1.2f,.2f),new Color(.08f,.2f,.16f));
            UniformDetails.Mark(parent,"ВОЕННАЯ БАЗА · ВЪЕЗД",new Vector3(home.x,5,south-.13f),Quaternion.identity,8,Color.white);
            UniformDetails.Mark(parent,"ВЫЕЗД → ДОРОГА",new Vector3(home.x,5,south+.13f),Quaternion.Euler(0,180,0),8,new Color(1,.85f,.25f));
            foreach(var item in parent.GetComponentsInChildren<Transform>())if(item.name=="Supply crate"&&Vector3.Distance(item.position,home)<80)item.position=new Vector3(home.x-52+(item.GetSiblingIndex()%5)*3,1,home.z+50);else if(item.name=="Fuel barrel"&&Vector3.Distance(item.position,home)<80)item.position=new Vector3(home.x-55+(item.GetSiblingIndex()%3)*3,1.2f,home.z-20);
        }
        public static void Build(GameRoot game,Transform parent)
        {
            Features.Clear();Color asphalt=new Color(.22f,.24f,.25f),stone=new Color(.6f,.6f,.55f),steel=new Color(.18f,.22f,.24f);
            for(int role=0;role<WorldBuilder.Places.Count;role++)
            {
                if(role==13)continue;Vector3 home=WorldBuilder.Places[role].Position;home.y=0;
                bool gang=role==9||role==14,army=role==3;float width=army?160:90,depth=army?160:94;Vector3 center=home+Vector3.forward*(army?35:25);
                ClearOldBlocks(parent,center,width,depth);
                Feature(parent,"Двор / проезд",center+Vector3.up*.035f,new Vector3(width,.08f,depth),asphalt,false);
                Feature(parent,"Пешеходный вход",home+new Vector3(6,.18f,11),new Vector3(6,.1f,21),stone,false);
                float north=center.z+depth*.5f,south=center.z-depth*.5f;
                Color fence=gang?new Color(.37f,.38f,.31f):new Color(.34f,.38f,.4f);float height=gang?1.15f:2.2f;
                Feature(parent,"Периметр",new Vector3(center.x,height*.5f,north),new Vector3(width,height,.2f),fence);
                foreach(int side in new[]{-1,1})
                {
                    if(role==12 && side==1){foreach(int end in new[]{-1,1})Feature(parent,"Ограда",new Vector3(center.x+width*.5f,height*.5f,home.z+end*31),new Vector3(.2f,height,40),fence);}
                    else Feature(parent,"Ограда",new Vector3(center.x+side*width*.5f,height*.5f,center.z),new Vector3(.2f,height,depth),fence);
                    Feature(parent,"Ограда у въезда",new Vector3(center.x+side*(width*.25f+5),height*.5f,south),new Vector3(width*.5f-10,height,.2f),fence);
                }
                if(army)BuildArmyAccess(parent,home,south);
                var gate=new GameObject("КПП · "+game.Roles[role].Name);gate.transform.position=new Vector3(home.x-5,1.2f,south);gate.transform.SetParent(parent,true);var arm=Feature(gate.transform,"Шлагбаум",new Vector3(home.x,1.2f,south),new Vector3(10,.12f,.18f),Color.white);arm.AddComponent<WorldDestructible>().Initialize(100);gate.AddComponent<FactionBarrier>().Initialize(role,arm.transform);
                Feature(parent,"Пост КПП",new Vector3(home.x-12,1.4f,south+3),new Vector3(4,2.8f,4),stone);
                InteriorSystem.Label(new Vector3(home.x,3.6f,south),game.Roles[role].Name.ToUpper()+" · ВЪЕЗД",parent,.05f);
                for(int row=0;row<5;row++)
                {
                    Vector3 p=home+new Vector3(-25,.11f,row*7);Feature(parent,"Парковочное место",p,new Vector3(12,.03f,.12f),Color.white,false,false);
                    for(int side=-1;side<=1;side+=2)Feature(parent,"Разметка парковки",p+Vector3.right*side*6+Vector3.forward*3,new Vector3(.12f,.03f,6),Color.white,false,false);
                }
                Feature(parent,"Скамья",home+new Vector3(17,.48f,10),new Vector3(3,.2f,.65f),new Color(.35f,.25f,.18f));
                foreach(int side in new[]{-1,1})Feature(parent,"Стойка входа",home+new Vector3(6+side*4,.7f,19),new Vector3(.25f,1.4f,.25f),steel);
                if(role==10)
                {
                    foreach(int side in new[]{-1,1})Feature(parent,"Колонна мэрии",home+new Vector3(6+side*8,3,22),new Vector3(.8f,6,.8f),new Color(.78f,.77f,.7f));
                    for(int step=0;step<4;step++)Feature(parent,"Ступени мэрии",home+new Vector3(6,.075f+step*.075f,18+step),new Vector3(13,.15f+step*.15f,1),stone);
                    Feature(parent,"Газон мэрии",home+new Vector3(-11,.1f,8),new Vector3(12,.1f,11),new Color(.25f,.42f,.26f),false);
                }
                if(army)
                {
                    Feature(parent,"Плац",home+new Vector3(0,.1f,72),new Vector3(38,.08f,24),new Color(.36f,.38f,.36f),false);
                    var barracks=Feature(parent,"Казарма",home+new Vector3(-43,2.5f,82),new Vector3(22,5,14),new Color(.43f,.47f,.36f));barracks.AddComponent<WorldDestructible>().Initialize(600);InteriorSystem.Register(barracks,new Vector3(22,5,14),"Казарма армии",3);
                    for(int i=0;i<5;i++)Feature(parent,"Полоса препятствий",home+new Vector3(47,1.1f,30+i*8),new Vector3(4,2.2f,.25f),new Color(.35f,.3f,.22f));
                }
                else if(role==4 || role==5 || role==6 || role==7 || role==8)
                {
                    Feature(parent,"Навес служебного транспорта",home+new Vector3(-24,4,24),new Vector3(18,.25f,22),new Color(.35f,.38f,.4f));
                    foreach(int side in new[]{-1,1})Feature(parent,"Опора навеса",home+new Vector3(-24+side*8,2,24),new Vector3(.3f,4,.3f),steel);
                    InteriorSystem.Label(home+new Vector3(-24,4.8f,13),role==4?"СКОРАЯ ПОМОЩЬ":role==5?"ПОЖАРНЫЙ РАСЧЁТ":"СЛУЖЕБНЫЙ ГАРАЖ",parent,.05f);
                }
                else if(gang)
                {
                    Feature(parent,"Дворовая площадка",home+new Vector3(-18,.12f,45),new Vector3(24,.04f,16),new Color(.33f,.4f,.31f),false);
                    for(int i=0;i<4;i++)Feature(parent,"Ящик припасов",home+new Vector3(20+i*2, .7f,42),new Vector3(1.6f,1.4f,1.6f),new Color(.38f,.32f,.2f));
                    InteriorSystem.Label(home+new Vector3(6,5,22),role==9?"GROVE · СВОЙ РАЙОН":"ОБЩАГ · НЕЗАВИСИМЫЕ",parent,.075f);
                }
                else if(role==1 || role==2 || role==12)
                {
                    Feature(parent,"Площадка досмотра",home+new Vector3(-25,.13f,14),new Vector3(13,.04f,20),new Color(.28f,.35f,.39f),false);
                    for(int i=0;i<3;i++)Feature(parent,"Защитный блок",home+new Vector3(22+i*4,.6f,-10),new Vector3(2.8f,1.2f,1),stone);
                }
                for(int floor=0;floor<2;floor++)for(int window=0;window<5;window++)Feature(parent,"Окно фасада",home+new Vector3(-3+window*4,3+floor*3,23.42f),new Vector3(2.1f,1.55f,.04f),new Color(.22f,.4f,.46f),false,false);
                Feature(parent,"Мачта",home+new Vector3(22,4,9),new Vector3(.12f,8,.12f),steel);Feature(parent,"Флаг",home+new Vector3(23.5f,7.1f,9),new Vector3(3,1.6f,.08f),game.Roles[role].Color,false,false);
            }
            for(int i=0;i<8;i++){float z=-535+i*30;Feature(parent,"Освещение периметра",new Vector3(1186,5,z),new Vector3(.12f,10,.12f),steel);}
            Feature(parent,"Спортивный двор",new Vector3(1080,.13f,-470),new Vector3(46,.04f,28),new Color(.3f,.37f,.3f),false);
            for(int i=0;i<4;i++)Feature(parent,"Турник",new Vector3(1005+i*6,2.2f,-500),new Vector3(3,.12f,.12f),steel);
        }
    }
    public sealed class FactionBarrier:MonoBehaviour
    {
        int faction;Transform arm;Quaternion closed;float angle;
        public void Initialize(int role,Transform boom){faction=role;arm=boom;closed=transform.localRotation;}
        void Update(){var game=GameRoot.Instance;if(!game || !game.Playing || game.Panel!="" || !arm)return;bool open=game.RoleIndex==faction && Vector3.Distance(game.Player.Position,transform.position)<30;angle=Mathf.MoveTowards(angle,open?82:0,Time.deltaTime*60);transform.localRotation=closed*Quaternion.Euler(0,0,angle);}
    }
}
