using System.Collections.Generic;
using UnityEngine;

namespace SunsetState
{
    public sealed class Place
    {
        public string Name; public Vector3 Position; public Color Color; public int Role;
        public Place(string name, float x, float z, Color color, int role = -1) { Name = name; Position = new Vector3(x, 1, z); Color = color; Role = role; }
    }
    public static class WorldBuilder
    {
        static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        public static readonly List<Place> Places = new List<Place>();
        public static Material Mat(Color color)
        {
            if (!materials.TryGetValue(color, out Material material))
            {
                material = new Material(Resources.Load<Material>("CityBase")); material.color = color;
                material.SetFloat("_Glossiness", .12f); materials[color] = material;
            }
            return material;
        }
        public static GameObject Box(string name, Vector3 position, Vector3 scale, Color color, Transform parent = null, bool collider = true)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
            obj.transform.position = position; obj.transform.localScale = scale;
            if (parent) obj.transform.SetParent(parent, true);
            obj.GetComponent<Renderer>().sharedMaterial = Mat(color);
            if(collider && (name=="City building" || name=="Casino district building" || name=="San Fierro townhouse" || name=="Cell block" || name=="Guard tower"))InteriorSystem.Register(obj,scale,name=="City building"?"Городское здание":name=="Cell block"?"Тюремный корпус":name=="Guard tower"?"Пост охраны":name=="San Fierro townhouse"?"Дом в Сан-Фиерро":"Казино Лас-Вентураса",name=="Cell block"?13:name=="Guard tower"?12:-1);
            if(collider)WorldDestructible.Configure(obj,name,scale);
            if (!collider) { obj.GetComponent<Collider>().enabled=false;Object.Destroy(obj.GetComponent<Collider>()); obj.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; }
            return obj;
        }
        public static void Build(GameRoot game)
        {
            Random.InitState(1979); Places.Clear();InteriorSystem.Entrances.Clear();
            var root = new GameObject("San Oro County — procedural world").transform;
            var ground = new Color(.47f, .51f, .35f);
            WaterBody.BuildGround(root,ground);GameRoot.BuildStopSigns(root);
            Box("Pacific water", new Vector3(-1140, -.1f, 0), new Vector3(420, 1, 2500), new Color(.14f, .43f, .53f), root,false);
            Box("West beach", new Vector3(-900, .02f, 0), new Vector3(90, .1f, 2200), new Color(.82f, .74f, .52f), root);
            var asphalt = new Color(.18f, .2f, .21f);
            var sidewalk = new Color(.56f, .56f, .51f);
            for (int i = -4; i <= 4; i++)
            {
                float p = i * 200;
                Box("Street N-S " + i, new Vector3(p, .015f, 0), new Vector3(26, .1f, 1820), asphalt, root);
                Box("Street E-W " + i, new Vector3(0, .02f, p), new Vector3(1820, .1f, 26), asphalt, root);
                for (int side = -1; side <= 1; side += 2)
                {
                    Box("Sidewalk", new Vector3(p + side * 17, .15f, 0), new Vector3(7, .3f, 1810), sidewalk, root);
                    Box("Sidewalk", new Vector3(0, .15f, p + side * 17), new Vector3(1810, .3f, 7), sidewalk, root);
                }
                for (int d = -880; d <= 880; d += 16)
                {
                    if (Mathf.Abs(d - Mathf.Round(d / 200f) * 200) < 22) continue;
                    Box("Lane marking", new Vector3(p, .085f, d), new Vector3(.23f, .02f, 6), new Color(.94f,.79f,.34f), root, false);
                    Box("Lane marking", new Vector3(d, .09f, p), new Vector3(6, .02f, .23f), new Color(.94f,.79f,.34f), root, false);
                }
            }
            for (int x = -4; x < 4; x++) for (int z = -4; z < 4; z++)
            {
                Vector3 center = new Vector3(x * 200 + 100, 0, z * 200 + 100);
                bool park = (x == -2 && z == 0) || (x == 1 && z == 2) || (x == -4 && z == -1);
                if (park)
                {
                    Box("Park lawn", center + Vector3.up * .2f, new Vector3(150, .4f, 150), new Color(.3f,.48f,.28f), root);
                    Box("Park path", center + Vector3.up * .44f, new Vector3(8,.05f,150), sidewalk, root);
                    for (int t = 0; t < 16; t++) Tree(center + new Vector3(Random.Range(-65,65),0,Random.Range(-65,65)), root);
                    continue;
                }
                for (int bx = -1; bx <= 1; bx += 2) for (int bz = -1; bz <= 1; bz += 2)
                {
                    Vector3 pos = center + new Vector3(bx * 41, 0, bz * 41);
                    bool downtown = Mathf.Abs(center.x) < 320 && Mathf.Abs(center.z) < 320;
                    float height = downtown ? Random.Range(25,80) : Random.Range(7,23);
                    Color facade = Color.Lerp(new Color(.45f,.48f,.45f), new Color(.76f,.66f,.51f),Random.value);
                    float width = Random.Range(29,48), depth = Random.Range(28,47);
                    Box("City building",pos+Vector3.up*height/2,new Vector3(width,height,depth),facade,root);
                    Box("Flat roof",pos+Vector3.up*(height+.4f),new Vector3(width+1,.8f,depth+1),new Color(.27f,.31f,.3f),root);
                    // Window strips keep the city legible without thousands of individual windows.
                    for (float floor = 3; floor < height-1; floor += 5)
                    {
                        var glass = new Color(.23f,.39f,.43f);
                        Box("Windows",pos+new Vector3(0,floor,depth/2+.03f),new Vector3(width*.8f,1.8f,.08f),glass,root,false);
                        Box("Windows",pos+new Vector3(width/2+.03f,floor,0),new Vector3(.08f,1.8f,depth*.8f),glass,root,false);
                    }
                }
                for (int t = 0; t < 3; t++) Tree(center + new Vector3(-74,0,-65+t*58),root);
            }
            Places.Add(new Place("Центр занятости", 30, 0, new Color(.83f,.98f,.46f), 0));
            Places.Add(new Place("Полицейский департамент", -370, 200, new Color(.3f,.6f,1), 1));
            Places.Add(new Place("Федеральное бюро", 230, 400, new Color(.36f,.48f,.8f), 2));
            Places.Add(new Place("Военная база", 630, 800, new Color(.45f,.57f,.3f), 3));
            Places.Add(new Place("Центральная больница", 430, 0, new Color(1,.4f,.4f), 4));
            Places.Add(new Place("Пожарная часть", -170, -400, new Color(1,.55f,.2f), 5));
            Places.Add(new Place("Таксопарк", 30, -200, new Color(1,.83f,.25f), 6));
            Places.Add(new Place("Транспортная компания", 630, -600, new Color(.65f,.75f,.86f), 7));
            Places.Add(new Place("Автомастерская", -570, -200, new Color(.3f,.84f,.78f), 8));
            Places.Add(new Place("Район Grove", -570, -600, new Color(.43f,.8f,.36f), 9));
            Places.Add(new Place("Мэрия San Oro", 30, 400, new Color(.8f,.67f,1), 10));
            Places.Add(new Place("Центр лицензирования", -370, -400, new Color(.4f,.88f,1), 11));
            Places.Add(new Place("Администрация тюрьмы", 930, -400, new Color(.6f,.68f,.78f), 12));
            Places.Add(new Place("Тюрьма San Oro", 1050, -400, new Color(1,.55f,.22f), 13));
            Places.Add(new Place("Общаг бандита", -770, -400, new Color(.9f,.35f,.33f), 14));
            Places.Add(new Place("Суд и адвокатская коллегия", 230, -600, new Color(.55f,.72f,.87f), 15));
            Places.Add(new Place("Особняк мафии", -170, 600, new Color(.6f,.35f,.7f), 16));
            Places.Add(new Place("Резиденция губернатора", 30, 600, new Color(.85f,.7f,.35f), 17));
            PrisonBuilder.Build(game,root);
            StateBuilder.Build(game,root);
            foreach (Place place in Places)
            {
                var headquarters=Box(place.Name,place.Position+new Vector3(6,3.5f,32),new Vector3(23,9,17),place.Color*.65f,root);
                InteriorSystem.Register(headquarters,new Vector3(23,9,17),place.Name,place.Role,place.Position);
                Box("Entrance marker",place.Position+Vector3.up*.1f,new Vector3(5,.15f,5),place.Color,root,false);
                var sign = new GameObject(place.Name+" sign"); sign.transform.position = place.Position+new Vector3(0,7,20);
                var label = sign.AddComponent<TextMesh>(); label.text = place.Name; label.fontSize = 50; label.characterSize = .1f;
                SetFont(label);
                label.anchor = TextAnchor.MiddleCenter; label.color = Color.white; sign.transform.rotation = Quaternion.identity;
                sign.transform.SetParent(root);
            }
            BuildBaseCamp(game,root,new Vector3(630,0,800));
            for (int i = 0; i < Places.Count; i++)
            {
                Place place = Places[i];
                var fleet=FleetCatalog.ForRole(i);
                for(int j=0;j<fleet.Length;j++)
                {
                    var entry=fleet[j];Vector3 parking=i==3?place.Position+new Vector3(-48+j%3*17,0,22+j/3*15):place.Position+new Vector3(j*17,0,-6);
                    if(entry.Kind==VehicleKind.Helicopter){parking=new Vector3(Mathf.Round(place.Position.x/200)*200,1,place.Position.z+55);Box("Helipad",parking+Vector3.down*.8f,new Vector3(17,.2f,17),new Color(.25f,.3f,.28f),root);Box("H marking",parking+Vector3.down*.65f,new Vector3(1,.05f,7),Color.white,root,false);}
                    var car=SpawnCar(game,parking,entry.Name,place.Color,false,90,entry.Kind);car.Faction=i;
                    AddServiceDetails(car,i);
                }
            }
            Box("Marina pier",new Vector3(-963,.5f,200),new Vector3(110,.7f,9),new Color(.4f,.3f,.2f),root);
            SpawnCar(game,new Vector3(-1020,1,210),"Coastline · катер",Color.white,false,0,VehicleKind.Boat);
            SpawnCar(game,new Vector3(10,1,-10),"Личный Sultan",new Color(.72f,.32f,.2f),false,90,VehicleKind.Sport);
            SpawnCar(game,new Vector3(-10,1,-8),"Личный Roadster",new Color(.2f,.22f,.26f),false,90,VehicleKind.Motorcycle);
            for (int i = 0; i < 28; i++)
            {
                int lane = Random.Range(-4,5); float along = Random.Range(-780f,780f);
                bool vertical = i%2==0;
                SpawnCar(game,vertical ? new Vector3(lane*200+6,1,along) : new Vector3(along,1,lane*200-6),"Городской автомобиль",Color.HSVToRGB(Random.value,.35f,.75f),true,vertical ? 0:90);
            }
            for (int i = 0; i < 65; i++)
            {
                int road = Random.Range(-4,5); float along = Random.Range(-790f,790f);
                var npc = new GameObject("Горожанин");
                npc.transform.position = new Vector3(road*200+17,.3f,along);
                var capsule=npc.AddComponent<CapsuleCollider>();capsule.height=1.8f;capsule.radius=.3f;capsule.center=Vector3.up*.9f;
                CharacterModels.Create(npc.transform,i%3==0?10:0,Color.HSVToRGB(Random.value,.4f,.7f),i%2==0,i%3);
                npc.AddComponent<Pedestrian>();
            }
            for (int i = 0; i < 22; i++)
            {
                var hill = GameObject.CreatePrimitive(PrimitiveType.Sphere); hill.name = "Desert mountain";
                hill.transform.position = new Vector3(-850+i*100,-15,-1550+Random.Range(0,180));
                hill.transform.localScale = new Vector3(240,Random.Range(100,230),230);
                hill.GetComponent<Renderer>().sharedMaterial = Mat(new Color(.61f,.53f,.38f));
            }
            ConvenienceStores.Build(root);StateBank.Build(root);ViceCityIsland.Build(game,root);
            FactionDistricts.Build(game,root);BlackMarket.Build(root);
            RoadServiceObject.Build(root);
            BuildAirport(root);
            BuildPalms(root);
            EnemyOutpost.Build(game,root);
            PatrolDrone.Spawn(Places[1].Position+Vector3.up*20,1);PatrolDrone.Spawn(Places[2].Position+Vector3.up*24,2);PatrolDrone.Spawn(StateBuilder.EastPort+Vector3.up*22,2);
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type=LightType.Directional;
            sun.transform.rotation=Quaternion.Euler(48,-35,0); sun.intensity=1.15f; sun.shadows=LightShadows.Soft; game.Sun=sun;
            RenderSettings.ambientLight = new Color(.58f,.65f,.68f);
            RenderSettings.fog=true; RenderSettings.fogColor=new Color(.66f,.75f,.75f); RenderSettings.fogMode=FogMode.ExponentialSquared; RenderSettings.fogDensity=.00035f;
            Camera.main.backgroundColor=RenderSettings.fogColor; Camera.main.farClipPlane=3500;
            WorldDestructible.BindWorld(root);
            Physics.SyncTransforms();foreach(var car in game.Cars)if(car && !car.OnWater)car.GetComponent<VehicleRecovery>().ValidateSpawn();
            var staticObjects=new List<GameObject>();foreach(var filter in root.GetComponentsInChildren<MeshFilter>())if(!filter.GetComponent<TextMesh>())staticObjects.Add(filter.gameObject);StaticBatchingUtility.Combine(staticObjects.ToArray(),root.gameObject);
        }
        public static VehicleMotor SpawnCar(GameRoot game,Vector3 position,string name,Color color,bool traffic,float yaw,VehicleKind kind=VehicleKind.Sedan)
        {
            if(kind!=VehicleKind.Boat && kind!=VehicleKind.Helicopter)position=SpawnSafety.VehiclePosition(position,kind==VehicleKind.Bus?new Vector3(3,3.5f,12.3f):kind==VehicleKind.Tank?new Vector3(3.6f,2.4f,7.6f):kind==VehicleKind.Truck?new Vector3(3.1f,3.4f,9.3f):(kind==VehicleKind.Motorcycle || kind==VehicleKind.Bicycle)?new Vector3(.8f,1.4f,2.2f):new Vector3(2.3f,1.9f,5.2f),yaw);
            var detailed=RealVehicleFactory.Spawn(game,position,name,color,traffic,yaw,kind);if(detailed)return detailed;
            var root=new GameObject(name);root.transform.position=new Vector3(position.x,kind==VehicleKind.Boat?.7f:position.y+.4f,position.z);root.transform.rotation=Quaternion.Euler(0,yaw,0);
            var glass=new Color(.13f,.26f,.3f);var dark=new Color(.08f,.1f,.11f);
            System.Func<string,Vector3,Vector3,Color,bool,GameObject> part=(label,pos,scale,tint,solid)=>
            {
                var obj=Box(label,Vector3.zero,scale,tint,null,solid);obj.transform.SetParent(root.transform,false);obj.transform.localPosition=pos;return obj;
            };
            float length=4.2f,width=1.9f,height=.75f;
            if(kind==VehicleKind.Bicycle)
            {
                width=.65f;length=2.1f;
                System.Action<string,Vector3,Vector3> tube=(label,a,b)=>{var obj=part(label,(a+b)*.5f,new Vector3(.065f,.065f,Vector3.Distance(a,b)),color,false);obj.transform.localRotation=Quaternion.LookRotation(b-a);};
                Vector3 rear=new Vector3(0,.35f,-.7f),front=new Vector3(0,.35f,.7f),pedal=new Vector3(0,.4f,-.05f),seat=new Vector3(0,1.05f,-.3f),head=new Vector3(0,1.05f,.55f);
                tube("Chain stay",rear,pedal);tube("Seat stay",rear,seat);tube("Seat tube",seat,pedal);tube("Top tube",seat,head);tube("Down tube",pedal,head);tube("Fork",front,head);
                part("Saddle",seat,new Vector3(.24f,.1f,.4f),dark,false);part("Handlebar",head+Vector3.up*.12f,new Vector3(.65f,.07f,.08f),dark,false);part("Pedal crank",pedal,new Vector3(.48f,.08f,.08f),dark,false);
                var rider=CharacterModels.Create(root.transform,0,color);rider.localPosition=new Vector3(0,.15f,-.25f);rider.localScale*=.85f;CharacterModels.FreezePose(rider,true);rider.gameObject.AddComponent<RiderVisual>();
            }
            else if(kind==VehicleKind.Motorcycle)
            {
                width=.55f;length=2.2f;
                part("Fuel tank",new Vector3(0,.8f,.1f),new Vector3(.55f,.55f,1.35f),color,true);
                part("Seat",new Vector3(0,1.07f,-.5f),new Vector3(.55f,.15f,.8f),dark,false);
                part("Front fork",new Vector3(0,.65f,.86f),new Vector3(.12f,1,.14f),new Color(.65f,.68f,.7f),false);
                part("Handlebar",new Vector3(0,1.22f,.7f),new Vector3(.95f,.1f,.13f),dark,false);
                // A visible rider belongs to the vehicle, while the walking avatar is hidden.
                var rider=part("Rider",new Vector3(0,1.7f,-.25f),new Vector3(.5f,.8f,.45f),color*.5f,false);rider.AddComponent<RiderVisual>();
            }
            else if(kind==VehicleKind.Helicopter)
            {
                width=2.7f;length=6;
                part("Fuselage",new Vector3(0,1.5f,0),new Vector3(2.5f,1.8f,4.8f),color,true);
                part("Cockpit",new Vector3(0,1.75f,1.9f),new Vector3(2.3f,1.25f,1.2f),glass,true);
                part("Tail boom",new Vector3(0,1.5f,-4.2f),new Vector3(.55f,.6f,4.2f),color,true);
                part("Tail fin",new Vector3(0,2.3f,-5.8f),new Vector3(.2f,2,1),color,false);
                for(int side=-1;side<=1;side+=2){part("Skid",new Vector3(side*1.5f,.2f,0),new Vector3(.18f,.22f,5.5f),dark,true);part("Skid strut",new Vector3(side*1.2f,.7f,0),new Vector3(.12f,1,.16f),dark,false);}
            }
            else if(kind==VehicleKind.Boat)
            {
                width=2.6f;length=6.5f;
                part("Hull",new Vector3(0,.25f,0),new Vector3(2.6f,.75f,6.5f),color,true);
                part("Deck",new Vector3(0,.7f,0),new Vector3(2.25f,.2f,5.8f),new Color(.72f,.61f,.41f),false);
                part("Windscreen",new Vector3(0,1.25f,.8f),new Vector3(2,.65f,.18f),glass,true);
                part("Cabin",new Vector3(0,1,-.6f),new Vector3(1.4f,.6f,1.8f),color,false);
            }
            else if(kind==VehicleKind.Tank)
            {
                width=3.4f;length=6.8f;height=1f;
                part("Hull",new Vector3(0,.85f,0),new Vector3(2.6f,.9f,6.4f),color,true);
                part("Glacis",new Vector3(0,.6f,2.9f),new Vector3(2.5f,.5f,.9f),color,true);
                part("Deck plate",new Vector3(0,1.35f,-.1f),new Vector3(2.7f,.18f,6),color,false);
                part("Engine deck",new Vector3(0,1.5f,-2.4f),new Vector3(2.3f,.35f,2),color,false);
                for(int side=-1;side<=1;side+=2)
                {
                    part("Track skirt",new Vector3(side*1.55f,.55f,0),new Vector3(.6f,.75f,6.6f),dark,true);
                    part("Fender",new Vector3(side*1.45f,1.1f,0),new Vector3(.65f,.12f,6.4f),color,false);
                }
                part("Turret",new Vector3(0,1.85f,-.3f),new Vector3(2.3f,.8f,3.1f),color,true);
                part("Turret ring",new Vector3(0,1.55f,-.3f),new Vector3(1.8f,.15f,2f),dark,false);
                part("Gun barrel",new Vector3(0,2f,2.4f),new Vector3(.24f,.24f,2.6f),dark,false);
                part("Muzzle brake",new Vector3(0,2f,3.6f),new Vector3(.34f,.34f,.55f),dark,false);
                part("Commander hatch",new Vector3(.55f,2.35f,-1),new Vector3(.75f,.18f,.75f),color,false);
                part("Loader hatch",new Vector3(-.55f,2.35f,-1),new Vector3(.65f,.16f,.65f),color,false);
                part("Antenna",new Vector3(.8f,2.7f,-1.4f),new Vector3(.07f,.75f,.07f),dark,false);
                part("Smoke launcher",new Vector3(-.55f,2.05f,.9f),new Vector3(.15f,.15f,.4f),new Color(.15f,.15f,.15f),false);
                part("Headlamp",new Vector3(1.1f,1.28f,3.1f),new Vector3(.3f,.2f,.2f),new Color(1,.93f,.65f),false);
            }
            else
            {
                if(kind==VehicleKind.Truck){length=7;width=2.5f;height=1;}
                if(kind==VehicleKind.Bus){length=10;width=2.6f;height=1;}
                if(kind==VehicleKind.SUV){height=1.2f;width=2.15f;length=4.8f;}
                if(kind==VehicleKind.Sport){height=.5f;length=4.4f;}
                part("Body",new Vector3(0,.75f,0),new Vector3(width,height,length),color,true);
                if(kind==VehicleKind.Bus)
                {
                    part("Passenger compartment",new Vector3(0,1.9f,0),new Vector3(width,1.6f,length-.3f),color,true);
                    part("Bus windows",new Vector3(0,2.1f,0),new Vector3(width+.03f,.8f,length-1),glass,false);
                    part("Roof",new Vector3(0,2.8f,0),new Vector3(width,.2f,length),color,false);
                }
                else if(kind==VehicleKind.Truck)
                {
                    part("Cargo box",new Vector3(0,2,-1),new Vector3(2.4f,2.1f,4.4f),color,true);
                    part("Driver cabin",new Vector3(0,1.6f,2.15f),new Vector3(2.3f,1.7f,2.2f),color,true);
                    part("Windshield",new Vector3(0,1.95f,3.27f),new Vector3(2,.7f,.08f),glass,false);
                }
                else
                {
                    part("Cabin",new Vector3(0,1.25f+height*.2f,-.25f),new Vector3(width*.86f,.7f,2),glass,true);
                    part("Roof",new Vector3(0,1.65f+height*.2f,-.25f),new Vector3(width*.88f,.13f,1.9f),color,false);
                    if(kind==VehicleKind.Sport)part("Spoiler",new Vector3(0,1.2f,-1.8f),new Vector3(2,.12f,.4f),dark,false);
                }
                for(int side=-1;side<=1;side+=2){part("Headlamp",new Vector3(side*width*.33f,.8f,length/2+.02f),new Vector3(.4f,.18f,.07f),new Color(1,.93f,.65f),false);part("Brake lamp",new Vector3(side*width*.33f,.8f,-length/2-.02f),new Vector3(.4f,.18f,.07f),new Color(.8f,.12f,.1f),false);}
            }
            if(kind!=VehicleKind.Helicopter && kind!=VehicleKind.Boat)
            {
                for(int side=(kind==VehicleKind.Motorcycle || kind==VehicleKind.Bicycle)?0:-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2)
                {
                    var wheel=GameObject.CreatePrimitive(PrimitiveType.Cylinder);wheel.name="Wheel";wheel.transform.SetParent(root.transform,false);
                    wheel.transform.localPosition=kind==VehicleKind.Tank
                        ?new Vector3(side*3.4f*.41f,.5f,end*7f*.28f)
                        :new Vector3(side*width*.52f,.4f,end*length*.32f);
                    wheel.transform.localRotation=Quaternion.Euler(0,0,90);
                    wheel.transform.localScale=kind==VehicleKind.Tank?new Vector3(1.25f,.46f,1.25f):kind==VehicleKind.Bicycle?new Vector3(.7f,.045f,.7f):new Vector3(.8f,.18f,.8f);
                    wheel.GetComponent<Renderer>().sharedMaterial=Mat(dark);Object.Destroy(wheel.GetComponent<Collider>());
                }
                var support=root.AddComponent<BoxCollider>();support.center=new Vector3(0,.8f,0);support.size=new Vector3(width*.9f,.6f,length*.8f);
            }
            var car=root.AddComponent<VehicleMotor>();car.Kind=kind;car.Title=name;car.Traffic=traffic;
            if(kind==VehicleKind.Helicopter)
            {
                var rotor=part("Main rotor",new Vector3(0,2.7f,0),new Vector3(12,.1f,.3f),dark,false);
                var cross=Box("Rotor cross",Vector3.zero,new Vector3(.3f,.1f,12),dark,null,false);cross.transform.SetParent(rotor.transform,false);cross.transform.localPosition=Vector3.zero;
                // Keep the pivot unscaled so perpendicular blades have equal size.
                rotor.transform.localScale=Vector3.one;rotor.GetComponent<MeshFilter>().mesh=CreateBladeMesh();
                car.Rotor=rotor.transform;
            }
            car.Initialize();game.Cars.Add(car);if(!car.OnWater)car.GetComponent<VehicleRecovery>().ValidateSpawn();return car;
        }
        static Mesh CreateBladeMesh()
        {
            var mesh=new Mesh();mesh.vertices=new[]{new Vector3(-6,0,-.18f),new Vector3(6,0,-.18f),new Vector3(6,0,.18f),new Vector3(-6,0,.18f)};mesh.triangles=new[]{0,2,1,0,3,2,0,1,2,0,2,3};mesh.RecalculateNormals();return mesh;
        }
        public static void AddServiceDetails(VehicleMotor car,int faction)
        {
            UniformDetails.Vehicle(car,faction);
            bool emergency=faction>=1 && faction<=5 || faction==12;
            if(emergency && !car.GetComponent<EmergencyLights>())car.gameObject.AddComponent<EmergencyLights>().Initialize(car,faction);
            var sign=new GameObject("Service identification");sign.transform.SetParent(car.transform,false);sign.transform.localPosition=new Vector3(0,car.Kind==VehicleKind.Helicopter?3.4f:3.8f,0);
            var text=sign.AddComponent<TextMesh>();text.text=faction==11?"УЧЕБНЫЙ":GameRoot.Instance.Roles[faction].Name.ToUpper();text.fontSize=40;text.characterSize=.055f;text.anchor=TextAnchor.MiddleCenter;
            SetFont(text);
        }
        public static void SetFont(TextMesh text){text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.GetComponent<Renderer>().sharedMaterial=WorldText.ForFont(text.font);WorldLabelFit.Attach(text,Mathf.Clamp(text.text.Length*.13f,1,5),.7f);}
        static void Tree(Vector3 pos,Transform parent)
        {
            Box("Tree trunk",pos+Vector3.up*2,new Vector3(.7f,4,.7f),new Color(.36f,.28f,.18f),parent);
            var crown=GameObject.CreatePrimitive(PrimitiveType.Sphere);crown.name="Tree crown";crown.transform.position=pos+Vector3.up*5;
            crown.transform.localScale=new Vector3(5,6,5);crown.transform.SetParent(parent);
            crown.GetComponent<Renderer>().sharedMaterial=Mat(new Color(.2f,.37f,.24f));Object.Destroy(crown.GetComponent<Collider>());
        }
        static void BuildAirport(Transform parent)
        {
            Color asphalt=new Color(.2f,.22f,.23f),palephalt=new Color(.34f,.37f,.4f);
            Box("Airport access road",new Vector3(0,.03f,-925),new Vector3(14,.12f,260),asphalt,parent);
            Box("Runway",new Vector3(0,.06f,-1080),new Vector3(230,.14f,26),palephalt,parent);
            for(float d=-110;d<=110;d+=20)Box("Runway marking",new Vector3(d,.16f,-1080),new Vector3(7,.02f,.7f),new Color(.94f,.8f,.4f),parent,false);
            Box("Runway taxiway",new Vector3(0,.05f,-1040),new Vector3(190,.12f,8),asphalt,parent);
            string[] hangars={"Ангар A","Ангар B","Ангар C"};
            for(int i=0;i<3;i++)
            {
                Vector3 at=new Vector3(-80+i*80,2,-1035);
                Box(hangars[i],at+Vector3.up*.5f,new Vector3(30,8,20),new Color(.62f,.64f,.66f),parent);
                Box(hangars[i]+" roof",at+Vector3.up*4.4f,new Vector3(31.5f,1,21.5f),new Color(.3f,.34f,.36f),parent);
                Box("Hangar door",at+new Vector3(0,2,-10.05f),new Vector3(24,5.6f,.2f),new Color(.24f,.28f,.32f),parent,false);
            }
            Box("Airport control tower",new Vector3(60,6,-1000),new Vector3(8,12,8),new Color(.7f,.72f,.75f),parent);
            Box("Tower glass deck",new Vector3(60,12.6f,-1000),new Vector3(11,2.4f,11),new Color(.2f,.4f,.46f),parent);
            Box("Airport apron",new Vector3(-20,.04f,-1005),new Vector3(60,.1f,30),palephalt,parent);
            var label=new GameObject("Los Santos Airport");label.transform.position=new Vector3(0,8,-1096);label.transform.rotation=Quaternion.Euler(0,180,0);
            var text=label.AddComponent<TextMesh>();text.text="LOS SANTOS AIRPORT";text.fontSize=42;text.characterSize=.09f;text.anchor=TextAnchor.MiddleCenter;SetFont(text);
        }
        static void BuildPalms(Transform parent)
        {
            for(int i=0;i<14;i++)
            {
                float z=Random.Range(-880,880);
                Vector3 pos=new Vector3(-885,-0,0)+new Vector3(Random.Range(-4,10),0,z);
                Box("Palm trunk",pos+Vector3.up*3.4f,new Vector3(.4f,7,.4f),new Color(.5f,.4f,.26f),parent);
                var palm=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                palm.name="Palm fronds";palm.transform.position=pos+Vector3.up*7.2f;palm.transform.localScale=new Vector3(4.4f,1.1f,4.4f);palm.transform.SetParent(parent);
                palm.GetComponent<Renderer>().sharedMaterial=Mat(new Color(.24f,.43f,.2f));Object.Destroy(palm.GetComponent<Collider>());
            }
        }
        static void BuildBaseCamp(GameRoot game,Transform parent,Vector3 at)
        {
            var steel=new Color(.62f,.66f,.68f);
            Box("Research lab",at+new Vector3(-30,2.4f,6),new Vector3(17,4.8f,11),new Color(.78f,.81f,.83f),parent);
            Box("Lab roof",at+new Vector3(-30,5,6),new Vector3(18,.6f,12),steel,parent);
            Box("Lab entrance",at+new Vector3(-30,1.6f,11.2f),new Vector3(3.4f,3.2f,.25f),new Color(.16f,.2f,.22f),parent,false);
            Box("Field hospital",at+new Vector3(22,2.2f,-4),new Vector3(14,4.3f,10),new Color(.9f,.9f,.92f),parent);
            Box("Hospital roof",at+new Vector3(22,4.6f,-4),new Vector3(15,.6f,11),new Color(.5f,.14f,.12f),parent);
            Box("Hospital cross V",at+new Vector3(22,3.2f,1.2f),new Vector3(.5f,1.8f,.14f),new Color(.9f,.1f,.1f),parent,false);
            Box("Hospital cross H",at+new Vector3(22,3.2f,1.2f),new Vector3(1.8f,.5f,.14f),new Color(.9f,.1f,.1f),parent,false);
            for(int i=0;i<5;i++)Box("Supply crate",at+new Vector3(4+i*3.4f,1,-14),new Vector3(2,2,3),new Color(.4f,.47f,.35f),parent);
            for(int i=0;i<3;i++)Box("Fuel barrel",at+new Vector3(-10+i*2.6f,1.2f,-20+((i%2)*2.4f)),new Vector3(1.1f,2.2f,1.1f),new Color(.6f,.24f,.16f),parent);
            Box("Launch pad marking",at+new Vector3(0,.12f,-26),new Vector3(20,.06f,20),new Color(.42f,.48f,.5f),parent,false);
            for(int i=0;i<4;i++)BaseNPC.Spawn(at+new Vector3(-30,1,6)+new Vector3(Random.Range(-6,6),0,Random.Range(-4,7)),"Учёный базы",false);
            for(int i=0;i<2;i++)BaseNPC.Spawn(at+new Vector3(22,1,-4)+new Vector3(i*4-2,0,8),"Полевой медик",true);
        }
    }
    public sealed class Pedestrian:MonoBehaviour
    {
        public float Health=100;public bool Neutralized;
        public void TakeVehicleDamage(float damage){if(Neutralized)return;Health=Mathf.Max(0,Health-damage);Neutralized=Health<=0;CivilianKnockdown.Fall(gameObject,Neutralized);}
        Vector3 origin;float phase;NpcGroundMotor locomotion;
        void Start(){locomotion=gameObject.AddComponent<NpcGroundMotor>();locomotion.Initialize();origin=transform.position;phase=Random.value*10;}
        void Update(){if(!Neutralized && !GetComponent<CivilianKnockdown>() && GameRoot.Instance && GameRoot.Instance.Playing && GameRoot.Instance.Panel==""){float t=Time.time*.12f+phase;Vector3 delta=origin+Vector3.forward*Mathf.Sin(t)*14-transform.position;delta.y=0;locomotion.Move(Vector3.ClampMagnitude(delta,1)*1.7f);if(delta.sqrMagnitude>.1f)transform.rotation=Quaternion.LookRotation(delta);}}
    }
    public sealed class BaseNPC:MonoBehaviour
    {
        public static int Scientists,Medics;
        public bool Medic;public float Health=100;public bool Neutralized;
        public void TakeVehicleDamage(float damage){if(Neutralized)return;Health=Mathf.Max(0,Health-damage);Neutralized=Health<=0;CivilianKnockdown.Fall(gameObject,Neutralized);}

        Vector3 center,goal;float pause,healNotice;NpcGroundMotor locomotion;
        public static void Spawn(Vector3 at,string name,bool medic)
        {
            var npc=new GameObject(name);npc.transform.position=at;
            var component=npc.AddComponent<BaseNPC>();component.Medic=medic;component.locomotion=npc.AddComponent<NpcGroundMotor>();component.locomotion.Initialize();component.center=npc.transform.position;component.goal=npc.transform.position;
            CharacterModels.Create(npc.transform,4,medic?new Color(.96f,.96f,.98f):new Color(.82f,.88f,.94f));
            if(medic)Medics++;else Scientists++;
        }
        void Update()
        {
            var game=GameRoot.Instance;
            if(!game || !game.Playing || game.Panel!="" || Neutralized || GetComponent<CivilianKnockdown>())return;
            if(Medic)
            {
                if(Vector3.Distance(game.Player.Position,transform.position)<6 && game.Player.Health<100)
                {
                    game.Player.Health=Mathf.Min(100,game.Player.Health+Time.deltaTime*6);
                    if(Time.unscaledTime-healNotice>2.5f){healNotice=Time.unscaledTime;game.Notify("Полевой медик укрепляет здоровье");}
                }
                return;
            }
            if(pause>0){pause-=Time.deltaTime;return;}
            Vector3 delta=goal-transform.position;delta.y=0;
            if(delta.magnitude<.6f){Vector3 requested=center+new Vector3(Random.Range(-13,13),0,Random.Range(-9,11));if(SpawnSafety.TryPlayerPosition(requested,out var safe,transform))goal=safe;pause=Random.Range(1.6f,4.2f);return;}
            locomotion.Move(delta.normalized*1.5f);
            transform.rotation=Quaternion.Euler(0,Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,0);
        }
    }
}
