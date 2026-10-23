using System.Collections.Generic;
using UnityEngine;
namespace SunsetState
{
    public sealed class RoleAccessory
    {
        public string Name,Stats;public int Slot;public float Run=1,Protection,Healing,Accuracy=1;public int Armor;
        public RoleAccessory(string name,string stats,int slot){Name=name;Stats=stats;Slot=slot;}
        public bool Allowed(int role,int id)
        {
            if(role==13)return false;bool law=role==1||role==2||role==3||role==12;
            if(id==3 || id==14)return law || role==5;
            if(id==4 || id==24)return role==3||role==4||role==5;
            if(id==5)return law||role==9||role==14||role==16;
            if(id==7)return law||role==9||role==14||role==16;
            if(id==13)return law||role==4||role==5;
            if(id==17 || id==21)return law;
            if(id==18)return role==9||role==14||role==16;
            return true;
        }
    }
    public sealed partial class GameRoot
    {
        public bool[] FemaleChoices=new bool[18];
        public int[] SkinChoices=new int[18],AccessoryChoices=new int[54],ShieldChoices=new int[18];Vector2 wardrobeScroll;
        static readonly RoleAccessory[] Accessories={
            new RoleAccessory("Без аксессуара","Без бонусов",0),
            new RoleAccessory("Защитные очки","Разброс оружия −15%",0){Accuracy=.85f},
            new RoleAccessory("Лёгкие ботинки","Бег +8%",2){Run=1.08f},
            new RoleAccessory("Служебный шлем","Входящий урон −10%",0){Protection=.1f},
            new RoleAccessory("Медицинская сумка","Личная аптечка лечит на 10 HP больше",1){Healing=10},
            new RoleAccessory("Разгрузка с бронепластиной","Максимум брони +25 · бег −5%",1){Armor=25,Run=.95f},
            new RoleAccessory("Уличный спринт · стиль CJ","Бег +20% · быстрый шаг и наклон корпуса",2){Run=1.2f},
            new RoleAccessory("Балаклава","Входящий урон −3% · не снимает розыск",0){Protection=.03f},
            new RoleAccessory("Городская кепка","Косметический головной убор",0),
            new RoleAccessory("Туристический рюкзак","Аптечка +5 HP · бег −2%",1){Healing=5,Run=.98f},
            new RoleAccessory("Стрелковые перчатки","Разброс −10%",1){Accuracy=.9f},
            new RoleAccessory("Высокие ботинки","Защита +5% · бег −3%",2){Protection=.05f,Run=.97f},
            new RoleAccessory("Очки-авиаторы","Разброс −12%",0){Accuracy=.88f},
            new RoleAccessory("Респиратор","Защита +8%",0){Protection=.08f},
            new RoleAccessory("Тяжёлый шлем","Защита +18% · бег −10%",0){Protection=.18f,Run=.9f},
            new RoleAccessory("Спортивные наколенники","Защита +6%",2){Protection=.06f},
            new RoleAccessory("Полевой рюкзак","Аптечка +8 HP · бег −4%",1){Healing=8,Run=.96f},
            new RoleAccessory("Служебный берет","Косметический головной убор",0),
            new RoleAccessory("Бандана района","Защита +2%",0){Protection=.02f},
            new RoleAccessory("Радиогарнитура","Косметическая гарнитура",0),
            new RoleAccessory("Дорожные очки","Разброс −8%",0){Accuracy=.92f},
            new RoleAccessory("Патрульная разгрузка","Максимум брони +20",1){Armor=20},
            new RoleAccessory("Лёгкий бронежилет","Максимум брони +15 · бег −2%",1){Armor=15,Run=.98f},
            new RoleAccessory("Утеплённый жилет","Защита +4%",1){Protection=.04f},
            new RoleAccessory("Медицинские перчатки","Аптечка +5 HP",1){Healing=5},
            new RoleAccessory("Беговые кроссовки","Бег +12%",2){Run=1.12f},
            new RoleAccessory("Тактические наколенники","Защита +8% · бег −3%",2){Protection=.08f,Run=.97f},
            new RoleAccessory("Сигнальный жилет","Защита +3%",1){Protection=.03f}
        };
        static readonly string[][] SkinNames={
            new[]{"Горожанин","Городской курьер","Уличный стиль"},new[]{"Патрульный","Мотопатруль","Группа реагирования"},new[]{"Агент бюро","Полевой агент","SWAT · штурмовой комплект"},new[]{"Гарнизон","Полевой комплект","Охрана конвоя"},new[]{"Врач приёмного отделения","Выездная бригада","Старший смены"},new[]{"Дежурный пожарный","Спасатель","Командир расчёта"},new[]{"Водитель такси","Городская смена","Межгород"},new[]{"Водитель грузовика","Рабочая смена","Дальнобойщик"},new[]{"Механик","Выездной мастер","Мастер цеха"},new[]{"Grove · район","Grove · улица","Grove · старший"},new[]{"Сотрудник мэрии","Выездная инспекция","Советник"},new[]{"Инструктор","Дорожный экзаменатор","Старший лицензер"},new[]{"Сотрудник охраны","Периметр","Старший караула"},new[]{"Тюремная роба","Рабочий блок","Жилая секция"},new[]{"Независимый","Тёмный квартал","Уличный авторитет"},new[]{"Защитник","Судебный адвокат","Партнёр коллегии"},new[]{"Костюм семьи","Полевой исполнитель","Консильери"},new[]{"Глава штата","Официальный приём","Выездная инспекция"}
        };
        int AccessoryAt(int slot){int index=RoleIndex*3+slot;int id=index<AccessoryChoices.Length?AccessoryChoices[index]:0;return id>0 && id<Accessories.Length && Accessories[id].Slot==slot && Accessories[id].Allowed(RoleIndex,id)?id:0;}
        float AccessorySum(System.Func<RoleAccessory,float> selector){float result=0;for(int i=0;i<3;i++)result+=selector(Accessories[AccessoryAt(i)]);return result;}
        public float RunMultiplier=>Accessories[AccessoryAt(0)].Run*Accessories[AccessoryAt(1)].Run*Accessories[AccessoryAt(2)].Run;
        public bool CJRun=>AccessoryAt(2)==6;
        public float AccuracyMultiplier=>Accessories[AccessoryAt(0)].Accuracy*Accessories[AccessoryAt(1)].Accuracy;
        public float HealingBonus=>AccessorySum(a=>a.Healing);
        public int AccessoryArmorBonus=>(int)AccessorySum(a=>a.Armor);
        public float AccessoryProtection=>Mathf.Min(.2f,AccessorySum(a=>a.Protection));
        public bool ShieldAllowed=>RoleIndex==1||RoleIndex==2||RoleIndex==3;
        public bool ShieldEquipped=>ShieldAllowed && ShieldChoices[RoleIndex]==1;
        public float ShieldDamageScale(Vector3 source,bool blocking)=>ShieldEquipped && blocking && Vector3.Dot(Player.transform.forward,Vector3.ProjectOnPlane(source-Player.Position,Vector3.up).normalized)>.35f?.2f:1;
        public bool ShieldBlocking=>ShieldEquipped && !Player.Vehicle && !Player.BoardRiding && Panel=="" && Input.GetMouseButton(1);
        void EquipAccessory(int id)
        {if(id<1||id>=Accessories.Length||!Accessories[id].Allowed(RoleIndex,id))return;Player.StopBoard();AccessoryChoices[RoleIndex*3+Accessories[id].Slot]=id;Armor=Mathf.Min(Armor,MaxArmor);Player.SetColor(Roles[RoleIndex].Color);Save();}
        void LoadOutfits(int[] skins,int[] accessories,int[] shields,bool[] females=null)
        {
            FemaleChoices=new bool[Roles.Length];if(females!=null)System.Array.Copy(females,FemaleChoices,Mathf.Min(females.Length,FemaleChoices.Length));
            SkinChoices=new int[Roles.Length];AccessoryChoices=new int[Roles.Length*3];ShieldChoices=new int[Roles.Length];
            if(skins!=null)System.Array.Copy(skins,SkinChoices,Mathf.Min(skins.Length,SkinChoices.Length));
            if(accessories!=null)System.Array.Copy(accessories,AccessoryChoices,Mathf.Min(accessories.Length,AccessoryChoices.Length));
            if(shields!=null)System.Array.Copy(shields,ShieldChoices,Mathf.Min(shields.Length,ShieldChoices.Length));
            for(int role=0;role<Roles.Length;role++){SkinChoices[role]=Mathf.Clamp(SkinChoices[role],0,5);ShieldChoices[role]=role>=1&&role<=3&&ShieldChoices[role]==1?1:0;for(int slot=0;slot<3;slot++){int i=role*3+slot,id=AccessoryChoices[i];if(id<1||id>=Accessories.Length||Accessories[id].Slot!=slot||!Accessories[id].Allowed(role,id))AccessoryChoices[i]=0;}}
        }
        void DrawWardrobe(float left,float h)
        {
            if(Button(new Rect(left+780,85,290,45),FemaleChoices[RoleIndex]?"ПЕРСОНАЖ: ЖЕНСКИЙ":"ПЕРСОНАЖ: МУЖСКОЙ")){Player.StopBoard();FemaleChoices[RoleIndex]=!FemaleChoices[RoleIndex];Player.SetColor(Roles[RoleIndex].Color);Save();}
            Label(left,85,750,45,"ИНВЕНТАРЬ / ОБЛИК · "+Roles[RoleIndex].Name.ToUpper(),heading);
            if(FreeRoleArmor && Button(new Rect(left+745,142,325,48),"БРОНЕЖИЛЕТ · БЕСПЛАТНО"))EquipFreeArmor();
            Label(left,140,720,55,"Бег ×"+RunMultiplier.ToString("F2")+" · защита "+Mathf.RoundToInt(AccessoryProtection*100)+"% · броня до "+MaxArmor+" · аптечка +"+(30+HealingBonus)+" HP\nОдин предмет на слот: голова, корпус, ноги. Форма и доступ к оружию определяются ролью.",small);
            int accessoryRows=0;for(int id=1;id<Accessories.Length;id++)if(Accessories[id].Allowed(RoleIndex,id))accessoryRows++;
            wardrobeScroll=GUI.BeginScrollView(new Rect(left,205,1080,h-255),wardrobeScroll,new Rect(0,0,1050,360+accessoryRows*80));
            for(int i=0;i<6;i++){int skin=i;string name=i<3?SkinNames[RoleIndex][i]:SkinNames[RoleIndex][i-3]+" · альтернативный";if(Button(new Rect(i%3*350,i/3*70,338,60),name+(SkinChoices[RoleIndex]==i?" · ✓":""))){Player.StopBoard();SkinChoices[RoleIndex]=skin;CasualClothes=false;Player.SetColor(Roles[RoleIndex].Color);Save();}}
            int row=0;for(int id=1;id<Accessories.Length;id++)
            {
                if(!Accessories[id].Allowed(RoleIndex,id))continue;var item=Accessories[id];float y=165+row++*80;
                RectBox(new Rect(0,y,1040,72),new Color(.1f,.16f,.12f));Label(15,y+6,670,30,item.Name,body);Label(15,y+38,680,26,item.Stats,small);
                if(Button(new Rect(750,y+14,270,44),AccessoryAt(item.Slot)==id?"СНЯТЬ":"НАДЕТЬ")){if(AccessoryAt(item.Slot)==id){Player.StopBoard();AccessoryChoices[RoleIndex*3+item.Slot]=0;Armor=Mathf.Min(Armor,MaxArmor);Player.SetColor(Roles[RoleIndex].Color);Save();}else EquipAccessory(id);}
            }
            float bottom=175+row*80;
            if(ShieldAllowed && Button(new Rect(0,bottom,1020,55),ShieldEquipped?"ЩИТ НАДЕТ · СНЯТЬ":"НАДЕТЬ ЩИТ · ПКМ: ЗАЩИТА СПЕРЕДИ 80%")){ShieldChoices[RoleIndex]=ShieldEquipped?0:1;Player.SetColor(Roles[RoleIndex].Color);Save();}
            if(Button(new Rect(0,bottom+75,1020,50),"МОИ ПОКУПКИ / ЕДА / НАПИТКИ →"))Toggle("purchases");
            GUI.EndScrollView();
        }
        public void ApplyPlayerOutfit()
        {
            if(!Player || !Player.Visual)return;var style=Player.Visual.gameObject.AddComponent<PlayerOutfitVisual>();style.Initialize(this,SkinChoices[RoleIndex],AccessoryAt(0),AccessoryAt(1),AccessoryAt(2));
        }
    }
    public sealed class OwnedOutfitMesh:MonoBehaviour
    {
        public Mesh Mesh;void OnDestroy(){if(Mesh){if(Application.isPlaying)Destroy(Mesh);else DestroyImmediate(Mesh);}}
    }
    public sealed class PlayerOutfitVisual:MonoBehaviour
    {
        GameRoot game;GameObject shield;Transform model;
        public void Initialize(GameRoot root,int skin,int head,int body,int legs)
        {
            game=root;model=transform.Find("Animated human");
            if(head==3 || head==14 || head==8 || head==17){foreach(var anchor in GetComponentsInChildren<Transform>())if(anchor.name=="Uniform Head")anchor.gameObject.SetActive(false);}
            if(!game.CasualClothes && skin>0)
            {
                var uniform=GetComponent<CharacterUniform>();var owned=new List<Material>(uniform.Materials);
                foreach(var renderer in GetComponentsInChildren<SkinnedMeshRenderer>())
                {var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++)if(materials[i] && !materials[i].name.Contains("Skin") && (materials[i].name.Contains("Shirt")||materials[i].name.Contains("Pants")||materials[i].name.Contains("Jacket")||materials[i].name.Contains("Suit")||materials[i].name.Contains("Coat"))){var tint=new Material(materials[i]);tint.color=Color.Lerp(tint.color,game.RoleIndex==13?new Color(1,.36f,.06f):game.Roles[game.RoleIndex].Color,Mathf.Clamp(.12f+skin*.13f,.2f,.75f));materials[i]=tint;owned.Add(tint);}renderer.sharedMaterials=materials;}
                uniform.Materials=owned.ToArray();
            }
            Transform skull=FittedAnchor(Bone("Head",1.65f),"Head fitting",1.43f),chest=FittedAnchor(Bone("Spine",1.1f),"Torso fitting",.9f);
            if(head==1 || head==12 || head==20){foreach(int side in new[]{-1,1})Part(skull,"Protective lens",new Vector3(side*.085f,.05f,.13f),new Vector3(.13f,.07f,.035f),new Color(.08f,.18f,.22f));Part(skull,"Glasses bridge",new Vector3(0,.05f,.15f),new Vector3(.05f,.025f,.02f),Color.black);}
            if(head==3 || head==14)Part(skull,"Helmet",new Vector3(0,.3f,0),new Vector3(.5f,.28f,.48f),new Color(.15f,.19f,.17f),true);
            if(head==7)
            {CreateBalaclava(skull);}
            if(body==4 || body==9 || body==16)Part(chest,"Medical backpack",new Vector3(0,.05f,-.24f),new Vector3(.33f,.4f,.18f),new Color(.8f,.15f,.15f));
            if(body==5 || body==21 || body==22 || body==23 || body==27)Part(chest,"Tactical loadout",new Vector3(0,.05f,.1f),new Vector3(.49f,.42f,.28f),body==27?new Color(.9f,.85f,.1f):new Color(.15f,.18f,.16f));
            if(head==8 || head==17){Part(skull,"Cap crown",new Vector3(0,.26f,0),new Vector3(.48f,.2f,.43f),head==17?game.Roles[game.RoleIndex].Color:new Color(.14f,.16f,.2f),true);if(head==8)Part(skull,"Cap visor",new Vector3(0,.22f,.24f),new Vector3(.35f,.04f,.22f),Color.black);}
            if(head==18)Part(skull,"Bandana",new Vector3(0,-.08f,.2f),new Vector3(.42f,.18f,.13f),game.Roles[game.RoleIndex].Color,true);
            if(head==13){Part(skull,"Respirator",new Vector3(0,-.1f,.22f),new Vector3(.3f,.2f,.17f),new Color(.15f,.17f,.18f),true);foreach(int side in new[]{-1,1})Part(skull,"Filter",new Vector3(side*.17f,-.1f,.23f),new Vector3(.12f,.14f,.14f),Color.gray,true);}
            if(head==19)foreach(int side in new[]{-1,1})Part(skull,"Radio headset",new Vector3(side*.25f,.04f,0),new Vector3(.08f,.18f,.14f),Color.black);
            if(body==10 || body==24)foreach(string side in new[]{"L","R"})Part(Bone("Fist."+side,.8f),"Glove",Vector3.zero,new Vector3(.14f,.13f,.17f),body==24?new Color(.65f,.85f,1):new Color(.14f,.16f,.14f),true);
            if(!game.CasualClothes && game.RoleIndex!=0 && game.RoleIndex!=9 && game.RoleIndex!=13 && game.RoleIndex!=14)
            {
                Part(chest,"Faction identification",new Vector3(.13f,.12f,.21f),new Vector3(.13f,.08f,.02f),game.RoleIndex==2?new Color(.9f,.73f,.22f):game.Roles[game.RoleIndex].Color);
                if(game.RoleIndex==2 && skin%3==2)UniformDetails.Swat(transform,skull,chest);
                if(skin==2)Part(skull,"Radio earpiece",new Vector3(.15f,0,0),new Vector3(.055f,.1f,.06f),Color.black);
            }
            if(legs==2 || legs==6 || legs==11 || legs==15 || legs==25 || legs==26)foreach(string side in new[]{"Foot.L","Foot.R"}){var foot=Bone(side,.12f);Part(foot,"Sport footwear",new Vector3(0,.02f,.04f),new Vector3(.13f,.12f,.27f),legs==6?new Color(.12f,.15f,.17f):new Color(.3f,.34f,.32f));}
            if(game.ShieldEquipped)
            {shield=Part(chest,"Ballistic shield",new Vector3(-.35f,.15f,.65f),new Vector3(.7f,.95f,.075f),new Color(.12f,.16f,.18f));Part(shield.transform,"Shield viewport",new Vector3(0,.28f,.05f),new Vector3(.35f,.13f,.03f),new Color(.3f,.55f,.65f));}
        }
        Transform FittedAnchor(Transform parent,string name,float height)
        {var anchor=new GameObject(name).transform;anchor.position=game.Player.transform.position+Vector3.up*height;anchor.rotation=game.Player.transform.rotation;anchor.SetParent(parent,true);return anchor;}
        void CreateBalaclava(Transform head)
        {
            const int segments=24;float[] heights={-.34f,-.23f,-.1f,.01f,.11f,.26f,.36f};
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int ring=0;ring<heights.Length;ring++)for(int i=0;i<=segments;i++)
            {float y=heights[ring],angle=i*Mathf.PI*2/segments;float radius=Mathf.Sqrt(Mathf.Max(.04f,1-y*y/(.38f*.38f)));vertices.Add(new Vector3(Mathf.Sin(angle)*.3f*radius,y,Mathf.Cos(angle)*.28f*radius));}
            for(int ring=0;ring<heights.Length-1;ring++)for(int i=0;i<segments;i++)
            {
                float angle=(i+.5f)*Mathf.PI*2/segments;
                // Leave a horizontal opening across the eyes at the front of the fitted hood.
                if(ring==3 && Mathf.Cos(angle)>.45f)continue;
                int a=ring*(segments+1)+i,b=a+segments+1;triangles.Add(a);triangles.Add(a+1);triangles.Add(b);triangles.Add(a+1);triangles.Add(b+1);triangles.Add(b);
            }
            var mesh=new Mesh();mesh.name="Balaclava with eye opening";mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
            var hood=new GameObject("Balaclava");hood.layer=2;hood.transform.position=head.position;hood.transform.rotation=game.Player.transform.rotation;hood.transform.SetParent(head,true);hood.AddComponent<MeshFilter>().sharedMesh=mesh;hood.AddComponent<MeshRenderer>().sharedMaterial=WorldBuilder.Mat(new Color(.06f,.065f,.07f));hood.AddComponent<OwnedOutfitMesh>().Mesh=mesh;
        }
        Transform Bone(string name,float height)
        {foreach(var bone in GetComponentsInChildren<Transform>())if(bone.name==name || bone.name.EndsWith(":"+name))return bone;var anchor=new GameObject(name+" anchor").transform;anchor.SetParent(transform,false);anchor.position=game.Player.transform.position+Vector3.up*height;anchor.rotation=game.Player.transform.rotation;return anchor;}
        GameObject Part(Transform parent,string name,Vector3 offset,Vector3 size,Color color,bool round=false)
        {
            var obj=round?GameObject.CreatePrimitive(PrimitiveType.Sphere):GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;if(Application.isPlaying)Destroy(obj.GetComponent<Collider>());else DestroyImmediate(obj.GetComponent<Collider>());obj.GetComponent<Renderer>().sharedMaterial=WorldBuilder.Mat(color);
            obj.transform.position=parent.position+game.Player.transform.rotation*offset;obj.transform.rotation=game.Player.transform.rotation;obj.transform.localScale=size;obj.transform.SetParent(parent,true);obj.layer=2;return obj;
        }
        void LateUpdate()
        {
            if(!game || game.Player.Visual!=transform)return;
            if(shield)shield.SetActive(!game.Player.Vehicle && !game.Player.BoardRiding);
            var animation=GetComponentInChildren<Animation>();bool sprint=game.CJRun && !game.Player.Vehicle && !game.Player.BoardRiding && Input.GetKey(KeyCode.LeftShift) && Input.GetAxisRaw("Vertical")!=0;
            if(animation)foreach(AnimationState state in animation)state.speed=sprint?1.3f:1;
            if(model)model.localRotation=Quaternion.Slerp(model.localRotation,Quaternion.Euler(sprint?8:0,0,0),Time.deltaTime*8);
        }
    }
}
