using UnityEngine;

namespace SunsetState
{
    public sealed partial class GameRoot
    {
        public int ToolIndex;public bool ServiceSelected;
        float personalMedkitReadyAt;
        void UsePersonalMedkit()
        {
            if(Time.time<personalMedkitReadyAt)return;
            if(Player.Health>=100){Notify("Здоровье уже восстановлено.");return;}
            personalMedkitReadyAt=Time.time+.8f;
            HealSelf();Save();
        }
        bool serviceDone,patientCarried;float toolCooldown;GameObject serviceSubject,stretcher,barrier;float serviceHealth,fireStrength;int serviceStage;
        static readonly string[][] Tools={
            new[]{"Телефон","Папка документов","Аптечка"},new[]{"Тайзер","Дубинка","Наручники"},new[]{"Тайзер","Дубинка","Наручники"},
            new[]{"Армейская аптечка","Полевой ремонт","Связь"},new[]{"Медицинская аптечка","Носилки","Стабилизация"},new[]{"Огнетушитель","Пожарный топор","Спасательная аптечка"},
            new[]{"Диспетчерская рация","Навигатор","Аптечка"},new[]{"Накладные","Ремкомплект","Аптечка"},new[]{"Ремкомплект","Эвакуатор","Аптечка"},
            new[]{"Аптечка","Ремкомплект","Телефон"},new[]{"Служебные документы","Рация","Аптечка"},new[]{"Экзаменационный планшет","Дорожный ремкомплект / конусы","Аптечка"},
            new[]{"Тайзер","Дубинка","Наручники"},new[]{"Тюремное поручение","Обращение в администрацию","Распорядок"},new[]{"Аптечка","Ремкомплект","Телефон"},new[]{"Материалы дела","Переговоры","Аптечка"},new[]{"Стяжки / освободить","Переговоры","Аптечка"},new[]{"Законодательный планшет","Охрана","Аптечка"}};
        void UpdateServiceTools()
        {
            if(Input.GetKeyDown(KeyCode.Q))UsePersonalMedkit();
            if(IsLaw)return;
            for(int i=0;i<3;i++)if(Input.GetKeyDown(i==2?KeyCode.Alpha0:(KeyCode)((int)KeyCode.Alpha8+i))){ToolIndex=i;ServiceSelected=true;Notify("Снаряжение: "+Tools[RoleIndex][i]+" · ЛКМ или F — использовать");}
            if(Input.GetKeyDown(KeyCode.F))UseServiceTool();
            if(patientCarried && serviceSubject)
            {
                serviceSubject.SetActive(!Player.Vehicle);
                if(!Player.Vehicle){serviceSubject.transform.position=Player.Position+Player.transform.forward*1.5f+Vector3.up*.6f;serviceSubject.transform.rotation=Quaternion.Euler(90,Player.transform.eulerAngles.y,0);}
            }
            if(RoleIndex==5 && fireStrength>0 && serviceSubject)
            {
                serviceSubject.transform.localScale=new Vector3(3+Mathf.Sin(Time.time*7)*.25f,3+fireStrength*.025f,3);
                if(Vector3.Distance(Player.Position,serviceSubject.transform.position)<3)DamagePlayer(Time.deltaTime*4);
            }
        }
        void PrepareServiceObjective()
        {
            ClearServiceObjective();serviceDone=true;
            if(RoleIndex!=4 && RoleIndex!=5 && RoleIndex!=8)return;
            serviceDone=false;serviceStage=RoleIndex==8?1:0;serviceHealth=25;fireStrength=100;
            Vector3 location=ActiveMission.Points[Mathf.Min(serviceStage,ActiveMission.Points.Length-1)];
            if(RoleIndex==4)
            {
                serviceSubject=new GameObject("Patient");serviceSubject.transform.position=location+new Vector3(4,.5f,0);serviceSubject.transform.rotation=Quaternion.Euler(90,0,0);
                var patient=CharacterModels.Create(serviceSubject.transform,0,new Color(.7f,.45f,.3f));patient.localPosition=Vector3.down*.9f;CharacterModels.FreezePose(patient);
                stretcher=WorldBuilder.Box("Medical stretcher",Vector3.zero,new Vector3(1.2f,.13f,2.5f),new Color(.35f,.65f,.8f),null,false);stretcher.transform.SetParent(serviceSubject.transform,false);stretcher.transform.localPosition=new Vector3(0,0,.5f);stretcher.transform.localRotation=Quaternion.Euler(90,0,0);stretcher.SetActive(false);
            }
            else if(RoleIndex==5)
            {
                serviceSubject=GameObject.CreatePrimitive(PrimitiveType.Sphere);serviceSubject.name="Fire";Destroy(serviceSubject.GetComponent<Collider>());serviceSubject.transform.position=location+new Vector3(4,1.5f,0);serviceSubject.transform.localScale=new Vector3(3,5,3);serviceSubject.GetComponent<Renderer>().sharedMaterial=WorldBuilder.Mat(new Color(1,.33f,.05f));
                var light=serviceSubject.AddComponent<Light>();light.color=new Color(1,.3f,.05f);light.intensity=3;light.range=18;
                barrier=WorldBuilder.Box("Rescue barricade",location+new Vector3(-5,1,0),new Vector3(2,2,.3f),new Color(.5f,.32f,.16f));
            }
            else
            {
                var broken=WorldBuilder.SpawnCar(this,location+new Vector3(5,1,0),"Неисправный автомобиль",new Color(.4f,.43f,.45f),false,0);broken.Durability=10;serviceSubject=broken.gameObject;
            }
        }
        void ClearServiceObjective(){if(serviceSubject){var car=serviceSubject.GetComponent<VehicleMotor>();if(car)Cars.Remove(car);Destroy(serviceSubject);}if(barrier)Destroy(barrier);serviceSubject=null;barrier=null;stretcher=null;patientCarried=false;serviceDone=true;}
        bool ServiceStageBlocked=>!serviceDone && ActiveMission!=null && Step==serviceStage;
        void UseServiceTool()
        {
            if(Time.time<toolCooldown)return;toolCooldown=Time.time+.8f;
            if(RoleIndex==15){Toggle("phone");return;}if(RoleIndex==17){Toggle("laws");return;}
            if(RoleIndex==13){Notify("J — тюремные поручения, освобождение или побег.");return;}
            if(RoleIndex==4)
            {
                if(!serviceSubject || Vector3.Distance(Player.Position,serviceSubject.transform.position)>6){HealSelf();return;}
                if(ToolIndex==1){if(serviceHealth<90){Notify("Сначала окажи помощь аптечкой (8) или стабилизируй пациента (0).");return;}patientCarried=!patientCarried;stretcher.SetActive(patientCarried);serviceDone=patientCarried;Notify(patientCarried?"Пациент на носилках. Вернись в машину и доставь его в больницу.":"Носилки опущены.");}
                else{serviceHealth=Mathf.Min(100,serviceHealth+(ToolIndex==2?75:40));DrawShot(Player.Position+Vector3.up,serviceSubject.transform.position,Color.green);Notify("Пациент: "+serviceHealth+"% · 9 — носилки");}
                return;
            }
            if(RoleIndex==5 && ToolIndex==0)
            {
                if(!serviceSubject || Vector3.Distance(Player.Position,serviceSubject.transform.position)>14){Notify("Подойди к очагу пожара на 12 метров.");return;}
                DrawShot(Player.Position+Vector3.up,serviceSubject.transform.position,Color.cyan);fireStrength=Mathf.Max(0,fireStrength-35);Notify("Очаг пожара: "+fireStrength+"%");
                if(fireStrength<=0){serviceSubject.SetActive(false);serviceDone=true;AwardXP(20);Notify("Пожар потушен. Можно завершить этап задания.");}return;
            }
            if(RoleIndex==5 && ToolIndex==1)
            {if(barrier && Vector3.Distance(Player.Position,barrier.transform.position)<5){Destroy(barrier);barrier=null;Notify("Завал разобран пожарным топором.");}else Notify("Подойди к деревянному завалу у места вызова.");return;}
            if((RoleIndex==8 && ToolIndex==0)||(RoleIndex==3 && ToolIndex==1)||(RoleIndex==7 && ToolIndex==1)||(RoleIndex==9 && ToolIndex==1)||(RoleIndex==14 && ToolIndex==1))
            {
                var car=Player.Vehicle?Player.Vehicle:NearestVehicle(Player.Position,7);if(!car){Notify("Рядом нет автомобиля для ремонта.");return;}car.Durability=100;if(RoleIndex==8 && serviceSubject==car.gameObject){serviceDone=true;AwardXP(20);}Notify("Автомобиль отремонтирован. Прочность 100%.");return;
            }
            if(RoleIndex==8 && ToolIndex==1){ToggleTow();return;}
            if(ToolIndex==2 || RoleIndex==3 || RoleIndex==9 || RoleIndex==14){HealSelf();return;}
            if(RoleIndex==11 && ToolIndex==1){if(RepairTraffic())return;var cone=WorldBuilder.Box("Training cone",Player.Position+Player.transform.forward*2+Vector3.up*.3f,new Vector3(.5f,.8f,.5f),new Color(1,.4f,.1f));Destroy(cone,90);Notify("Учебный конус установлен на 90 секунд.");return;}
            if(ToolIndex==1 && (RoleIndex==6 || RoleIndex==10)){Toggle("map");return;}
            Notify(ActiveMission!=null?"Служебная запись: "+ActiveMission.Steps[Step]:"Нет активного поручения. J — сюжет и работа.");
        }
        void HealSelf(){if(Player.Health>=100){Notify("Здоровье уже восстановлено.");return;}Player.Health=Mathf.Min(100,Player.Health+30+HealingBonus);Notify("Аптечка: +"+(30+HealingBonus)+" здоровья");}
        FixedJoint towJoint;
        void ToggleTow()
        {
            if(towJoint){Destroy(towJoint);towJoint=null;Notify("Автомобиль отсоединён");return;}
            if(!Player.Vehicle){Notify("Сядь в служебную машину и подъедь к автомобилю для буксировки.");return;}
            var target=NearestVehicle(Player.Position,12);if(!target || target.Flying || target.OnWater){Notify("Рядом нет подходящего автомобиля.");return;}
            target.Body.position=Player.Vehicle.transform.position-Player.Vehicle.transform.forward*8;target.Body.rotation=Player.Vehicle.Body.rotation;
            target.Traffic=false;towJoint=target.gameObject.AddComponent<FixedJoint>();towJoint.connectedBody=Player.Vehicle.Body;towJoint.breakForce=150000;Notify("Автомобиль взят на буксир. F — отсоединить.");
        }
        void DrawServiceEquipment(float left,float h)
        {
            Label(left,90,700,45,"СНАРЯЖЕНИЕ / "+Roles[RoleIndex].Name.ToUpper(),heading);
            Label(left,150,1050,60,"8 / 9 / 0 — выбор инструмента · F или ЛКМ — использовать\nДля помощи пациенту, тушения или ремонта сначала возьми задание через J.",body);
            for(int i=0;i<3;i++){float x=left+i*360;RectBox(new Rect(x,250,345,160),new Color(.1f,.16f,.12f));Label(x+15,270,315,65,Tools[RoleIndex][i],heading);int chosen=i;if(Button(new Rect(x+15,355,315,40),(i==2?"0":(i+8).ToString())+" · ВЗЯТЬ")){ToolIndex=chosen;ServiceSelected=true;Panel="";Time.timeScale=1;SetCursor(true);}}
            if(Button(new Rect(left,450,530,55),"СВОЯ КАРЬЕРА И ПОВЫШЕНИЯ →"))Panel="career";
            if(Button(new Rect(left+550,450,530,55),"СЮЖЕТ И ПОРУЧЕНИЯ →")){storyTab=true;Panel="jobs";}
            if(FreeRoleArmor && Button(new Rect(left,515,1050,38),"БРОНЕЖИЛЕТ · БЕСПЛАТНО"))EquipFreeArmor();
            Label(left,570,1050,80,"Звание: "+RankTitle+" · "+CareerXP[RoleIndex]+" XP\nЛичный транспорт — H · служебный — G. У каждой структуры есть свои инструменты и отдельная сюжетная линия.",heading);
        }
    }
}
