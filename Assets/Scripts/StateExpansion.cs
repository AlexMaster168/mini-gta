using UnityEngine;
namespace SunsetState
{
    public static partial class StateBuilder
    {
        public static readonly Vector3 NorthDistrict=new Vector3(-1500,1,4800),EastPort=new Vector3(4300,1,1000),DesertAirfield=new Vector3(2600,1,5200),WestVillage=new Vector3(-3800,1,4600);
        public static readonly Vector3[] ExpansionCenters={NorthDistrict,EastPort,DesertAirfield,WestVillage};
        public static readonly string[] ExpansionNames={"СЕВЕРНЫЙ ОКРУГ","ВОСТОЧНЫЙ ПРОМПОРТ","ПУСТЫННЫЙ АЭРОДРОМ","ЗАПАДНЫЙ ПОСЁЛОК"};
        public static readonly Vector3[][] ExpansionRoutes={
            new[]{SanFierro,NorthDistrict},new[]{NorthDistrict,WestVillage},new[]{NorthDistrict,DesertAirfield},new[]{LasVenturas,DesertAirfield},new[]{LasVenturas,EastPort},new[]{EastPort,new Vector3(1400,0,0)}};
        static void BuildExpansion(GameRoot game,Transform root)
        {
            for(int r=0;r<ExpansionRoutes.Length;r++)Highway(ExpansionRoutes[r][0],ExpansionRoutes[r][1],root,"Окружная трасса "+(r+1));
            for(int d=0;d<ExpansionCenters.Length;d++)
            {
                Vector3 center=ExpansionCenters[d];Color facade=d==1?new Color(.48f,.49f,.45f):d==2?new Color(.65f,.59f,.45f):new Color(.62f,.58f,.48f);
                for(int lane=-1;lane<=1;lane++)
                {
                    WorldBuilder.Box("District road",center+new Vector3(lane*160,-.92f,0),new Vector3(22,.12f,850),new Color(.2f,.22f,.23f),root);
                    WorldBuilder.Box("District road",center+new Vector3(0,-.91f,lane*160),new Vector3(850,.12f,22),new Color(.2f,.22f,.23f),root);
                }
                for(int x=-2;x<2;x++)for(int z=-2;z<2;z++)
                {
                    Vector3 p=center+new Vector3(x*160+80,-1,z*160+80);float height=d==1?14:d==2?7:5;
                    WorldBuilder.Box("City building",p+Vector3.up*height/2,new Vector3(d==1?65:38,height,d==1?48:34),facade,root);
                    WorldBuilder.Box("District roof",p+Vector3.up*(height+.15f),new Vector3(d==1?67:40,.3f,d==1?50:36),facade*.65f,root);
                }
                Sign(center+new Vector3(0,8,-30),ExpansionNames[d],root);
                for(int i=0;i<3;i++)WorldBuilder.SpawnCar(game,center+new Vector3(6,0,40+i*60),"Окружной транспорт",facade,true,0,i==0?VehicleKind.Truck:VehicleKind.Sedan);
            }
            WorldBuilder.Box("Northern airfield runway",DesertAirfield+new Vector3(500,-.9f,0),new Vector3(45,.15f,1200),new Color(.27f,.29f,.3f),root);
            for(int i=-5;i<=5;i++)WorldBuilder.Box("Airfield markings",DesertAirfield+new Vector3(500,-.8f,i*95),new Vector3(1,.04f,25),Color.white,root,false);
            WorldBuilder.SpawnCar(game,DesertAirfield+new Vector3(500,1,100),"Окружной вертолёт",Color.gray,false,0,VehicleKind.Helicopter);
        }
        public static bool ExpansionRoad(Vector2 p)
        {
            foreach(var route in ExpansionRoutes){Vector2 a=new Vector2(route[0].x,route[0].z),b=new Vector2(route[1].x,route[1].z),delta=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,delta)/delta.sqrMagnitude);if(Vector2.Distance(p,a+delta*t)<15)return true;}
            foreach(var center in ExpansionCenters){float x=p.x-center.x,z=p.y-center.z;if(Mathf.Abs(x)<425 && Mathf.Abs(z)<425 && (Mathf.Abs(x-Mathf.Round(x/160)*160)<11 || Mathf.Abs(z-Mathf.Round(z/160)*160)<11))return true;}
            return false;
        }
        public static Vector3 GarageCenter(Vector3 p)
        {if(ViceCityIsland.Contains(p))return ViceCityIsland.Center;Vector3 best=Vector3.zero;float distance=Vector3.Distance(p,best);foreach(var c in new[]{SanFierro,LasVenturas,NorthDistrict,EastPort,DesertAirfield,WestVillage})if(Vector3.Distance(p,c)<distance){distance=Vector3.Distance(p,c);best=c;}return best;}
    }
}
