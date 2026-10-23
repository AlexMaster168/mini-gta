using System.Collections.Generic;
using UnityEngine;
namespace SunsetState
{
    public sealed class WaterBody
    {
        public Rect Bounds;public float Surface;public Vector3 Shore;
        static readonly List<WaterBody> bodies=new List<WaterBody>();
        public static WaterBody At(Vector3 p){if(Mathf.Abs(p.x-ViceCityIsland.Center.x)<800&&Mathf.Abs(p.z-ViceCityIsland.Center.z)<900)return null;foreach(var body in bodies)if(body.Bounds.Contains(new Vector2(p.x,p.z)))return body;return null;}
        public static void BuildGround(Transform root,Color color)
        {
            bodies.Clear();
            var holes=new[]{new Rect(-1350,-1250,420,2500),new Rect(3150,4100,2300,2600)};
            var pieces=new List<Rect>{new Rect(-6000,-3250,12000,11000)};
            foreach(var hole in holes)
            {var next=new List<Rect>();foreach(var rect in pieces){if(!rect.Overlaps(hole)){next.Add(rect);continue;}float left=Mathf.Max(rect.xMin,hole.xMin),right=Mathf.Min(rect.xMax,hole.xMax),bottom=Mathf.Max(rect.yMin,hole.yMin),top=Mathf.Min(rect.yMax,hole.yMax);
                if(left>rect.xMin)next.Add(Rect.MinMaxRect(rect.xMin,rect.yMin,left,rect.yMax));if(right<rect.xMax)next.Add(Rect.MinMaxRect(right,rect.yMin,rect.xMax,rect.yMax));if(bottom>rect.yMin)next.Add(Rect.MinMaxRect(left,rect.yMin,right,bottom));if(top<rect.yMax)next.Add(Rect.MinMaxRect(left,top,right,rect.yMax));}pieces=next;}
            foreach(var rect in pieces)WorldBuilder.Box("County ground",new Vector3(rect.center.x,-2,rect.center.y),new Vector3(rect.width,4,rect.height),color,root);
            foreach(var hole in holes)WorldBuilder.Box("Seabed",new Vector3(hole.center.x,-12.2f,hole.center.y),new Vector3(hole.width,.4f,hole.height),new Color(.32f,.3f,.2f),root);
            bodies.Add(new WaterBody{Bounds=holes[0],Surface=.4f,Shore=new Vector3(-890,1,200)});
            bodies.Add(new WaterBody{Bounds=holes[1],Surface=.125f,Shore=ViceCityIsland.Center+new Vector3(700,0,200)});
        }
    }
    public sealed class VehicleWaterState:MonoBehaviour
    {
        VehicleMotor car;public bool Flooded {get;private set;}
        void Awake(){car=GetComponent<VehicleMotor>();}
        void FixedUpdate()
        {
            var water=WaterBody.At(transform.position);bool immersed=water!=null&&(transform.position.y<water.Surface-.35f||car.OnWater&&car.Durability<=0);
            if(car.OnWater&&car.Durability>0)return;
            if(immersed){if(!Flooded&&car.Driver)GameRoot.Instance.Notify("Машина затоплена! E — выбраться · F7 — спасение / эвакуация");Flooded=true;car.Traffic=false;car.Body.useGravity=true;car.Body.constraints&=~RigidbodyConstraints.FreezePositionY;car.Body.linearDamping=2.2f;}
            else if(Flooded&&water==null){Flooded=false;car.Body.linearDamping=.035f;}
        }
    }
    public sealed partial class GameRoot
    {
        float breath=15;VehicleMotor submergedVehicle;
        public bool Swimming=>Player&&!Player.Vehicle&&WaterBody.At(Player.Position)!=null&&Player.Position.y<WaterBody.At(Player.Position).Surface;
        void UpdateWater()
        {
            var water=WaterBody.At(Player.Position);bool underwater=water!=null&&Player.Position.y+(Player.Vehicle?1.05f:1.6f)<water.Surface;
            if(Player.Vehicle&&water!=null&&Player.Vehicle.transform.position.y<water.Surface-.3f)submergedVehicle=Player.Vehicle;
            breath=underwater?Mathf.Max(0,breath-Time.deltaTime):Mathf.Min(15,breath+Time.deltaTime*4);
            if(underwater&&breath<=0)Player.Health=Mathf.Max(0,Player.Health-Time.deltaTime*14);
        }
        void EvacuateWater(bool tow)
        {
            var car=Player.Vehicle?Player.Vehicle:submergedVehicle;var water=WaterBody.At(car?car.transform.position:Player.Position);
            if(water==null){Notify("Рядом нет затонувшего транспорта или водоёма.");return;}
            if(tow&&car&&Money<300){Notify("Подъём транспорта стоит $300. Спасение человека бесплатно.");return;}
            Player.Teleport(water.Shore);breath=15;
            if(tow&&car){Vector3 safe=SpawnSafety.VehiclePosition(water.Shore+Vector3.forward*12,car.Dimensions,0,car);car.transform.SetPositionAndRotation(safe+Vector3.up*.4f,Quaternion.identity);car.Body.position=car.transform.position;car.Body.rotation=car.transform.rotation;car.Body.linearVelocity=Vector3.zero;car.Body.angularVelocity=Vector3.zero;car.Body.constraints&=~RigidbodyConstraints.FreezePositionY;if(car.Durability>0)car.Durability=Mathf.Max(35,car.Durability);Money-=300;submergedVehicle=null;}
            Panel="";Time.timeScale=1;SetCursor(true);Notify(tow?"Спасатели эвакуировали тебя и подняли транспорт на берег. $300.":"Спасатели доставили тебя на берег. Машина остаётся в воде.");Save();
        }
        void DrawWaterRescue(float left,float h)
        {Label(left,90,1040,50,"СПАСАТЕЛЬНАЯ СЛУЖБА · ВОДА",heading);Label(left,170,1040,95,"E — выйти из затонувшей машины. На воде WASD — плыть, Пробел — всплыть. Под водой запас воздуха 15 секунд, затем здоровье уменьшается. Спасение человека бесплатно, подъём машины — $300.",body);
            if(Button(new Rect(left,320,1040,70),"СПАСТИ ПЕРСОНАЖА · БЕСПЛАТНО"))EvacuateWater(false);if(Button(new Rect(left,420,1040,70),"СПАСТИ И ПОДНЯТЬ ТРАНСПОРТ · $300"))EvacuateWater(true);}
    }
}
