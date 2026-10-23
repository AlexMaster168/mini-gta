using UnityEngine;

namespace SunsetState
{
    public static partial class StateBuilder
    {
        public static readonly Vector3 SanFierro=new Vector3(-2400,1,2100);
        public static readonly Vector3 LasVenturas=new Vector3(2300,1,2200);
        public static readonly Vector3[][] Routes={
            new[]{new Vector3(0,0,800),new Vector3(0,0,1100),new Vector3(-2400,0,1450),new Vector3(-2400,0,1620)},
            new[]{new Vector3(800,0,0),new Vector3(1400,0,0),new Vector3(2300,0,1480),new Vector3(2300,0,1720)},
            new[]{new Vector3(-1840,0,2100),new Vector3(1740,0,2200)}};
        public static string City(Vector3 p)=>ViceCityIsland.Contains(p)?"ВАЙС-СИТИ":Vector3.Distance(p,SanFierro)<950?"САН-ФИЕРРО":Vector3.Distance(p,LasVenturas)<1000?"ЛАС-ВЕНТУРАС":Mathf.Abs(p.x)<1250 && p.z<1100?"ЛОС-САНТОС":"ТРАССЫ SAN ORO";
        public static void Build(GameRoot game,Transform root)
        {
            City(game,root,SanFierro,false);City(game,root,LasVenturas,true);
            for(int r=0;r<Routes.Length;r++)for(int i=0;i<Routes[r].Length-1;i++)Highway(Routes[r][i],Routes[r][i+1],root,new[]{"LS — SF","LS — LV","SF — LV"}[r]);
            // An original red suspension bridge on the San Fierro route.
            Vector3 bridge=Vector3.Lerp(Routes[0][1],Routes[0][2],.56f);Vector3 direction=(Routes[0][2]-Routes[0][1]).normalized;Vector3 side=Vector3.Cross(Vector3.up,direction);
            for(int tower=-1;tower<=1;tower+=2)for(int bank=-1;bank<=1;bank+=2)
            {
                Vector3 pos=bridge+direction*tower*55+side*bank*15;
                WorldBuilder.Box("San Fierro bridge tower",pos+Vector3.up*20,new Vector3(3,40,3),new Color(.65f,.22f,.15f),root);
                var beam=WorldBuilder.Box("Bridge cable",pos+direction*30+Vector3.up*26,new Vector3(1,1,100),new Color(.6f,.24f,.16f),root,false);beam.transform.rotation=Quaternion.LookRotation(direction)*Quaternion.Euler(15*tower,0,0);
            }
            Sign(new Vector3(-18,9,25),"LOS SANTOS",root);
            Sign(SanFierro+new Vector3(0,9,35),"SAN FIERRO",root);
            Sign(LasVenturas+new Vector3(0,9,35),"LAS VENTURAS",root);
            BuildExpansion(game,root);
        }
        static void Highway(Vector3 a,Vector3 b,Transform root,string title)
        {
            Vector3 delta=b-a;delta.y=0;Vector3 center=(a+b)/2;
            var road=WorldBuilder.Box(title,center+Vector3.up*.065f,new Vector3(26,.12f,delta.magnitude),new Color(.19f,.21f,.22f),root);road.transform.rotation=Quaternion.LookRotation(delta);
            var line=WorldBuilder.Box("Highway center line",center+Vector3.up*.135f,new Vector3(.25f,.02f,delta.magnitude),new Color(.95f,.79f,.3f),root,false);line.transform.rotation=road.transform.rotation;
            Vector3 side=Vector3.Cross(Vector3.up,delta.normalized);
            for(float distance=300;distance<delta.magnitude-300;distance+=450)
            {
                var location=a+delta.normalized*distance+side*22;
                Sign(location+Vector3.up*6,title+"  →",root);
            }
        }
        static void City(GameRoot game,Transform root,Vector3 origin,bool desert)
        {
            Color ground=desert?new Color(.7f,.61f,.42f):new Color(.4f,.48f,.38f);
            WorldBuilder.Box(desert?"Venturas desert":"San Fierro district",origin+Vector3.down*.98f,new Vector3(1700,.1f,1700),ground,root);
            for(int i=-3;i<=3;i++)
            {
                float p=i*160;
                WorldBuilder.Box("City avenue",origin+new Vector3(p,-.93f,0),new Vector3(24,.13f,1120),new Color(.19f,.21f,.22f),root);
                WorldBuilder.Box("City avenue",origin+new Vector3(0,-.92f,p),new Vector3(1120,.13f,24),new Color(.19f,.21f,.22f),root);
                for(int side=-1;side<=1;side+=2)
                {
                    WorldBuilder.Box("City sidewalk",origin+new Vector3(p+side*16,-.8f,0),new Vector3(6,.3f,1100),new Color(.57f,.56f,.5f),root);
                    WorldBuilder.Box("City sidewalk",origin+new Vector3(0,-.8f,p+side*16),new Vector3(1100,.3f,6),new Color(.57f,.56f,.5f),root);
                }
            }
            for(int x=-3;x<3;x++)for(int z=-3;z<3;z++)
            {
                var center=origin+new Vector3(x*160+80,-1,z*160+80);
                for(int dx=-1;dx<=1;dx+=2)for(int dz=-1;dz<=1;dz+=2)
                {
                    Vector3 p=center+new Vector3(dx*30,0,dz*30);float height=desert?Random.Range(16,60):Random.Range(8,35);
                    Color color=desert?Color.Lerp(new Color(.7f,.59f,.4f),new Color(.49f,.31f,.3f),Random.value):Color.Lerp(new Color(.56f,.61f,.62f),new Color(.64f,.6f,.5f),Random.value);
                    WorldBuilder.Box(desert?"Casino district building":"San Fierro townhouse",p+Vector3.up*height/2,new Vector3(35,height,35),color,root);
                    WorldBuilder.Box("Glass facade",p+new Vector3(0,height*.55f,17.6f),new Vector3(28,height*.6f,.1f),new Color(.21f,.36f,.41f),root,false);
                    if(desert && Mathf.Abs(x)<2)
                    {
                        WorldBuilder.Box("Casino neon",p+new Vector3(0,height+1,0),new Vector3(36,1,36),Color.HSVToRGB(Random.value,.65f,.95f),root,false);
                        Sign(p+new Vector3(0,height+6,18),new[]{"OASIS","ROYAL SUN","LUCKY STAR","MIRAGE BAY"}[Random.Range(0,4)],root);
                    }
                }
            }
            if(!desert)
            {
                WorldBuilder.Box("San Fierro harbour",origin+new Vector3(-670,-.8f,0),new Vector3(220,.4f,850),new Color(.36f,.39f,.4f),root);
                for(int i=0;i<18;i++)WorldBuilder.Box("Harbour container",origin+new Vector3(-660+(i%3)*22,2,(i/3)*48-150),new Vector3(12,5,30),Color.HSVToRGB(i*.13f,.5f,.65f),root);
                for(int i=0;i<5;i++)
                {
                    var hill=GameObject.CreatePrimitive(PrimitiveType.Sphere);hill.name="San Fierro hills";hill.transform.SetParent(root);hill.transform.position=origin+new Vector3(-650+i*310,-20,780);hill.transform.localScale=new Vector3(350,150+i*15,350);hill.GetComponent<Renderer>().sharedMaterial=WorldBuilder.Mat(new Color(.35f,.45f,.31f));
                }
            }
            else
            {
                for(int i=-5;i<=5;i++)
                {
                    Vector3 p=origin+new Vector3(i*100,0,650);
                    WorldBuilder.Box("Desert cactus",p+Vector3.up*3,new Vector3(1.2f,6,1.2f),new Color(.3f,.4f,.22f),root);
                }
            }
            for(int i=0;i<8;i++)WorldBuilder.SpawnCar(game,origin+new Vector3((i%3-1)*160+6,0,(i/3-1)*160+35),"Городской автомобиль",Color.HSVToRGB(Random.value,.35f,.75f),true,0);
            WorldBuilder.SpawnCar(game,origin+new Vector3(-7,0,12),desert?"Venturas Sport":"Fierro Roadster",desert?new Color(.85f,.65f,.25f):new Color(.25f,.55f,.7f),false,90,desert?VehicleKind.Sport:VehicleKind.Motorcycle);
            var heli=WorldBuilder.SpawnCar(game,origin+new Vector3(0,0,-35),"Городской Sparrow",new Color(.7f,.7f,.68f),false,0,VehicleKind.Helicopter);
        }
        static void Sign(Vector3 position,string text,Transform parent)
        {
            var obj=new GameObject(text);obj.transform.position=position;obj.transform.SetParent(parent);obj.transform.rotation=Quaternion.Euler(0,180,0);
            var label=obj.AddComponent<TextMesh>();label.text=text;label.fontSize=50;label.characterSize=.13f;label.anchor=TextAnchor.MiddleCenter;label.color=new Color(.9f,.96f,.85f);
            WorldBuilder.SetFont(label);
        }
    }
}
