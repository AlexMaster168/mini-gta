using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SunsetState
{
    [Serializable] public sealed class SaveData { public int ShootingXP,Difficulty,DifficultyVersion; public int Role,Money,Completed,Wanted,Contraband,Board,OwnedGuns,Licenses,Laws,Bank;public int[] Purchases,LoadedAmmo,ReserveAmmo; public int[] Stories,Choices,Career,Skins,Accessories,Shields,Courts; public bool[] Females;public bool Escaped,CasualClothes; public float X,Z,Health,Hour,Armor,Visa; }
    public sealed class Role
    {
        public string Name,Icon,Description;public Color Color;public int Pay;
        public Role(string name,string icon,string description,Color color,int pay){Name=name;Icon=icon;Description=description;Color=color;Pay=pay;}
    }
    public sealed class Mission
    {
        public string Title,Description; public Vector3[] Points; public string[] Steps; public int Reward; public bool Vehicle,Escape,Release,Combat,Fleeing; public float TimeLimit;
        public Mission(string title,string description,int reward,bool vehicle,Vector3[] points,params string[] steps){Title=title;Description=description;Reward=reward;Vehicle=vehicle;Points=points;Steps=steps;}
    }
    public sealed partial class GameRoot:MonoBehaviour
    {
        public static GameRoot Instance;
        public PlayerMotor Player;public Light Sun;public List<VehicleMotor> Cars=new List<VehicleMotor>();
        public bool Playing;public string Panel="";public int RoleIndex,Money=2500,Completed,Wanted;
        public float Hour=14;public Mission ActiveMission;public int Step;
        public readonly Role[] Roles={
            new Role("Гражданский","01","Курьер, исследователь города. Начни свою историю.",new Color(.83f,.98f,.46f),350),
            new Role("Полиция","02","Патрулируй улицы, проверяй вызовы, задерживай нарушителей.",new Color(.3f,.6f,1),900),
            new Role("ФБР","03","Наблюдение, сбор улик, секретные операции.",new Color(.5f,.6f,.9f),1400),
            new Role("Армия","04","Доставка снабжения и охрана рубежей штата.",new Color(.55f,.7f,.35f),1100),
            new Role("Медик","05","Выезжай на вызовы и доставляй пациентов в больницу.",new Color(1,.45f,.45f),850),
            new Role("Пожарный","06","Спасательные выезды и проверка безопасности зданий.",new Color(1,.6f,.25f),850),
            new Role("Таксист","07","Забирай пассажиров и отвози их по городу.",new Color(1,.84f,.3f),600),
            new Role("Дальнобойщик","08","Грузы, склады и длинные маршруты.",new Color(.65f,.8f,.9f),1000),
            new Role("Механик","09","Помогай водителям и доставляй запчасти.",new Color(.3f,.85f,.78f),750),
            new Role("Уличная банда","10","Рискованные сделки, контрабанда и уход от полиции.",new Color(.5f,.85f,.4f),1600),
            new Role("Мэрия","11","Работа с документами, инспекции и городские поручения.",new Color(.8f,.67f,1),1200),
            new Role("Лицензеры","12","Учебные маршруты, экзамены и доставка лицензий.",new Color(.4f,.88f,1),800),
            new Role("Охрана тюрьмы","13","Патрулирование, контроль периметра и конвоирование.",new Color(.6f,.68f,.78f),1000),
            new Role("Заключённый","14","Тюремные поручения, освобождение или рискованный побег.",new Color(1,.55f,.22f),250),
            new Role("Свободный бандит","15","Сам по себе: криминальные заказы, ограбления и погони.",new Color(.9f,.35f,.33f),1800),new Role("Адвокат","16","Защита подследственных, сбор доказательств и выступления в суде.",new Color(.55f,.72f,.87f),1100),new Role("Мафия","17","Сделки семьи, похищения и противостояние полиции.",new Color(.6f,.35f,.7f),1900),new Role("Губернатор","18","Управляй штатом, принимай законы и переживи похищения мафии.",new Color(.85f,.7f,.35f),1800)};
        string notice="";float noticeUntil,hold,wantedTimer,autosave;GameObject marker;Texture2D map;
        GUIStyle title,heading,body,small,button;bool stylesReady;Vector2 roleScroll;int missionVariant;
        static bool PreviewMode=>Array.IndexOf(Environment.GetCommandLineArgs(),"-sunset-preview")>=0;
        static bool IsTestRun=>SmokeMode||PreviewMode;
        static bool SmokeMode=>Array.IndexOf(Environment.GetCommandLineArgs(),"-sunset-smoke")>=0;
        string SavePath=>Path.Combine(IsTestRun?Application.temporaryCachePath:Application.persistentDataPath,SmokeMode?"sunset-smoke-save.json":PreviewMode?"sunset-preview-save.json":"sunset-save.json");
        public static bool HasSave=>File.Exists(Path.Combine(Application.persistentDataPath,"sunset-save.json"));
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Bootstrap()
        {
            if(!FindFirstObjectByType<GameRoot>())new GameObject("Sunset State").AddComponent<GameRoot>();
        }
        void Awake()
        {
            Instance=this;Application.targetFrameRate=60;
            if(IsTestRun)Application.runInBackground=true;
            if(!Camera.main){var cam=new GameObject("Main Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.gameObject.AddComponent<AudioListener>();}
            WorldBuilder.Build(this);
            var player=new GameObject("Player");Player=player.AddComponent<PlayerMotor>();Player.Initialize();player.transform.position=new Vector3(0,2,0);
            Camera.main.gameObject.AddComponent<FollowCamera>().Target=Player;
            InitializeCombat();
            gameObject.AddComponent<StateRadio>();
            CreateMap();SetCursor(false);
        }
        void StartGame(bool load)
        {
            if(load)Load();else{CancelMission();RoleIndex=0;CasualClothes=false;Money=2500;Completed=0;Wanted=0;Escaped=false;StoryProgress=new int[Roles.Length];StoryChoices=new int[Roles.Length];CareerXP=new int[Roles.Length];LoadOutfits(null,null,null);CourtOutcomes=new int[10];visaUntil=0;BankBalance=0;StateLaws=0;PersonalLicenses=0;OwnedGunMask=0;ShootingXP=0;Purchases=new int[6];Contraband=0;SelectedBoard=PersonalBoard.Skateboard;Hour=14;Player.Health=100;Player.Teleport(new Vector3(0,2,0));EquipForRole();}
            Playing=true;Panel="";Time.timeScale=1;SetCursor(true);Player.SetColor(Roles[RoleIndex].Color);
            if(!load && !SmokeMode){Panel="roles";Time.timeScale=0;SetCursor(false);}
            Notify("Добро пожаловать! Tab — профессии · J — работа · G — служебный · H — личный · U — доска · M — карта");
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.F11))ToggleFullscreen();
            if(!Playing)return;
            if(Input.GetKeyDown(KeyCode.Escape)){Toggle("pause");return;}
            if(Input.GetKeyDown(KeyCode.Tab))Toggle("roles");
            if(Input.GetKeyDown(KeyCode.M))Toggle("map");
            if(Input.GetKeyDown(KeyCode.J))Toggle("jobs");
            if(Input.GetKeyDown(KeyCode.G))Toggle("garage");
            if(Input.GetKeyDown(KeyCode.H))Toggle("personal-garage");
            if(Panel=="" && Input.GetKeyDown(KeyCode.U))Player.ToggleBoard();
            if(Input.GetKeyDown(KeyCode.K))Toggle("career");
            if(Input.GetKeyDown(KeyCode.P))Toggle("phone");
            if(Input.GetKeyDown(KeyCode.F7))Toggle("water-rescue");
            if(Panel!="")return;
            UpdateTraining();UpdateCombat();if(Panel!="")return;
            UpdateServiceTools();if(Panel!="")return;
            Hour=(Hour+Time.deltaTime*.012f)%24;
            float daylight=Mathf.Clamp01(Mathf.Sin((Hour-6)/12*Mathf.PI));Sun.intensity=.12f+daylight;
            Sun.transform.rotation=Quaternion.Euler(Hour*15-90,-35,0);
            RenderSettings.ambientLight=Color.Lerp(new Color(.18f,.23f,.33f),new Color(.58f,.65f,.68f),daylight);
            autosave+=Time.deltaTime;if(autosave>30){Save();autosave=0;}
            if(Wanted>0){wantedTimer+=Time.deltaTime;if(wantedTimer>((StateLaws&4)!=0?18:25)){Wanted--;wantedTimer=0;}}
            UpdatePursuit();UpdateWantedResponse();UpdateMission();UpdateDuty();UpdateRoadAdvice();UpdateHostage();UpdateGovernor();UpdateCompanion();UpdateTravel();UpdatePhone();UpdateWater();UpdateTrafficRules();
            if(Player.Health<=0 || Player.Position.y < -18){Player.Teleport(RoleHome(4));Player.Health=100;Money=Mathf.Max(0,Money-((StateLaws&1)!=0?0:300));Wanted=0;Notify("Ты в больнице. Медицинская помощь: $300");Save();}
            if(!WorldLimits.Contains(Player.Position)){Player.Teleport(new Vector3(0,2,0));Notify("Ты вернулся в город.");}
        }
        void UpdatePursuit()
        {
            if(Wanted==0 || InInterior){if(pursuer)pursuer.AIControls=null;return;}
            // A visible pursuing unit makes the wanted level affect free play.
            if(!pursuer){pursuer=WorldBuilder.SpawnCar(this,Player.Position+new Vector3(0,0,-65),"Police Pursuit",new Color(.2f,.35f,.65f),false,0);pursuer.Faction=1;WorldBuilder.AddServiceDetails(pursuer,1);HostileAgent.AttachToPatrol(this,pursuer);pursuer.SetEmergency(true);}
            var unit=pursuer.GetComponent<HostileAgent>();if(unit && unit.Neutralized){pursuer.AIControls=null;pursuer=null;return;}
            if(unit && unit.Stunned){pursuer.AIControls=Vector2.zero;return;}
            Vector3 delta=Player.Position-pursuer.transform.position;delta.y=0;
            Vector3 local=pursuer.transform.InverseTransformDirection(delta);float angle=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
            float throttle=delta.magnitude>9?(pursuer.Speed<60?.8f:0):pursuer.Speed>3?-1:0;
            pursuer.AIControls=new Vector2(Mathf.Clamp(angle/35,-1,1),throttle);
            if(delta.magnitude<7){arrest+=Time.deltaTime;if(arrest>6){Money=Mathf.Max(0,Money-500);CancelMission();RoleIndex=13;Escaped=false;Wanted=0;EquipForRole();Player.Teleport(WorldBuilder.Places[13].Position);Player.SetColor(Roles[13].Color);Notify("Задержание. Ты в тюрьме. J — тюремные задания или побег.");arrest=0;}}
            else arrest=Mathf.Max(0,arrest-Time.deltaTime);
        }
        VehicleMotor pursuer;float arrest;
        void Toggle(string name){Panel=Panel==name?"":name;Time.timeScale=Panel==""?1:0;SetCursor(Panel=="");}
        static void SetCursor(bool locked){Cursor.lockState=locked?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!locked;}
        public void Notify(string text){notice=text;noticeUntil=Time.unscaledTime+5;}
        public VehicleMotor NearestVehicle(Vector3 position,float radius)
        {VehicleMotor nearest=null;foreach(var car in Cars){if(!car || car.Driver)continue;float d=Vector3.Distance(position,car.transform.position);if(d<radius){nearest=car;radius=d;}}return nearest;}
        public bool TrySpecialAction()
        {
            if(TryRangeAction())return true;
            if(TryBlackMarketAction())return true;
            if(TryTravelAction())return true;
            if(TryInteriorAction())return true;
            if(TryPrisonAction())return true;
            if(TryNpcConversation())return true;
            foreach(Place place in WorldBuilder.Places)if(Vector3.Distance(Player.Position,place.Position)<7){if(place.Role==4){Player.Health=100;Notify("Здоровье восстановлено");}else{Toggle("roles");}return true;}
            return false;
        }
        public void Fire()
        {
            TryFireWeapon();
        }
        Vector3 Point(int x,int z)=>OnVice?ViceCityIsland.Center+new Vector3(x*120,0,z*120):new Vector3(x*200,1,z*200);
        Vector3[] Jitter(Vector3[] points){for(int i=0;i<points.Length-1;i++)points[i]+=new Vector3(UnityEngine.Random.Range(-45,45),0,UnityEngine.Random.Range(-45,45));return points;}
        public Mission MakeMission(int variant)
        {
            int v=Mathf.Abs(variant)%JobsPerRole;Vector3 home=RoleHome(RoleIndex);
            if(OnVice)return MakeViceMission(RoleIndex,v);
            if(RoleIndex==17)return MakeGovernorMission(v);
            if(RoleIndex==16)return MakeMafiaMission(v,0);
            if(RoleIndex==15)return MakeLawyerMission(v,0);
            if(v>=6)return MakeExtraJob(v,home);
            if(RoleIndex==13){if(v<3)return PrisonMission(v);return new Mission(new[]{"Уборка двора","Ремонт складов","Тюремная кухня"}[v-3],"Выполни работу в пределах тюрьмы и сдай отметки администрации.",300+v*50,false,new[]{new Vector3(1040,1,-460),new Vector3(1120,1,-420),new Vector3(1010,1,-410)},"Получи рабочее поручение","Выполни работу","Передай отметки администрации");}
            if(v==2 && RoleIndex<12 && RoleIndex!=8 && RoleIndex!=9 && RoleIndex!=11)
                return new Mission(new[]{"Междугородняя доставка","Патруль трёх городов","Межштатное расследование","Снабжение гарнизонов","Помощь трём городам","Спасательная инспекция","Междугороднее такси","Груз через весь штат","","","Муниципальная делегация"}[RoleIndex],"Посети Сан-Фиерро и Лас-Вентурас, затем вернись в Лос-Сантос. Для маршрута нужен транспорт.",Roles[RoleIndex].Pay*3,true,new[]{StateBuilder.SanFierro,StateBuilder.LasVenturas,home},"Прибудь в центр Сан-Фиерро","Прибудь в центр Лас-Вентураса","Вернись на базу в Лос-Сантосе");
            if(v>=3)return MakeAdvancedMission(v,home);
            switch(RoleIndex)
            {
                case 1:{var mission=new Mission(new[]{"Ночной патруль","Проверка вызова","Безопасные улицы"}[v],"Объедь отмеченные участки, проверь обстановку и вернись в департамент. Вызов требует соблюдения таймера.",900+(v==1?700:0),true,Jitter(new[]{Point(-2,-1-v),Point(v,1),home}),"Проверь первый участок","Оформи проверку второго участка","Вернись в департамент");if(v==1)mission.TimeLimit=200;return mission;}
                case 2:return new Mission(new[]{"Тихое наблюдение","След контрабанды","Дело № 47"}[v],"Посети точки наблюдения, собери данные и доставь отчёт в бюро.",1400,false,new[]{Point(-3+v,-2),Point(2,-1-v),home},"Собери данные на месте","Забери материалы информатора","Доставь материалы в ФБР");
                case 3:return new Mission(new[]{"Снабжение базы","Проверка периметра","Армейский конвой"}[v],"На служебном автомобиле проверь точку снабжения и доставь груз на базу.",1100,true,new[]{Point(3,-3+v),Point(4,1),home},"Забери снабжение","Пройди контрольный пункт","Доставь груз на базу");
                case 4:return new Mission(new[]{"Срочный вызов","Помощь на дороге","Дежурная смена"}[v],"Выезжай к пациенту, окажи первую помощь и доставь его в больницу.",850,true,new[]{Point(-2+v,-2),home},"Окажи помощь пациенту: остановись у маркера","Доставь пациента в больницу");
                case 5:return new Mission(new[]{"Спасательный выезд","Проверка гидрантов","Тревога на складе"}[v],"Прибудь на место, проведи спасательные работы и вернись в часть.",850,true,new[]{Point(-3+v,2),home},"Проведи спасательные работы у маркера","Вернись в пожарную часть");
                case 6:return new Mission(new[]{"Поездка в центр","Дальний пассажир","Вечерний тариф"}[v],"Забери пассажира и отвези его по адресу. Нужен автомобиль.",600,true,new[]{Point(-2,1+v),Point(2+v,-2)},"Забери пассажира","Отвези пассажира по адресу");
                case 7:return new Mission(new[]{"Продукты для города","Стройматериалы","Складской маршрут"}[v],"Забери груз на складе и выполни доставку в два пункта.",1000,true,new[]{home,Point(-3,2-v),Point(1+v,3)},"Загрузи автомобиль на складе","Доставь первую часть груза","Доставь оставшийся груз");
                case 8:return new Mission(new[]{"Выездной ремонт","Доставка запчастей","Помощь водителю"}[v],"Забери инструменты, помоги водителю и вернись в мастерскую.",750,true,new[]{home,Point(1+v,-3),home},"Забери инструменты","Проведи ремонт автомобиля у маркера","Вернись в мастерскую");
                case 9:return new Mission(new[]{"Рискованная посылка","Тёмная сделка","Уличный маршрут"}[v],"Забери посылку и доставь её в укрытие. Будь готов к полицейскому преследованию.",1600,true,new[]{Point(-3,1+v),Point(2+v,-3),home},"Забери посылку","Доставь посылку в укрытие","Вернись в свой район");
                case 10:return new Mission(new[]{"Городская инспекция","Документы для больницы","Муниципальный контракт"}[v],"Забери документы, посети муниципальный объект и передай отчёт в мэрию.",1200,true,new[]{home,Point(-1+v,2),home},"Забери документы в мэрии","Проведи инспекцию объекта","Передай отчёт в мэрию");
                case 11:return new Mission(new[]{"Практический экзамен","Учебный маршрут","Доставка лицензий"}[v],"Пройди контрольные точки учебного маршрута с остановками и вернись в автошколу.",800,true,new[]{Point(-1-v,-1),Point(1,2-v),home},"Пройди первый участок экзамена","Выполни остановку на втором участке","Вернись в центр лицензирования");
                case 12:return new Mission(new[]{"Контроль периметра","Конвоирование","Смена караула"}[v],"Проверь посты охраны и доставь рапорт в тюремную администрацию.",1000,false,new[]{new Vector3(950,1,-530),new Vector3(1170,1,-320),home},"Проверь южный пост","Проверь северный пост","Передай рапорт начальнику караула");
                case 14:return new Mission(new[]{"Заказ без имён","Налёт на склад","Последняя посылка"}[v],"Получи наводку, забери добычу у маркера склада и доберись до укрытия. После налёта начнётся розыск.",1800,true,new[]{Point(-2,1),Point(3-v,-2),home},"Встреться с информатором","Забери добычу со склада","Скройся в личном укрытии");
                default:return new Mission(new[]{"Первая подработка","Письмо через город","Городской курьер"}[v],"Забери документы и доставь их получателю. Можно пешком или на автомобиле.",350,false,new[]{Point(-1,1+v),Point(1+v,-1)},"Забери документы","Доставь документы получателю");
            }
        }
        Mission MakeAdvancedMission(int v,Vector3 home)
        {
            int pay=Roles[RoleIndex].Pay;bool fighters=Armed && (IsLaw||RoleIndex==3||RoleIndex==9||RoleIndex==13||RoleIndex==14);
            if(v==3){var mission=new Mission(StormTitles[RoleIndex],"Усиленный выезд: у точек замечены вооружённые группы. Возьми снаряжение и отработай участок.",(int)(pay*1.4f),true,new[]{Point(2,-1),Point(-1,2),home},"Отработай первую точку","Отработай вторую точку","Вернись на базу и сдай рапорт");mission.Combat=fighters;return mission;}
            if(v==4){var mission=new Mission(UrgentTitles[RoleIndex],"Экстренное поручение со счётчиком времени: уложись во все точки, иначе задание провалено.",(int)(pay*1.7f),true,new[]{Point(2,2),Point(-2,-1),home},"Успей к первой точке","Финальная точка маршрута","Доложи на базе"){TimeLimit=240};mission.Combat=fighters;return mission;}
            return new Mission(ChaseTitles[RoleIndex],"Цель уходит на автомобиле: догони и прижми её к маркеру, остановив, затем сдай результат.",(int)(pay*2.1f),true,new[]{Point(3,-2),Point(-2,2)},"Догоняй нарушителя у маркера","Доставь задержанного к точке сдачи"){Fleeing=true};
        }
        static readonly string[] StormTitles={"Срочная доставка по району","Штурм притона","Штурм явки","Учения: прорыв обороны","Массовое ДТП","Ночной выезд","Три заказа подряд","Двойной рейс","Ночная спецзаявка","Силовой откат","Особая комиссия","Экзамен повышенной сложности","Штурм блока","","Мокрая тема"};
        static readonly string[] UrgentTitles={"Три отметки за полчаса","Тревога: 10-31","Мобилизация: 10-13","Тревога: 10-23","Жёлтая зона: 10 минут","Горящий квартал: таймер","Час пик","Часовая партия","Час до закрытия СТО","Перестрелка на сундуках","Экстренная сессия","Экзамен на время","Смена под угрозой","","Обчистить контору за час"};
        static readonly string[] ChaseTitles={"Погоня за заёмщиком","Погоня 10-80","Перехват конвоя","Контр-перехват","Догони уносивших кровь","Погоня за поджигателем","Убегающий клиент","Догони рейс-нарушителя","Догони заявителя","Догоняй стукача","Догоняй фургон с актами","Нарушитель ПДД","Догоняй беглеца","","Догоняй предателя"};
        void AcceptMission(int variant)
        {
            activeCampaignRole=-1;ActivateMission(MakeMission(variant));
        }
        void ActivateMission(Mission mission)
        {
            ClearHostage();courtResolved=false;roadRepairTarget=null;ActiveMission=mission;Step=0;hold=0;missionTime=ActiveMission.TimeLimit;Panel="";Time.timeScale=1;SetCursor(true);
            if(!marker){marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder);marker.GetComponent<Collider>().enabled=false;Destroy(marker.GetComponent<Collider>());marker.GetComponent<Renderer>().sharedMaterial=WorldBuilder.Mat(new Color(.82f,1,.3f));}
            gpsDestination=null;PhoneMessage(StoryBook.Stories[RoleIndex].Contact,ActiveMission.Title+"\n"+ActiveMission.Steps[0]);marker.SetActive(true);MoveMarker();Notify("Задание принято: "+ActiveMission.Title);
            ActiveMission.Combat=ActiveMission.Combat||(activeCampaignRole>=0&&Armed);
            SpawnMissionEnemies();PrepareMafiaHostage();
            if(ActiveMission.Fleeing)
            {
                bool tank=RoleIndex==3;
                pursuitTarget=WorldBuilder.SpawnCar(this,ActiveMission.Points[0]+new Vector3(0,1,-26),tank?"Уклоняющийся танк":"Цель погони",new Color(.8f,.15f,.12f),false,90,tank?VehicleKind.Tank:VehicleKind.Sedan);
                pursuitTarget.Traffic=false;
            }
            PrepareServiceObjective();
        }
        void MoveMarker(){marker.transform.position=ActiveMission.Points[Step]+Vector3.up*2;marker.transform.localScale=new Vector3(5,2,5);}
        void UpdateMission()
        {
            if(TrainingActive)return;
            if(ActiveMission==null){ClearPursuitTarget();return;}
            marker.transform.Rotate(0,35*Time.deltaTime,0);
            if(ActiveMission.TimeLimit>0)
            {
                missionTime-=Time.deltaTime;
                if(missionTime<=0){CancelMission();Notify("Задание провалено: время вышло.");return;}
            }
            bool close,stopped;
            if(ActiveMission.Fleeing && Step==0 && pursuitTarget)
            {
                marker.transform.position=pursuitTarget.transform.position+Vector3.up*2;
                close=Vector3.Distance(Player.Position,pursuitTarget.transform.position)<13;
                stopped=pursuitTarget.Speed<6;
            }
            else
            {
                close=Vector3.Distance(Player.Position,ActiveMission.Points[Step])<10;
                stopped=!Player.Vehicle || Player.Vehicle.Speed<6;
            }
            if(Step==0 && MissionThreatsRemain){hold=0;return;}
            if(roadRepairTarget && roadRepairTarget.Broken && Step==0){hold=0;return;}
            if(BankStageBlocked){hold=0;return;}
            if(ServiceStageBlocked){hold=0;return;}
            if(HostageStageBlocked){hold=0;return;}
            if(LawyerStageBlocked(close,stopped)){hold=0;return;}
            if(close && stopped && (!ActiveMission.Vehicle || Player.Vehicle))hold+=Time.deltaTime;else hold=0;
            if(hold<3)return;
            Step++;hold=0;
            OnPrisonMissionStep();
            if((RoleIndex==9 && Step==1)||(RoleIndex==14 && Step==2)){Wanted=3;wantedTimer=0;Notify("Полиция вышла на след. Уходи от преследования!");}
            if(Step>=ActiveMission.Points.Length)
            {
                bool release=ActiveMission.Release;int campaign=activeCampaignRole;int payout=MissionPay(ActiveMission.Reward);Money+=payout;Completed++;AwardXP(campaign>=0?220:100);Notify("Задание выполнено! +$"+payout);ClearMissionEnemies();ClearServiceObjective();ClearPursuitTarget();ActiveMission=null;activeCampaignRole=-1;marker.SetActive(false);missionVariant++;if(release){RoleIndex=0;Escaped=false;Wanted=0;Player.Teleport(WorldBuilder.Places[0].Position);Player.SetColor(Roles[0].Color);Notify("Поручение выполнено. Ты освобождён!");}if(campaign>=0)CompleteStoryChapter(campaign);Save();
            }
            else{MoveMarker();Notify(ActiveMission.Steps[Step]);}
        }
        float missionTime;
        void CancelMission(){ClearHostage();roadRepairTarget=null;ActiveMission=null;activeCampaignRole=-1;hold=0;ClearMissionEnemies();ClearServiceObjective();ClearPursuitTarget();if(marker)marker.SetActive(false);}
        void SelectRole(int index)
        {
            StopTraining(false);ClearGovernorRescue();ClearCustody();CancelMission();RoleIndex=index;CasualClothes=false;Escaped=false;Wanted=index==9||index==14?1:0;Player.Teleport(RoleHome(index)+Vector3.right*4);Player.SetColor(Roles[index].Color);
            EquipForRole();
            storyTab=true;Panel=SmokeMode?"":"jobs";Time.timeScale=SmokeMode?1:0;SetCursor(SmokeMode);Notify("Твоя роль: "+Roles[index].Name+". Выбери свою историю.");Save();
        }
        public void Save()
        {
            try{var s=new SaveData{ShootingXP=ShootingXP,Difficulty=DifficultyIndex,DifficultyVersion=1,Courts=CourtOutcomes,Visa=VisaRemaining,Bank=BankBalance,Licenses=PersonalLicenses,OwnedGuns=OwnedGunMask,Purchases=Purchases,LoadedAmmo=SavedMagazines,ReserveAmmo=SavedReserves,Females=FemaleChoices,Skins=SkinChoices,Accessories=AccessoryChoices,Shields=ShieldChoices,Laws=StateLaws,Role=RoleIndex,Money=Money,Contraband=Contraband,Board=(int)SelectedBoard,Completed=Completed,X=SavePosition.x,Z=SavePosition.z,Health=Player.Health,Hour=Hour,Armor=Armor,Escaped=Escaped,CasualClothes=CasualClothes,Wanted=Wanted,Stories=StoryProgress,Choices=StoryChoices,Career=CareerXP};File.WriteAllText(SavePath,JsonUtility.ToJson(s));}
            catch(Exception e){Debug.LogWarning("Save failed: "+e.Message);Notify("Не удалось сохранить игру: "+e.Message);}
        }
        void Load()
        {
            try{StopTraining(false);var s=JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));RestoreDifficulty(s);ShootingXP=Mathf.Clamp(s.ShootingXP,0,1000);LoadOutfits(s.Skins,s.Accessories,s.Shields,s.Females);Contraband=Mathf.Clamp(s.Contraband,0,20);SelectedBoard=(PersonalBoard)Mathf.Clamp(s.Board,0,2);RoleIndex=Mathf.Clamp(s.Role,0,Roles.Length-1);CourtOutcomes=s.Courts??new int[10];Array.Resize(ref CourtOutcomes,10);visaUntil=Time.realtimeSinceStartup+Mathf.Clamp(s.Visa,0,1200);BankBalance=Mathf.Max(0,s.Bank);StateLaws=s.Laws&7;PersonalLicenses=s.Licenses&7;OwnedGunMask=s.OwnedGuns&71;Money=Mathf.Max(0,s.Money);Completed=Mathf.Max(0,s.Completed);Hour=s.Hour%24;CasualClothes=s.CasualClothes;EquipForRole();LoadShop(s.OwnedGuns,s.Purchases,s.LoadedAmmo,s.ReserveAmmo);Armor=Mathf.Clamp(s.Armor,0,MaxArmor);LoadCareer(s.Career);Escaped=s.Escaped;Wanted=Mathf.Clamp(s.Wanted,0,5);StoryProgress=s.Stories??new int[Roles.Length];StoryChoices=s.Choices??new int[Roles.Length];Array.Resize(ref StoryProgress,Roles.Length);Array.Resize(ref StoryChoices,Roles.Length);for(int i=0;i<Roles.Length;i++){StoryProgress[i]=Mathf.Clamp(StoryProgress[i],0,StoryBook.ChapterCount);StoryChoices[i]=Mathf.Clamp(StoryChoices[i],0,StoryBook.ChoiceMask);}Player.Health=Mathf.Clamp(s.Health,1,100);Player.Teleport(new Vector3(Mathf.Clamp(s.X,WorldLimits.MinX+10,WorldLimits.MaxX-10),2,Mathf.Clamp(s.Z,WorldLimits.MinZ+10,WorldLimits.MaxZ-10)));}
            catch(Exception e){Debug.LogWarning(e);Notify("Сохранение недоступно. Начата новая игра.");}
        }
        void OnApplicationQuit(){if(Playing)Save();}
        void OnApplicationFocus(bool focused){if(!focused && !IsTestRun && Playing && Panel=="")Toggle("pause");}
        void CreateMap()
        {
            map=new Texture2D(1024,1024);map.filterMode=FilterMode.Bilinear;
            Color deep=new Color(.07f,.23f,.4f),mid=new Color(.13f,.38f,.5f),surf=new Color(.23f,.52f,.6f),shore=new Color(.84f,.77f,.56f);
            Color grassA=new Color(.44f,.52f,.35f),grassB=new Color(.37f,.46f,.29f);
            Color desertA=new Color(.75f,.66f,.47f),desertB=new Color(.66f,.57f,.4f),mountain=new Color(.48f,.42f,.34f);
            Color blockA=new Color(.42f,.43f,.39f),blockB=new Color(.51f,.47f,.41f),downtown=new Color(.57f,.57f,.55f),park=new Color(.24f,.41f,.25f);
            Color road=new Color(.6f,.61f,.56f),avenue=new Color(.52f,.53f,.49f),highwayCol=new Color(.64f,.63f,.56f),runwayCol=new Color(.42f,.45f,.48f);
            Vector2 sf=new Vector2(StateBuilder.SanFierro.x,StateBuilder.SanFierro.z),lv=new Vector2(StateBuilder.LasVenturas.x,StateBuilder.LasVenturas.z);
            for(int y=0;y<1024;y++)for(int x=0;x<1024;x++)
            {
                float wx=x/1023f*WorldLimits.Width+WorldLimits.MinX,wz=y/1023f*WorldLimits.Depth+WorldLimits.MinZ;
                Vector2 pt=new Vector2(wx,wz);Color c;
                bool southDesert=wz<-940&&wz>-1700,lvDesert=Vector2.Distance(pt,lv)<1150,sfRegion=Vector2.Distance(pt,sf)<1020;
                if(wx<-935 && wx>-1350 && Mathf.Abs(wz)<1250){float t=Mathf.InverseLerp(-935,-1360,wx);c=Color.Lerp(surf,t>.35f?deep:mid,t);}
                else if(wx<-900 && wx>-945 && Mathf.Abs(wz)<1250)c=shore;
                else if(lvDesert)c=Color.Lerp(desertA,desertB,Mathf.PerlinNoise(wx*.0028f,wz*.0028f));
                else if(southDesert)c=Color.Lerp(desertA,mountain,Mathf.PerlinNoise(wx*.004f+9,wz*.004f)*.9f);
                else if(Mathf.Abs(wx)<2100&&wz>2450)c=mountain;
                else if(sfRegion)c=Color.Lerp(grassA,new Color(.34f,.44f,.3f),Mathf.PerlinNoise(wx*.003f,wz*.003f));
                else c=Color.Lerp(grassA,grassB,Mathf.PerlinNoise(wx*.0025f,wz*.0025f));
                if(Mathf.Abs(wx-ViceCityIsland.Center.x)<1150&&Mathf.Abs(wz-ViceCityIsland.Center.z)<1300)c=mid;
                if(Mathf.Abs(wx-ViceCityIsland.Center.x)<800&&Mathf.Abs(wz-ViceCityIsland.Center.z)<900){c=shore;if((Mathf.Abs((wx-ViceCityIsland.Center.x)/160-Mathf.Round((wx-ViceCityIsland.Center.x)/160))<.09f||Mathf.Abs((wz-ViceCityIsland.Center.z)/160-Mathf.Round((wz-ViceCityIsland.Center.z)/160))<.09f)&&Mathf.Abs(wx-ViceCityIsland.Center.x)<720&&Mathf.Abs(wz-ViceCityIsland.Center.z)<720)c=road;}
                // Los Santos blocks, downtown core and parks
                if(Mathf.Abs(wx)<810&&Mathf.Abs(wz)<810&&!southDesert)
                {
                    int cx=Mathf.RoundToInt((wx-100)/200),cz=Mathf.RoundToInt((wz-100)/200);
                    bool parkCell=(cx==-2&&cz==0)||(cx==1&&cz==2)||(cx==-4&&cz==-1);
                    if(parkCell)c=park;
                    else if(Mathf.Abs(wx)<320&&Mathf.Abs(wz)<320)c=downtown;
                    else c=Color.Lerp(blockA,blockB,Mathf.PerlinNoise(wx*.006f,wz*.006f));
                }
                // San Fierro hills shading and harbour, Venturas neon glow
                if(sfRegion && wz>2100-260&&wz<2100+1100&&wx<-2900){c=Color.Lerp(c,new Color(.3f,.4f,.3f),.7f);}
                if(sfRegion && Mathf.Abs(wz-sf.y)<430&&wx<sf.x-540&&wx>sf.x-800)c=new Color(.34f,.37f,.39f);
                if(Vector2.Distance(pt,lv)<980&&(Mathf.PerlinNoise(wx*.01f,wz*.01f)>.82f))c=Color.Lerp(c,new Color(1,.85f,.55f),.8f);
                // desert mountains cluster around (-850..850, -1500)
                if(Mathf.Abs(wx)<900&&wz<-1430)c=mountain;
                // Military compound and prison zones
                if(Vector2.Distance(pt,new Vector2(630,800))<190)c=new Color(.36f,.45f,.27f);
                if(Mathf.Abs(pt.x-1080)<130&&Mathf.Abs(pt.y+430)<130)c=new Color(.5f,.42f,.36f);
                // grove district tint
                if(Vector2.Distance(pt,new Vector2(-570,-600))<150)c=new Color(.36f,.46f,.28f);
                // Runway strip south of Los Santos
                if(Mathf.Abs(wx)<120&&Mathf.Abs(wz+1080)<16)c=runwayCol;
                if(Mathf.Abs(wx)<10&&wz<-880&&wz>-1020)c=road;
                // Roads
                bool lsRoad=Mathf.Abs(wx)<830&&Mathf.Abs(wz)<830&&(Mathf.Abs(wx-Mathf.Round(wx/200)*200)<13||Mathf.Abs(wz-Mathf.Round(wz/200)*200)<13);
                bool sfRoad=Vector2.Distance(pt,sf)<820&&(Mathf.Abs((wx-sf.x)-Mathf.Round((wx-sf.x)/160)*160)<11||Mathf.Abs((wz-sf.y)-Mathf.Round((wz-sf.y)/160)*160)<11);
                bool lvRoad=Vector2.Distance(pt,lv)<820&&(Mathf.Abs((wx-lv.x)-Mathf.Round((wx-lv.x)/160)*160)<11||Mathf.Abs((wz-lv.y)-Mathf.Round((wz-lv.y)/160)*160)<11);
                if(lsRoad)c=road;else if(sfRoad||lvRoad)c=avenue;
                bool highway=false;
                foreach(var route in StateBuilder.Routes)for(int r=0;r<route.Length-1;r++)if(SegmentDistance(pt,new Vector2(route[r].x,route[r].z),new Vector2(route[r+1].x,route[r+1].z))<15)highway=true;
                if(highway)c=highwayCol;
                if(StateBuilder.ExpansionRoad(pt))c=highwayCol;
                // The original red bridge on the LS—SF route
                Vector3 bridge=Vector3.Lerp(StateBuilder.Routes[0][1],StateBuilder.Routes[0][2],.56f);
                if(SegmentDistance(pt,new Vector2(bridge.x,bridge.z),new Vector2(bridge.x+40,bridge.z+55))<9)c=new Color(.8f,.2f,.12f);
                map.SetPixel(x,y,c);
            }map.Apply();
        }
        static float SegmentDistance(Vector2 p,Vector2 a,Vector2 b){Vector2 delta=b-a;return Vector2.Distance(p,a+delta*Mathf.Clamp01(Vector2.Dot(p-a,delta)/delta.sqrMagnitude));}
        void Styles()
        {
            if(stylesReady)return;stylesReady=true;
            title=new GUIStyle(GUI.skin.label){fontSize=66,fontStyle=FontStyle.Bold};
            heading=new GUIStyle(GUI.skin.label){fontSize=24,fontStyle=FontStyle.Bold};
            body=new GUIStyle(GUI.skin.label){fontSize=15,wordWrap=true};small=new GUIStyle(body){fontSize=12};
            button=new GUIStyle(GUI.skin.button){fontSize=15,fontStyle=FontStyle.Bold,wordWrap=true,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(18,14,10,10)};
        }
        void RectBox(Rect rect,Color color){Color old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
        bool Button(Rect rect,string text){GUI.backgroundColor=new Color(.48f,.65f,.28f);bool result=GUI.Button(rect,text,button);GUI.backgroundColor=Color.white;return result;}
        void Label(float x,float y,float w,float h,string text,GUIStyle style=null){GUI.Label(new Rect(x,y,w,h),text,style??body);}
        void OnGUI()
        {
            if(PreviewMode)PreviewRepaint();Styles();float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);
            float w=Screen.width/scale,h=Screen.height/scale;
            if(!Playing){DrawMenu(w,h);return;}
            DrawHud(w,h);
            if(Panel!="")DrawPanel(w,h);
            if(Time.unscaledTime<noticeUntil){RectBox(new Rect(w/2-300,23,600,48),new Color(.12f,.2f,.14f,.95f));GUI.color=Roles[0].Color;Label(w/2-283,35,566,38,notice,small);GUI.color=Color.white;}
        }
        void DrawMenu(float w,float h)
        {
            RectBox(new Rect(0,0,w,h),new Color(.035f,.075f,.055f,.91f));
            if(Button(new Rect(w-360,35,330,42),"F11 · "+(Screen.fullScreen?"ПЕРЕЙТИ В ОКНО":"НА ВЕСЬ ЭКРАН")))ToggleFullscreen();
            GUI.color=Roles[0].Color;Label(80,65,500,30,"SINGLE PLAYER  /  SAN ORO COUNTY",small);GUI.color=Color.white;
            Label(75,120,600,95,"SUNSET",title);GUI.color=Roles[0].Color;Label(75,190,600,95,"STATE",title);GUI.color=Color.white;
            Label(80,310,470,90,"Твоя история. Твои правила.\nИсследуй город, меняй профессии, выполняй задания\nи решай, по какую сторону закона жить.");
            if(Button(new Rect(80,425,345,52),"НАЧАТЬ НОВУЮ ИГРУ   →"))StartGame(false);
            if(HasSave && Button(new Rect(80,490,345,52),"ПРОДОЛЖИТЬ ИСТОРИЮ"))StartGame(true);
            DrawDifficultyChoices(650,180,550);
            Label(80,h-110,1100,25,"18 РОЛЕЙ И ИСТОРИЙ     /     3 ГОРОДА + VICE CITY     /     ЛИЧНЫЙ И СЛУЖЕБНЫЙ ТРАНСПОРТ     /     ОФЛАЙН",small);
            Label(80,h-75,1000,45,"Авторский 3D-прототип. Не связан с Rockstar Games или Arizona RP.\nWASD · мышь · E — машина · Tab — профессии · J — задания · M — карта · G — служебный · H — личный · U — доска",small);
        }
        void DrawHud(float w,float h)
        {
            RectBox(new Rect(25,25,360,80),new Color(.04f,.08f,.06f,.8f));GUI.color=Roles[0].Color;Label(42,33,320,35,"SUNSET STATE",heading);GUI.color=Color.white;Label(43,75,330,25,Roles[RoleIndex].Name.ToUpper()+" / "+RankTitle,small);
            Label(w-230,28,210,32,"$ "+Money.ToString("N0"),heading);Label(w-230,65,210,25,((int)Hour).ToString("00")+":"+((int)(Hour%1*60)).ToString("00")+"   |   Розыск: "+Wanted,small);
            RectBox(new Rect(w-325,120,300,155),new Color(.04f,.08f,.06f,.9f));
            if(ActiveMission!=null){GUI.color=Roles[0].Color;Label(w-308,133,270,25,"ЗАДАНИЕ  /  "+(Step+1)+" ИЗ "+ActiveMission.Points.Length+(ActiveMission.TimeLimit>0?"  ·  "+Mathf.FloorToInt(Mathf.Max(0,missionTime)/60)+"%"+":"+(missionTime%60).ToString("00"):""),small);GUI.color=Color.white;Label(w-308,160,265,38,ActiveMission.Title);Label(w-308,197,265,65,ActiveMission.Steps[Step]+"\n"+Mathf.RoundToInt(Vector3.Distance(Player.Position,ActiveMission.Points[Step]))+" м  ·  $"+ActiveMission.Reward,small);}
            else{Label(w-308,137,265,35,"Город ждёт тебя",heading);Label(w-308,185,265,85,"J — взять задание · G — служебный · H — личный · U — доска\nTab — выбрать профессию\nM — открыть карту",small);}
            DrawMap(new Rect(25,h-210,210,175),false);
            DrawGoalGuide(w,h);
            DrawCombatHud(w,h);DrawTrainingHud();
            if(Player.Vehicle)Label(w-330,h-215,310,30,"ТРАНСПОРТ: "+Mathf.CeilToInt(Player.Vehicle.Durability)+" / "+Player.Vehicle.MaxDurability+" HP",small);
            DrawDutyBanner(w,h);
            DrawRaidBanner(w,h);
            Label(420,62,500,28,"P — телефон / GPS"+(OnVice?" · VICE CITY · виза "+Mathf.CeilToInt(VisaRemaining/60)+" мин":"")+((RoleIndex==1||RoleIndex==2)?" · F6 — вызовы":""),small);
            RectBox(new Rect(255,h-82,200,7),new Color(.3f,.1f,.1f));RectBox(new Rect(255,h-82,200*Mathf.Clamp01(Player.Health/100),7),Roles[0].Color);
            Label(255,h-112,250,28,"ЗДОРОВЬЕ  "+Mathf.CeilToInt(Player.Health)+" · Q — аптечка",small);
            Label(255,h-65,790,50,Player.BoardRiding?"ДОСКА: WASD — разгон / поворот · Пробел — тормоз · U — убрать\nH — личный гараж · G — служебный гараж":Player.Vehicle && Player.Vehicle.Kind==VehicleKind.Tank?"ТАНК: Z/X — башня · PageUp/Down — ствол · ПКМ — прицел · ЛКМ — огонь\nWASD — езда · E — выход · H — личный гараж · G — служебный":Player.Vehicle&&Player.Vehicle.Flying?"ВЕРТОЛЁТ: W/S — вперёд/назад · A/D — поворот · Пробел — вверх · Ctrl — вниз\nE — выход после посадки · G — служебный · H — личный · U — доска · O — маячки · Esc — пауза":Player.Vehicle?"W — газ · S — тормоз / задний ход · A/D — руль · Пробел — ручник\nЛКМ — стрельба (1–4) · R — перезарядка · T/Y — радио · E — выход":"WASD — движение · Shift — бег · E — действие · F — инструмент / наручники\nTab — роли · J — задания · M — карта · G — служебный · H — личный · U — доска · K — карьера"+(IsLaw?"  ·  N — мегафон · C — допрос":"")+(RoleIndex==11?"  ·  L — продажа лицензий":"")+(RoleIndex==9||RoleIndex==14?"  ·  следи за конвоями полиции":""),small);
            Label(w-220,h-100,200,60,Player.Vehicle?Mathf.RoundToInt(Player.Vehicle.Speed)+" КМ/Ч":"ПЕШКОМ",heading);
            if(Player.Vehicle)Label(w-220,h-65,210,28,Player.Vehicle.Flying?"ВОЗДУШНЫЙ ТРАНСПОРТ":"ПЕРЕДАЧА "+(Player.Vehicle.Gear<0?"R":Player.Vehicle.Gear.ToString())+" · "+Mathf.RoundToInt(Player.Vehicle.RPM)+" RPM",small);
            if(StateRadio.Current!="")Label(420,32,500,25,"♫ "+StateRadio.Current+"  ·  T / Y",small);
            if(Swimming||breath<15)Label(420,92,500,30,"ВОЗДУХ: "+Mathf.CeilToInt(breath)+" С · F7 — спасение / эвакуация",small);
            if(!Player.Vehicle||CanDriveBy)DrawCrosshair(w,h);
            string prompt=TrainingActive?"E — закончить тренировку":NearbyRange()>=0?"E — тир: тренировка стрельбы":NearbyBlackMarket()>=0?"E — Гуррам: чёрный рынок / снять розыск":InteriorPrompt();
            if(Player.Vehicle)prompt="E — выйти из автомобиля";
            else if(prompt==""){foreach(var p in WorldBuilder.Places)if(Vector3.Distance(Player.Position,p.Position)<7)prompt="E — "+p.Name;if(prompt=="" && NearestVehicle(Player.Position,5))prompt="E — сесть в автомобиль";}
            if(ActiveMission!=null && Vector3.Distance(Player.Position,ActiveMission.Points[Step])<12)prompt=ActiveMission.Vehicle && !Player.Vehicle?"Для задания нужен автомобиль":Player.Vehicle && Player.Vehicle.Speed>=6?"Остановись у маркера":"Выполнение: "+Mathf.FloorToInt(hold/3*100)+"%";
            if(ActiveMission!=null && Vector3.Distance(Player.Position,ActiveMission.Points[Step])<25 && ServiceStageBlocked)prompt=RoleIndex==4?"Выйди из машины · 8 / 0 — помощь · 9 — носилки · F — применить":RoleIndex==5?"8 — огнетушитель · 9 — топор · F — применить":"8 — ремкомплект · F — ремонт рядом с машиной";
            if(ActiveMission!=null && Step==0 && MissionThreatsRemain && Vector3.Distance(Player.Position,ActiveMission.Points[0])<50)prompt=IsLaw?"Нейтрализуй угрозу · 1–7 — оружие · 8 — тайзер · F — наручники":"Вооружённые противники · 1–7 — оружие · R — перезарядка";
            if(prompt!=""){RectBox(new Rect(w/2-260,h-170,520,42),new Color(.04f,.08f,.06f,.92f));Label(w/2-245,h-160,490,30,prompt);}
            if(arrest>0 && Wanted>0)Label(w/2-180,90,420,30,"Задержание: уезжай от патруля! "+Mathf.CeilToInt(6-arrest),body);
        }
        Vector2 MapPoint(Rect rect,Vector3 point,bool full=true)
        {
            if(full && !mapFocused)return new Vector2(rect.x+(point.x-WorldLimits.MinX)/WorldLimits.Width*rect.width,rect.y+(1-(point.z-WorldLimits.MinZ)/WorldLimits.Depth)*rect.height);
            Vector3 center=full?mapCenter:Player.Position;float span=full?mapSpan:1300;
            return new Vector2(rect.center.x+(point.x-center.x)/span*rect.width,rect.center.y-(point.z-center.z)/span*rect.height);
        }

        void DrawCrosshair(float w,float h)
        {
            if(!Armed || ServiceSelected || WeaponIndex==8 || WeaponIndex==9)return;
            var camera=Camera.main;if(!camera)return;
            var target=FindAutoTarget(camera,AutoAimRange);bool hostile=target;
            Vector2 point=AimScreenPoint(camera,target,w,h);
            Color tint=hostile?new Color(1,.22f,.15f,.98f):new Color(1,1,1,.9f);
            Color shadow=new Color(0,0,0,.55f);
            float cx=point.x,cy=point.y;
            void Tick(float x,float y,float width,float height)=>RectBox(new Rect(x,y,width,height),tint);
            void Outline(float x,float y,float width,float height)=>RectBox(new Rect(x,y,width,height),shadow);
            // Draw an outlined centre dot and four ticks so the aim point stays readable on any background.
            Outline(cx-3,cy-3,6,6);Tick(cx-2,cy-2,4,4);
            Outline(cx-2,cy-17,4,11);Tick(cx-1,cy-16,2,9);
            Outline(cx-2,cy+7,4,12);Tick(cx-1,cy+8,2,10);
            Outline(cx-17,cy-2,11,4);Tick(cx-16,cy-1,9,2);
            Outline(cx+7,cy-2,12,4);Tick(cx+8,cy-1,10,2);
            if(!hostile)return;
            // Lock brackets flash around the crosshair when auto-aim is holding a target.
            void Bracket(float x,float y,int sx,int sy){Outline(x-1,y-sy,9,4);Tick(x,y-sy/2,7,2);Outline(x-sx,y-1,4,9);Tick(x-sx/2,y,2,7);}
            Bracket(cx-27,cy-27,1,1);Bracket(cx+16,cy-27,1,1);Bracket(cx-27,cy+18,1,-1);Bracket(cx+16,cy+18,1,-1);
        }
        void DrawMap(Rect rect,bool full)
        {
            mapCaptions.Clear();
            if(full && !mapFocused)GUI.DrawTexture(rect,map);else if(full)GUI.DrawTextureWithTexCoords(rect,map,new Rect((mapCenter.x-mapSpan*.5f-WorldLimits.MinX)/WorldLimits.Width,(mapCenter.z-mapSpan*.5f-WorldLimits.MinZ)/WorldLimits.Depth,mapSpan/WorldLimits.Width,mapSpan/WorldLimits.Depth));else GUI.DrawTextureWithTexCoords(rect,map,new Rect((Player.Position.x-650-WorldLimits.MinX)/WorldLimits.Width,(Player.Position.z-650-WorldLimits.MinZ)/WorldLimits.Depth,1300f/WorldLimits.Width,1300f/WorldLimits.Depth));
            DrawMapDetails(rect,full);
            foreach(var p in WorldBuilder.Places){Vector2 at=MapPoint(rect,OnVice?ViceCityIsland.Homes[p.Role]:p.Position,full);if(rect.Contains(at))RectBox(new Rect(at.x-3,at.y-3,6,6),p.Color);}
            if(ActiveMission!=null){var at=MapPoint(rect,ActiveMission.Points[Step],full);at.x=Mathf.Clamp(at.x,rect.x+6,rect.xMax-6);at.y=Mathf.Clamp(at.y,rect.y+6,rect.yMax-6);RectBox(new Rect(at.x-6,at.y-6,12,12),Color.yellow);}
            if(Call!=null){var at=MapPoint(rect,Call.Point,full);at.x=Mathf.Clamp(at.x,rect.x+6,rect.xMax-6);at.y=Mathf.Clamp(at.y,rect.y+6,rect.yMax-6);RectBox(new Rect(at.x-6,at.y-6,12,12),new Color(.35f,.62f,1));}
            DrawPlayerMapArrow(rect,full);
            if(full)DrawRegionLabels(rect);
        }
        void DrawPanel(float w,float h)
        {
            RectBox(new Rect(0,0,w,h),new Color(.025f,.055f,.04f,1));float left=(w-1080)/2;
            GUI.color=Roles[0].Color;Label(left,40,800,25,"SUNSET STATE  /  ОДИНОЧНАЯ ИСТОРИЯ",small);GUI.color=Color.white;
            if(Button(new Rect(left+945,38,135,40),"ЗАКРЫТЬ")){Panel="";Time.timeScale=1;SetCursor(true);return;}
            if(Panel=="roles")
            {
                Label(left,85,900,45,"КЕМ ТЫ СТАНЕШЬ СЕГОДНЯ?",heading);
                Label(left,135,1040,40,"Роль можно менять свободно. Ты получишь форму и появишься у своей организации; активное задание отменится.",small);
                roleScroll=GUI.BeginScrollView(new Rect(left,190,1080,h-225),roleScroll,new Rect(0,0,1050,Mathf.CeilToInt(Roles.Length/5f)*215));
                for(int i=0;i<Roles.Length;i++)
                {
                    var r=Roles[i];float x=(i%5)*211,y=(i/5)*215;
                    RectBox(new Rect(x,y,201,202),i==RoleIndex?new Color(.21f,.3f,.17f):new Color(.1f,.16f,.12f));
                    GUI.color=r.Color;Label(x+14,y+10,170,36,r.Icon+"  "+r.Name,body);GUI.color=Color.white;
                    Label(x+14,y+53,173,87,r.Description,small);Label(x+14,y+137,170,22,"ДО $"+r.Pay+" ЗА ЗАДАНИЕ",small);
                    int index=i;if(Button(new Rect(x+10,y+163,181,32),i==RoleIndex?"ТЕКУЩАЯ РОЛЬ":"ВЫБРАТЬ →"))SelectRole(index);
                }GUI.EndScrollView();
            }
            else if(Panel=="garage" || Panel=="personal-garage")DrawGarage(left,h);
            else if(Panel=="accessories")DrawAccessories(left,h);
            else if(Panel=="wardrobe")DrawWardrobe(left,h);
            else if(Panel=="shop")DrawShop(left,h);
            else if(Panel=="license-center")DrawLicenseCenter(left,h);
            else if(Panel=="purchases")DrawPurchases(left,h);
            else if(Panel=="hideout")DrawHideout(left,h);
            else if(Panel=="equipment")DrawEquipment(left,h);
            else if(Panel=="career")DrawCareer(left,h);
            else if(Panel=="licenses")DrawLicenses(left,h);
            else if(Panel=="difficulty")DrawDifficultyChoices(left,100,1040);
            else if(Panel=="shooting-range")DrawRangePanel(left,h);
            else if(Panel=="black-market")DrawBlackMarket(left,h);
            else if(Panel=="water-rescue")DrawWaterRescue(left,h);
            else if(Panel=="phone")DrawPhone(left,h);
            else if(Panel=="bank")DrawBank(left,h);
            else if(Panel=="travel")DrawTravel(left,h);
            else if(Panel=="conversation")DrawConversation(left,h);
            else if(Panel=="laws")DrawLawsPanel(left,h);
            else if(Panel=="court")DrawCourtPanel(left,h);
            else if(Panel=="operations")DrawOperationsPanel(left,h);
            else if(Panel=="interrogate")DrawInterrogatePanel(left,h);
            else if(Panel=="map")
            {
                DrawNavigationMap(left,h);
            }
            else if(Panel=="jobs")
            {
                if(DrawStoryJobs(left,h))return;
                Label(left,85,950,45,"РАБОТА / "+Roles[RoleIndex].Name.ToUpper(),heading);
                if(Button(new Rect(left+600,88,235,38),"СЮЖЕТ МОЕЙ РОЛИ"))storyTab=true;
                Label(left,135,1000,45,"Жёлтый маркер — этап задания. У законников вызовы 10-кодов приходят по рации автоматически: N — мегафон · C — допрос. У лицензеров: L — продажа лицензий.",small);
                jobsScroll=GUI.BeginScrollView(new Rect(left,190,1080,h-255),jobsScroll,new Rect(0,0,1060,Mathf.CeilToInt(JobsPerRole/2f)*140));
                for(int i=0;i<JobsPerRole;i++)
                {
                    var job=MakeMission(i);float x=(i%2)*530,y=(i/2)*140;
                    RectBox(new Rect(x,y,516,124),new Color(.1f,.16f,.12f));
                    Label(x+18,y+10,490,30,(i+1)+". "+job.Title,body);
                    Label(x+18,y+46,310,74,job.Description,small);
                    string tag=job.TimeLimit>0?" ТАЙМЕР":job.Fleeing?" ПОГОНЯ":job.Combat?" БОЙ":"";
                    Label(x+337,y+46,175,24,"$"+job.Reward+" "+tag,small);
                    int variant=i;if(Button(new Rect(x+337,y+72,160,40),"ПРИНЯТЬ"))AcceptMission(variant);
                }GUI.EndScrollView();
                if(ActiveMission!=null && Button(new Rect(left,h-60,320,40),"ОТМЕНИТЬ ТЕКУЩЕЕ ЗАДАНИЕ")){CancelMission();Notify("Задание отменено");}
            }
            else if(Panel=="story-finish")DrawStoryFinish(left,h);
            else
            {
                Label(left,100,800,55,"ПАУЗА",title);Label(left,195,900,35,"Выполнено заданий: "+Completed+"   /   Роль: "+Roles[RoleIndex].Name+"   /   Баланс: $"+Money);
                if(Button(new Rect(left,270,370,52),"ВЕРНУТЬСЯ В ГОРОД")){Panel="";Time.timeScale=1;SetCursor(true);}
                if(Button(new Rect(left,338,370,52),"СОХРАНИТЬ ПРОГРЕСС")){Save();Notify("Прогресс сохранён");}
                if(Button(new Rect(left,406,370,52),"ВЕРНУТЬСЯ К СВОЕЙ БАЗЕ")){Player.Teleport(RoleHome(RoleIndex));Panel="";Time.timeScale=1;SetCursor(true);}
                if(Button(new Rect(left,474,370,52),"СОХРАНИТЬ И ВЫЙТИ")){Save();Application.Quit();}
                if(Button(new Rect(left,542,370,42),"F11 · ПОЛНЫЙ ЭКРАН / ОКНО"))ToggleFullscreen();
                if(Button(new Rect(left+440,550,620,42),"СЛОЖНОСТЬ: "+Difficulty.Name.ToUpper()))Toggle("difficulty");
                Label(left+440,270,620,260,"УПРАВЛЕНИЕ\n\nWASD — ходьба / вождение · Мышь — камера\nShift — бег · Пробел — прыжок / ручной тормоз\nE — взаимодействие / вход / выход из машины\nTab — профессии · J — задания · M — карта\nG — служебный · H — личный транспорт · O — маячки\nВертолёт: Пробел — вверх, Ctrl / C — вниз\nЛКМ — выстрел · Колесо / Num+ / Num− — масштаб камеры",body);
                Label(left,h-100,1050,50,"Сохраняются роль, деньги, здоровье, время и позиция. Текущее задание и состояние машин сбрасываются при запуске.\nЕсть истории для 15 ролей, оружие и служебные инструменты. Входы в здания — E. Внутри фракции доступны раздевалка, снаряжение и сюжет.",small);
            }
        }
    }
}
