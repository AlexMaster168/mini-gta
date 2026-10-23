using System.Collections.Generic;
using UnityEngine;

namespace SunsetState
{
    // Patrol duty: dispatch radio calls (10-codes), megaphone, interrogations, transports and raids.
    public sealed partial class GameRoot
    {
        public sealed class DutyCall
        {
            public string Code,Title,Description;
            public Vector3 Point;public float Expires;
            public int Reward,Wave;
            public List<HostileAgent> Threats=new List<HostileAgent>();
            public VehicleMotor Runner;public bool Warned;
            public GameObject Marker;
        }
        public DutyCall Call;
        const float CallLifetime=150;
        float nextCallAt=25,callHold,supportCooldown,megaCooldown;
        int shoutIndex;
        public static readonly string[] Shouts=
        {
            "10-31! Стой, полиция! Руки на виду!",
            "10-55! Водитель, немедленно прижмись к обочине!",
            "10-66! Стой, проверка документов!",
            "10-80! Прекратить погоню, иначе применим силу!",
            "10-13! Запрошено подкрепление, работаем по углам!"
        };
        bool LawDuty=>RoleIndex==1||RoleIndex==2||RoleIndex==3;
        void UpdateDuty()
        {
            if((RoleIndex==10||RoleIndex==17)&&!Player.Vehicle&&Input.GetKeyDown(KeyCode.L))Toggle("laws");
            HandleDutyInput();
            UpdateDispatch();
            UpdatePursuitTarget();
            UpdateTransport();
            UpdateBanditRaid();
        }
        void HandleDutyInput()
        {
            if((RoleIndex==1||RoleIndex==2) && (Input.GetKeyDown(KeyCode.F6)||!Player.Vehicle&&Input.GetKeyDown(KeyCode.L)))Toggle("operations");
            if(RoleIndex==11 && Input.GetKeyDown(KeyCode.L))TrySellLicenses();
            if(!LawDuty)return;
            if(Input.GetKeyDown(KeyCode.N))Megaphone();
            if(Input.GetKeyDown(KeyCode.C))TryInterrogate();
        }
        void UpdateDispatch()
        {
            bool law=LawDuty && !InInterior;
            if(Call==null && law && ActiveMission==null && Time.time>=nextCallAt)
            {
                StartCall();
                nextCallAt=Time.time+Random.Range(50,85);
                return;
            }
            if(Call==null)return;
            if(!law || Time.unscaledTime>Call.Expires){EndCall(false);return;}
            if(Call.Marker)Call.Marker.transform.Rotate(0,35*Time.deltaTime,0);
            for(int i=Call.Threats.Count-1;i>=0;i--)if(!Call.Threats[i]||Call.Threats[i].Neutralized)Call.Threats.RemoveAt(i);
            bool success=false;
            switch(Call.Code)
            {
                case "10-23":
                    if(RoleIndex==3 && Call.Wave<3 && Call.Threats.Count==0){NextSiegeWave();return;}
                    success=Call.Threats.Count==0;break;
                case "10-37":success=operationEvidence;break;
                case "10-82":success=operationEvidence && Call.Threats.Count==0;break;
                case "10-91":success=hostageRescued;break;
                case "10-60":success=operationEvidence&&Call.Threats.Count==0;break;
                case "10-32": case "10-31": case "10-90": success=Call.Threats.Count==0;break;
                case "10-66":
                    if(Call.Threats.Count==1 && Call.Threats[0])
                    {
                        var threat=Call.Threats[0];
                        if(Vector3.Distance(Player.Position,threat.transform.position)<3.5f){callHold+=Time.deltaTime;success=callHold>2.5f;}
                    }
                    break;
                case "10-55":
                    if(Call.Runner)
                    {
                        var car=Call.Runner;
                        bool stopped=Vector3.Distance(Player.Position,car.transform.position)<9 && car.Speed<5;
                        callHold=stopped?callHold+Time.deltaTime:0;
                        success=callHold>2.2f;
                    }
                    break;
                case "10-13":
                    callHold=Vector3.Distance(Player.Position,Call.Point)<7?callHold+Time.deltaTime:0;
                    success=callHold>2.5f;
                    break;
            }
            if(success)EndCall(true);
        }
        void StartCall()
        {
            Call=new DutyCall{Expires=Time.unscaledTime+CallLifetime};
            Vector3 point=DutyPoint();Call.Point=point;Call.Wave=1;
            if(RoleIndex==3)
            {
                Call.Code="10-23";Call.Title="Осада периметра";Call.Description="Диверсанты штурмуют базу, три волны";
                Call.Reward=700;Call.Expires=Time.unscaledTime+CallLifetime*1.6f;
                SpawnSiegeThreats();
                Notify("Рация: 10-23! Осада базы, волна 1/3. Держи периметр, награда растёт с каждой волной.");
            }
            else
            {
                if(Random.value<.45f){StartOperation(RoleIndex==1?new[]{0,1,4,5,6}[Random.Range(0,5)]:Random.Range(0,7));return;}
                int kind=Random.Range(0,5);
                if(kind==4)
                {
                    Call.Code="10-90";Call.Title="Битва с бандой";Call.Description="Банда захватила склад, всех задержать";
                    Call.Reward=1600;Call.Expires=Time.unscaledTime+280;
                    for(int i=0;i<4;i++)
                    {
                        var threat=HostileAgent.Spawn(this,point+new Vector3(-14+i*9,1,-7+Random.Range(-3,4)),"Бандит склада",false);
                        Call.Threats.Add(threat);
                    }
                    Notify("Рация: 10-90! Битва с бандой. Нейтрализуй всех; за каждого посаженного в тюрьму +$600 (зона Администрации).");
                }
                else if(kind==0)
                {
                    Call.Code="10-31";Call.Title="Преступление в процессе";Call.Description="Вооружённые лица в квартале";
                    Call.Reward=750;
                    for(int i=0;i<2;i++)
                    {
                        var threat=HostileAgent.Spawn(this,point+new Vector3(-6+i*10,1,6),i%2==0?"Вооружённый грабитель":"Орудующий преступник",false);
                        threat.Suspect=true;Call.Threats.Add(threat);
                    }
                    Notify("Рация: 10-31! Вызов. Нейтрализуй и задержи подозреваемых.");
                }
                else if(kind==1)
                {
                    Call.Code="10-55";Call.Title="Пьяный водитель";Call.Description="Красный автомобиль, хаотичное движение";
                    Call.Reward=600;
                    var runner=WorldBuilder.SpawnCar(this,point,"10-55 пьяный водитель",new Color(.85f,.14f,.14f),false,Random.Range(0f,359f));
                    runner.Traffic=false;runner.Durability=45;Call.Runner=runner;
                    Notify("Рация: 10-55! Пьяный водитель. Догони и выдави на обочину (N — мегафон).");
                }
                else if(kind==2)
                {
                    Call.Code="10-66";Call.Title="Подозрительное лицо";Call.Description="Беспокойный субъект у дороги";
                    Call.Reward=500;
                    var suspect=HostileAgent.Spawn(this,point+Vector3.up,"Подозреваемый 10-66",false);
                    suspect.Suspect=true;Call.Threats.Add(suspect);
                    Notify("Рация: 10-66! Подозрительное лицо. Подойди и проверь документы, затем тайзер (8) и наручники (F).");
                }
                else
                {
                    Call.Code="10-13";Call.Title="Офицеру нужна помощь";Call.Description="Раненый патрульный, нужна эвакуация";
                    Call.Reward=650;
                    var victim=HostileAgent.Spawn(this,point+Vector3.up+Vector3.right*2,"Раненый патрульный",false);
                    victim.Suspect=true;victim.Stun(120);Call.Threats.Add(victim);
                    Notify("Рация: 10-13! Стой у раненого напарника две секунды — эвакуация.");
                }
            }
            var mark=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Destroy(mark.GetComponent<Collider>());
            mark.GetComponent<Renderer>().sharedMaterial=WorldBuilder.Mat(RoleIndex==3?new Color(1,.3f,.25f):new Color(.25f,.55f,1));
            Call.Marker=mark;mark.transform.position=point+Vector3.up*2;mark.transform.localScale=new Vector3(4,2,4);
        }
        void NextSiegeWave()
        {
            Call.Wave++;Call.Reward+=250;
            ClearCallThreats();
            SpawnSiegeThreats();
            Notify("Рация: 10-23! Волна "+Call.Wave+"/3 отбита, идёт вторая атака. Награда: $"+Call.Reward);
        }
        void SpawnSiegeThreats()
        {
            for(int i=0;i<3+Call.Wave*2;i++)
            {
                var threat=HostileAgent.Spawn(this,Call.Point+new Vector3(-9+i*7,1,8+Random.Range(-2,3)),"Диверсант · волна "+Call.Wave,false);
                threat.Suspect=false;Call.Threats.Add(threat);
            }
        }
        void EndCall(bool done)
        {
            ClearOperation();
            if(done){Money+=Call.Reward;Completed++;AwardXP(70+Call.Wave*20);Notify("Вызов закрыт: "+Call.Code+(Call.Wave>1?" · "+Call.Wave+" мили":"")+" · +$"+Call.Reward);}
            else Notify("Вызов "+(Call!=null?Call.Code:"10-xx")+" снят: время вышло или смена заехала.");
            nextCallAt=Mathf.Max(nextCallAt,Time.time+45);
            if(Call!=null)
            {
                ClearCallThreats();
                if(Call.Marker)Destroy(Call.Marker);
                if(Call.Runner){Cars.Remove(Call.Runner);Destroy(Call.Runner.gameObject);}
            }
            Call=null;callHold=0;
            Save();
        }
        void ClearCallThreats()
        {
            if(Call==null)return;
            foreach(var threat in Call.Threats)if(threat && threat.gameObject!=transportSuspect)Destroy(threat.gameObject);
            Call.Threats.Clear();
        }
        Vector3 DutyPoint()
        {
            if(OnVice)return ViceCityIsland.Center+new Vector3(Random.Range(-3,4)*160,0,Random.Range(-3,4)*160);
            Vector3 at=Player.Position+new Vector3(Random.Range(-360,360),0,Random.Range(-360,360));
            float x=Mathf.Clamp(at.x,-800,800),z=Mathf.Round(Mathf.Clamp(at.z,-800,800)/200)*200;
            return new Vector3(x,1.2f,z);
        }
        void Megaphone()
        {
            if(Time.time<megaCooldown)return;megaCooldown=Time.time+.9f;
            string shout=Shouts[shoutIndex=(shoutIndex+1)%Shouts.Length];
            Notify("МЕГАФОН: "+shout);
            foreach(var enemy in Enemies)
            {
                if(!enemy || enemy.Neutralized)continue;
                if(Vector3.Distance(Player.Position,enemy.transform.position)>32)continue;
                enemy.Panic(3.2f);
                if(shout.StartsWith("10-13") && Time.time>=supportCooldown)
                {
                    supportCooldown=Time.time+25;
                    enemy.TakeDamage(45);
                    DrawShot(Player.Position+Vector3.up*1.4f,enemy.transform.position+Vector3.up,new Color(.3f,.8f,1));
                    Notify("Поддержка на месте: снайперы накрыли цель");
                }
            }
            if(shout.StartsWith("10-55"))
            {
                var target=Call!=null && Call.Runner?Call.Runner:NearestVehicle(Player.Position,40);
                if(target && Vector3.Distance(Player.Position,target.transform.position)<45)
                {
                    target.Traffic=false;target.AIControls=new Vector2(0,target.Speed>3?-1:0);
                    if(Call!=null && Call.Runner==target)Call.Warned=true;
                    Notify("Водитель тормозит. Подъезжай ближе для задержания.");
                }
            }
        }
        void TryInterrogate()
        {
            if(RoleIndex!=1 && RoleIndex!=2)return;
            if(interrogatable && interrogatable.Neutralized && Vector3.Distance(Player.Position,interrogatable.transform.position)<6)
            {
                interrogationSeed=Mathf.Abs(interrogatable.gameObject.GetInstanceID())*31+17;
                truthIndex=InterrogationTruth(interrogationSeed);
                Panel="interrogate";Time.timeScale=0;SetCursor(false);
                return;
            }
            Notify("Нет задержанного рядом. Оглуши тайзером (8) и надень наручники (F).");
        }
        public static int InterrogationTruth(int seed)=>Mathf.Abs(seed)%3;
        int interrogationSeed,truthIndex;HostileAgent interrogatable;
        static readonly string[] InterrogationTruths=
        {
            "Это мы устроили бойню у склада: оружие прятали под напольными досками в цеху.",
            "Каждую ночь вывозили ящики на фургоне без номеров через западные скалы.",
            "Сообщника зовут «Круглый»: красный Sport, шрам на левой брови, караулит у заправки."
        };
        static readonly string[] InterrogationLies=
        {
            "Я вообще первый раз в этом городе, тут никто меня не знает.",
            "Это не моя куртка и не мой телефон — у меня стальное алиби по всей области.",
            "Я с полицией на дружеской ноге: у меня справка прямо от прокурора."
        };
        public string InterrogationStatement(int index)
        {
            int truth=Mathf.Clamp(truthIndex,0,2);
            return index==truth?InterrogationTruths[interrogationSeed%InterrogationTruths.Length]:InterrogationLies[(interrogationSeed+index*7)%InterrogationLies.Length];
        }
        public void AnswerInterrogation(int index)
        {
            Panel="";Time.timeScale=1;SetCursor(true);
            if(interrogatable==null)return;
            if(index==truthIndex)
            {
                Money+=400;AwardXP(60);Notify("Признание получено: +$400. Подозреваемый сдал сообщника!");
                var accomplice=HostileAgent.Spawn(this,Player.Position+Player.transform.forward*35+Vector3.up,"Сообщник · награда $700",false);
                accomplice.Bounty=700;
            }
            else Notify("Допрос сорвался: подозреваемый ушёл в отказ.");
            bool debtor=transportSuspect==interrogatable.gameObject;
            if(debtor)transportSuspect=null;
            Destroy(interrogatable.gameObject);interrogatable=null;
        }
        void DrawInterrogatePanel(float left,float h)
        {
            if(interrogatable==null){Panel="";Time.timeScale=1;SetCursor(true);return;}
            Label(left,85,900,45,"ДОПРОС ПОДОЗРЕВАЕМОГО",heading);
            Label(left,135,1040,40,"Один из ответов — правда. Правдивое признание: +$400 и наводка на сообщника с наградой.",small);
            for(int i=0;i<3;i++)
            {
                float y=200+i*150;RectBox(new Rect(left,y,1080,130),new Color(.1f,.16f,.12f));
                Label(left+20,y+12,1040,66,InterrogationStatement(i),body);
                int index=i;if(Button(new Rect(left+20,y+84,300,36),(i+1)+". ЗАДАТЬ ЭТОТ ВОПРОС"))AnswerInterrogation(index);
            }
            if(Button(new Rect(left+780,h-160,280,42),"ПРЕРВАТЬ ДОПРОС")){Panel="";Time.timeScale=1;SetCursor(true);}
        }
        public void DrawDutyBanner(float w,float h)
        {
            if(Call==null)return;
            float bx=w/2-320;
            RectBox(new Rect(bx,86,640,54),new Color(.05f,.09f,.17f,.92f));
            GUI.color=new Color(.5f,.72f,1);
            Label(bx+16,90,606,25,"РАЦИЯ · "+Call.Code+"  ·  "+Call.Title,small);
            GUI.color=Color.white;
            Label(bx+16,112,612,26,Call.Description+"  ·  $"+Call.Reward+"  ·  "+Mathf.CeilToInt(Mathf.Max(0,Call.Expires-Time.unscaledTime))+" с",small);
        }
        public void DrawRaidBanner(float w,float h)
        {
            if(raidBus==null)return;
            float bx=w/2-320;
            RectBox(new Rect(bx,148,640,40),new Color(.14f,.07f,.04f,.92f));
            GUI.color=new Color(1,.6f,.35f);
            Label(bx+16,152,606,25,"РЕЙД · Останови конвойный фургон и охрану · $1400",small);
        }
        public void DrawLicenses(float left,float h)
        {
            Label(left,85,900,45,"ПРОДАЖА ЛИЦЕНЗИЙ",heading);
            Label(left,135,1040,40,"Клиент: "+(client?client.name:"—")+". У каждого клиента свой шанс покупки: чем дороже лицензия, тем сложнее уговорить.",small);
            for(int i=0;i<3;i++)
            {
                float y=200+i*150;RectBox(new Rect(left,y,1080,130),new Color(.1f,.18f,.14f));
                Label(left+20,y+12,700,34,LicenseNames[i],body);
                Label(left+20,y+54,700,60,LicenseDesc[i],small);
                int id=i;if(Button(new Rect(left+760,y+40,300,50),"ПРОДАТЬ · $"+LicensePrices[id]))SellLicense(id);
            }
            if(Button(new Rect(left+760,h-160,300,42),"ЗАВЕРШИТЬ СМЕНУ")){Panel="";Time.timeScale=1;SetCursor(true);}
        }
        public void SellLicense(int id,bool force=false)
        {
            if(!client){Notify("Клиент ушёл: дождись следующего прохожего.");return;}
            if(!force && Random.value>LicenseChances[Mathf.Clamp(id,0,2)]){Notify("Клиент отказался: «"+InterrogationLies[Random.Range(0,3)]+"»");return;}
            Money+=LicensePrices[id];AwardXP(30);client.name+=" · взял лицензию";client=null;
            Panel="";Time.timeScale=1;SetCursor(true);Notify("Лицензия продана: +$"+LicensePrices[id]);Save();
        }
        // Cuffed suspects are walked/driven to the prison zone and booked.
        GameObject transportSuspect;
        // Bandit raids: the police money convoy can be ambushed before it reaches the department.
        VehicleMotor raidBus;float raidUntil,raidHold,nextRaidAt=140;readonly List<HostileAgent> raidGuards=new List<HostileAgent>();
        void UpdateBanditRaid()
        {
            bool bandit=RoleIndex==9||RoleIndex==14;
            if(!bandit || InInterior)return;
            if(raidBus==null)
            {
                if(ActiveMission==null && Time.time>=nextRaidAt){StartBanditRaid();nextRaidAt=Time.time+Random.Range(120,180);}
                return;
            }
            if(Time.unscaledTime>raidUntil){EndBanditRaid(false);return;}
            var target=WorldBuilder.Places[1].Position;
            Vector3 delta=target-raidBus.transform.position;delta.y=0;
            if(delta.magnitude>20)
            {
                var local=raidBus.transform.InverseTransformDirection(delta.normalized);
                raidBus.AIControls=new Vector2(Mathf.Clamp(Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg/28,-1,1),.45f);
            }
            else raidBus.AIControls=Vector2.zero;
            int guards=0;foreach(var guard in raidGuards)if(guard && !guard.Neutralized)guards++;
            bool inPlace=Vector3.Distance(Player.Position,raidBus.transform.position)<11 && raidBus.Speed<7 && raidGuards.Count>0 && guards==0;
            raidHold=inPlace?raidHold+Time.deltaTime:0;
            if(raidHold>2.5f)EndBanditRaid(true);
        }
        void StartBanditRaid()
        {
            var at=DutyPoint();
            raidBus=WorldBuilder.SpawnCar(this,at+new Vector3(0,1,-4),"Конвойный фургон",new Color(.3f,.45f,.75f),false,Random.Range(0f,359f));
            raidBus.Traffic=false;raidBus.Durability=Mathf.Max(raidBus.Durability,70);
            raidGuards.Clear();
            raidGuards.Add(HostileAgent.Spawn(this,at+new Vector3(-5,1,7),"Охранник конвоя",false));
            raidGuards.Add(HostileAgent.Spawn(this,at+new Vector3(5,1,7),"Охранник конвоя",false));
            raidUntil=Time.unscaledTime+140;raidHold=0;
            Wanted=Mathf.Max(Wanted,2);wantedTimer=0;
            Notify("Радио Grove: по трассе идёт конвой полиции с деньгами. Останови фургон, нейтрализуй охрану — $1400.");
        }
        void EndBanditRaid(bool done)
        {
            if(done){Money+=1400;Completed++;AwardXP(90);Notify("Конвой остановлен: $1400 в руки Grove.");}
            else Notify("Конвой уехал в департамент. Рейд сорван.");
            foreach(var guard in raidGuards)if(guard && !guard.Neutralized)Destroy(guard.gameObject);
            raidGuards.Clear();
            if(raidBus){raidBus.AIControls=null;raidBus=null;}
            raidHold=0;
        }
        // Licensing center: on-street license sales to pedestrians.
        Pedestrian client;
        static readonly string[] LicenseNames={"ВОДИТЕЛЬСКАЯ ЛИЦЕНЗИЯ","КОММЕРЧЕСКАЯ ЛИЦЕНЗИЯ","ЛИЦЕНЗИЯ НА ОРУЖИЕ"};
        static readonly string[] LicenseDesc={
            "Базовая водительская лицензия: полный курс ПДД плюс практический зачёт. Продажа на месте.",
            "Право управления грузовым и коммерческим транспортом с отметкой штата. Ходатайство подписывает автошкола San Oro.",
            "Особая лицензия с курсом обучения и страховкой. Требует подписи шерифа и личного осмотра."};
        static readonly int[] LicensePrices={250,480,900};
        static readonly float[] LicenseChances={.65f,.45f,.3f};
        public Pedestrian NearestPedestrian(float radius)
        {
            Pedestrian best=null;
            foreach(var ped in Object.FindObjectsOfType<Pedestrian>())
            {
                if(!ped || !ped.isActiveAndEnabled)continue;
                float d=Vector3.Distance(Player.Position,ped.transform.position);
                if(d<radius){radius=d;best=ped;}
            }
            return best;
        }
        void TrySellLicenses()
        {
            if(RoleIndex!=11)return;
            if(Player.Vehicle){Notify("Выйди из автомобиля: торговля ведётся у прохожих.");return;}
            client=NearestPedestrian(7);
            if(!client){Notify("Рядом нет клиента. Остановись возле прохожего на улице.");return;}
            Panel="licenses";Time.timeScale=0;SetCursor(false);
        }
        // Mission-specific fleeing vehicle (chase step) handled alongside duty runners.
        void UpdatePursuitTarget()
        {
            if(!pursuitTarget || ActiveMission==null || !ActiveMission.Fleeing){ClearPursuitTarget();return;}
            if(Step>0){pursuitTarget.AIControls=new Vector2(0,pursuitTarget.Speed>2?-1:0);return;}
            Vector3 delta=pursuitTarget.transform.position-Player.Position;delta.y=0;
            if(delta.magnitude<8){pursuitTarget.AIControls=new Vector2(0,pursuitTarget.Speed>2?-1:0);return;}
            Vector3 worldFlee=-delta.normalized;
            float drift=0;
            if(Mathf.Abs(pursuitTarget.transform.position.x)>760)drift=Mathf.Sign(-pursuitTarget.transform.position.x);
            if(Mathf.Abs(pursuitTarget.transform.position.z)>760)drift=Mathf.Sign(pursuitTarget.transform.position.z<0?1:-1)*-1;
            if(drift!=0)worldFlee=Vector3.ProjectOnPlane(worldFlee+new Vector3(drift*.8f,0,0).normalized,Vector3.up).normalized;
            Vector3 local=pursuitTarget.transform.InverseTransformDirection(worldFlee);
            float steer=Mathf.Clamp(Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg/30,-1,1);
            pursuitTarget.AIControls=new Vector2(steer,.75f);
        }
        void ClearPursuitTarget()
        {
            if(!pursuitTarget)return;Cars.Remove(pursuitTarget);Destroy(pursuitTarget.gameObject);pursuitTarget=null;
        }
        VehicleMotor pursuitTarget;
    }
}
