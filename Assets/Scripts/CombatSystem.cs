using System.Collections.Generic;
using UnityEngine;

namespace SunsetState
{
    public sealed class WeaponDefinition
    {
        public string Name;public int Capacity;public float Damage,Interval,Spread,Range,Reload;
        public WeaponDefinition(string name,int capacity,float damage,float interval,float spread,float range,float reload){Name=name;Capacity=capacity;Damage=damage;Interval=interval;Spread=spread;Range=range;Reload=reload;}
    }
    public sealed partial class GameRoot
    {
        public static readonly WeaponDefinition[] Weapons={
            new WeaponDefinition("Пистолет",15,24,.26f,.006f,100,1.4f),
            new WeaponDefinition("Desert Eagle",7,58,.48f,.009f,130,1.8f),
            new WeaponDefinition("Uzi",32,16,.075f,.027f,85,1.9f),
            new WeaponDefinition("MP5",30,21,.095f,.013f,130,1.8f),
            new WeaponDefinition("M4",30,32,.12f,.007f,220,2.1f),
            new WeaponDefinition("AK-47",30,38,.15f,.016f,200,2.3f),
            new WeaponDefinition("Дробовик",8,15,.8f,.07f,50,2.4f),
            new WeaponDefinition("Тайзер",1,0,1.6f,.003f,45,1.8f),
            new WeaponDefinition("Дубинка",0,12,.65f,0,3,0),
            new WeaponDefinition("Наручники",0,0,.6f,0,3.5f,0),
            new WeaponDefinition("Снайперская винтовка",10,90,.95f,.0018f,500,2.8f),
            new WeaponDefinition("РПК · пулемёт",75,36,.1f,.018f,260,4),
            new WeaponDefinition("G36",30,30,.085f,.008f,230,2.2f)};
        public int WeaponIndex;
        public float Armor=0;
        public float MaxArmor=>(RoleIndex==2?150:100)+AccessoryArmorBonus;
        int[] magazines=new int[Weapons.Length],reserves=new int[Weapons.Length];float nextShot,reloadingUntil;bool reloading;Vector2 weaponsScroll;
        public float FireInterval=>Weapons[WeaponIndex].Interval;
        public bool IsLaw=>RoleIndex==1 || RoleIndex==2 || RoleIndex==12;
        public bool LawEnforcement=>IsLaw || RoleIndex==3;
        public bool Armed=>IsLaw || RoleIndex==3 || RoleIndex==9 || RoleIndex==14 || RoleIndex==16 || RoleIndex==0 && OwnedGunMask!=0;
        public readonly List<HostileAgent> Enemies=new List<HostileAgent>();
        GameObject heldGun,vest;AudioSource combatAudio;AudioClip shotSound,taserSound;
        AudioClip[] recordedShots;
        int RemainingEnemies { get {int count=0;foreach(var enemy in Enemies)if(enemy && enemy.MissionEnemy && !enemy.Neutralized)count++;return count;} }
        void InitializeCombat()
        {
            combatAudio=gameObject.AddComponent<AudioSource>();combatAudio.volume=.18f;
            shotSound=MakeSound(false);taserSound=MakeSound(true);
            recordedShots=new AudioClip[7];string[] soundNames={"Pistol","Deagle","Uzi","MP5","M4","AK47","Shotgun"};for(int i=0;i<7;i++)recordedShots[i]=Resources.Load<AudioClip>("Audio/Weapons/"+soundNames[i]);
            heldGun=WorldBuilder.Box("Equipped weapon",Vector3.zero,new Vector3(.12f,.15f,.7f),new Color(.1f,.12f,.13f),null,false);
            heldGun.transform.SetParent(Player.transform,false);heldGun.transform.localPosition=new Vector3(.4f,1.15f,.5f);
            vest=WorldBuilder.Box("Body armour",Vector3.zero,new Vector3(.52f,.43f,.26f),new Color(.12f,.16f,.17f),null,false);
            vest.transform.SetParent(Player.transform,false);vest.transform.localPosition=Vector3.up*1.1f;
            EquipForRole();
        }
        static AudioClip MakeSound(bool electric)
        {
            int rate=22050;float[] samples=new float[electric?5000:2600];var noise=new System.Random(41);
            for(int i=0;i<samples.Length;i++){float t=i/(float)rate;float decay=Mathf.Exp(-t*(electric?17:38));samples[i]=(electric?Mathf.Sin(t*900*6.28318f)*(float)noise.NextDouble():((float)noise.NextDouble()*2-1)+Mathf.Sin(t*95*6.28318f)*.45f)*decay;}
            var clip=AudioClip.Create(electric?"Taser discharge":"Gunshot",samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        void EquipForRole()
        {
            ServiceSelected=!Armed;ToolIndex=0;
            Armor=RoleIndex==2?150:IsLaw || RoleIndex==3?100:RoleIndex==9 || RoleIndex==14 || RoleIndex==16?50:0;
            WeaponIndex=RoleIndex==0 && OwnedGunMask!=0?FirstOwnedWeapon():RoleIndex==2 || RoleIndex==3?4:IsLaw?3:RoleIndex==9 || RoleIndex==14 || RoleIndex==16?2:0;
            for(int i=0;i<Weapons.Length;i++){magazines[i]=Weapons[i].Capacity;reserves[i]=Weapons[i].Capacity*6;}
            reloading=false;nextShot=0;
        }
        void UpdateCombat()
        {
            if(!heldGun)return;
            heldGun.SetActive(Armed && !ServiceSelected && !Player.Vehicle && WeaponIndex!=9);vest.SetActive(Armor>0 && !Player.Vehicle);
            heldGun.transform.localScale=WeaponIndex==7?new Vector3(.18f,.14f,.3f):WeaponIndex==8?new Vector3(.09f,.09f,.85f):new Vector3(.12f,.15f,WeaponIndex<4?.4f:.7f);
            heldGun.GetComponent<Renderer>().sharedMaterial=WorldBuilder.Mat(WeaponIndex==7?new Color(.9f,.75f,.15f):new Color(.1f,.12f,.13f));
            if(Input.GetKeyDown(KeyCode.I)){Toggle("equipment");return;}
            if(!Armed)return;
            for(int i=0;i<10;i++)if(Input.GetKeyDown(i==9?KeyCode.Alpha0:(KeyCode)((int)KeyCode.Alpha1+i)))
            {if(!CanUseWeapon(i))continue;WeaponIndex=i;ServiceSelected=false;reloading=false;Notify("Выбрано: "+Weapons[i].Name);}
            if(Input.GetKeyDown(KeyCode.R))ReloadWeapon();
            if(Input.GetKeyDown(KeyCode.F) && IsLaw)TryUseCuffs();
            if(reloading && Time.time>=reloadingUntil){int needed=Weapons[WeaponIndex].Capacity-magazines[WeaponIndex],loaded=Mathf.Min(needed,reserves[WeaponIndex]);magazines[WeaponIndex]+=loaded;reserves[WeaponIndex]-=loaded;reloading=false;}
        }
        void ReloadWeapon()
        {
            if(reloading || Weapons[WeaponIndex].Capacity==0 || magazines[WeaponIndex]>=Weapons[WeaponIndex].Capacity)return;
            if(reserves[WeaponIndex]==0){Notify("Нет запасных патронов. I — снаряжение.");return;}
            reloading=true;reloadingUntil=Time.time+Weapons[WeaponIndex].Reload;Notify("Перезарядка: "+Weapons[WeaponIndex].Name);
        }
        HostileAgent autoAimTarget;
        float AutoAimRange=>Mathf.Min(Weapons[WeaponIndex].Range,WeaponIndex==10?400:150);
        static Vector3 AimPoint(HostileAgent enemy)=>enemy.transform.position+Vector3.up*(enemy.Downed?.35f:1.05f);
        bool ClearAimLine(Vector3 origin,Vector3 point,HostileAgent target)
        {
            Vector3 delta=point-origin;float distance=delta.magnitude;if(distance<.05f)return true;
            foreach(var hit in Physics.RaycastAll(origin,delta/distance,distance-.03f,~(1<<2),QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(target.transform))return false;
            return true;
        }
        HostileAgent FindAutoTarget(Camera camera,float maxDistance)
        {
            if(!camera){autoAimTarget=null;return null;}
            float bestScore=-1;HostileAgent best=null;Vector3 origin=camera.transform.position;
            foreach(var enemy in Enemies)
            {
                if(!enemy || !enemy.gameObject.activeInHierarchy || enemy.Neutralized || enemy.Health<=0 || enemy.IsOfficer && LawEnforcement)continue;
                Vector3 point=AimPoint(enemy),delta=point-origin;
                if(delta.sqrMagnitude<.1f || Vector3.Distance(Player.Position,point)>maxDistance)continue;
                Vector3 viewport=camera.WorldToViewportPoint(point);
                if(viewport.z<=0 || viewport.x<=0 || viewport.x>=1 || viewport.y<=0 || viewport.y>=1)continue;
                bool retained=enemy==autoAimTarget;float dot=Vector3.Dot(delta.normalized,camera.transform.forward);
                if(dot<Mathf.Cos((retained?22:18)*Mathf.Deg2Rad))continue;
                if(!ClearAimLine(origin,point,enemy) || !ClearAimLine(Player.transform.position+Vector3.up*1.3f,point,enemy))continue;
                float score=dot+(retained?.025f:0);if(score<=bestScore)continue;best=enemy;bestScore=score;
            }
            autoAimTarget=best;return best;
        }
        Vector2 AimScreenPoint(Camera camera,HostileAgent target,float width,float height)
        {
            if(!target)return new Vector2(width*.5f,height*.5f);
            Vector3 viewport=camera.WorldToViewportPoint(AimPoint(target));return new Vector2(viewport.x*width,(1-viewport.y)*height);
        }
        void TryFireWeapon()
        {
            if(!Armed || ServiceSelected){UseServiceTool();return;}
            if(Player.Vehicle || ShieldBlocking || reloading || Time.time<nextShot)return;
            if(!CanUseWeapon(WeaponIndex))return;
            var weapon=Weapons[WeaponIndex];nextShot=Time.time+weapon.Interval;
            if(WeaponIndex==9){TryUseCuffs();return;}
            if(WeaponIndex>=7 && WeaponIndex<=9 && !IsLaw)return;
            if(weapon.Capacity>0 && magazines[WeaponIndex]<=0){ReloadWeapon();return;}
            if(weapon.Capacity>0)magazines[WeaponIndex]--;
            Vector3 start=Player.transform.position+Vector3.up*1.3f;Camera camera=Camera.main;
            HostileAgent locked=FindAutoTarget(camera,AutoAimRange);
            Vector3 aimPoint=locked?AimPoint(locked):camera.transform.position+camera.transform.forward*weapon.Range;
            Vector3 flat=Vector3.ProjectOnPlane(aimPoint-start,Vector3.up);if(flat.sqrMagnitude>.1f)Player.transform.rotation=Quaternion.LookRotation(flat);
            if(WeaponIndex==8){UseBaton();return;}
            int soundIndex=WeaponIndex<7?WeaponIndex:WeaponIndex==10?1:WeaponIndex==11?5:4;
            combatAudio.pitch=WeaponIndex==2?1.12f:WeaponIndex==10?.7f:WeaponIndex==3?.95f:1;combatAudio.PlayOneShot(WeaponIndex==7?taserSound:recordedShots[soundIndex]?recordedShots[soundIndex]:shotSound);
            int pellets=WeaponIndex==6?7:1;
            for(int i=0;i<pellets;i++)
            {
                Vector3 direction,end;
                if(locked)
                {
                    direction=(aimPoint+Random.insideUnitSphere*weapon.Spread*AccuracyMultiplier*1.2f-start).normalized;
                    end=start+direction*weapon.Range;
                }
                else
                {
                    direction=(camera.transform.forward+camera.transform.right*Random.Range(-weapon.Spread,weapon.Spread)*AccuracyMultiplier+camera.transform.up*Random.Range(-weapon.Spread,weapon.Spread)*AccuracyMultiplier).normalized;
                    end=camera.transform.position+direction*weapon.Range;
                    if(Physics.Raycast(camera.transform.position,direction,out RaycastHit target,weapon.Range))end=target.point;
                }
                RaycastHit hit;bool didHit=WeaponIndex==7?Physics.SphereCast(start,.45f,(end-start).normalized,out hit,weapon.Range,~(1<<2),QueryTriggerInteraction.Ignore):Physics.Raycast(start,(end-start).normalized,out hit,weapon.Range);
                if(didHit)
                {
                    end=hit.point;var enemy=hit.collider.GetComponentInParent<HostileAgent>();
                    var actor=NpcEffects.Actor(hit.collider);
                    if(WeaponIndex==7){if(actor && NpcEffects.Stun(actor,12))Notify("Цель на земле на 12 секунд.");}
                    else if(hit.collider.GetComponentInParent<VehicleMotor>()){var car=hit.collider.GetComponentInParent<VehicleMotor>();car.GetComponent<VehicleDestruction>().BulletHit(weapon.Damage,hit.point);if(enemy)enemy.TakeDamage(weapon.Damage);}
                    else if(enemy)enemy.TakeDamage(weapon.Damage);
                    else if(hit.collider.GetComponentInParent<PatrolDrone>()){hit.collider.GetComponentInParent<PatrolDrone>().TakeDamage(weapon.Damage);}
                    else if(hit.collider.GetComponent<Pedestrian>())
                    {if(WeaponIndex!=7)hit.collider.gameObject.SetActive(false);if(!LawEnforcement){Wanted=Mathf.Min(5,Wanted+2);wantedTimer=0;Notify("Нападение на гражданского · объявлен розыск");}}
                    else{var structure=hit.collider.GetComponentInParent<WorldDestructible>();if(structure)structure.Damage(weapon.Damage*.3f);}
                }
                DrawShot(start,end,WeaponIndex==7?new Color(.3f,.7f,1):new Color(1,.8f,.35f));
            }
            if(!LawEnforcement && (RoleIndex==9 || RoleIndex==14 || RoleIndex==16)){Wanted=Mathf.Min(5,Wanted+1);wantedTimer=0;}
        }
        void UseBaton()
        {
            GameObject nearest=null;float range=3.2f;
            // Freshly created controllers can precede their first physics registration in scripted calls.
            foreach(var enemy in Enemies)if(enemy && NpcEffects.Alive(enemy.gameObject)){float distance=Vector3.Distance(Player.Position,enemy.transform.position);if(distance<range){range=distance;nearest=enemy.gameObject;}}
            foreach(var collider in Physics.OverlapSphere(Player.Position+Vector3.up*.8f,3.2f,~(1<<2),QueryTriggerInteraction.Ignore))
            {var actor=NpcEffects.Actor(collider);if(!NpcEffects.Alive(actor))continue;float distance=Vector3.Distance(Player.Position,actor.transform.position);if(distance<range){range=distance;nearest=actor;}}
            if(nearest && NpcEffects.Stun(nearest,8))Notify("Цель сбита дубинкой на 8 секунд.");else Notify("Подойди к человеку на расстояние удара.");
        }
        public bool TryUseCuffs()
        {
            if(!IsLaw || Player.Vehicle)return false;
            foreach(var enemy in Enemies)
            {
                if(!enemy || enemy.Neutralized || Vector3.Distance(Player.Position,enemy.transform.position)>3.5f)continue;
                if(!enemy.Stunned && enemy.Health>25){Notify("Противник сопротивляется. Сначала используй тайзер или дубинку.");return false;}
                if(transportSuspect){Notify("Сначала доставь текущего задержанного.");return false;}enemy.Arrest();Money+=150;AwardXP(35);interrogatable=enemy;transportSuspect=enemy.gameObject;if(RoleIndex==1||RoleIndex==2||RoleIndex==12)Notify("Задержание: наручники надеты. +$150 · C — допрос · доставь в тюрьму (Администрация) за +$600");else Notify("Задержание: наручники надеты. +$150");return true;
            }
            Notify("Рядом нет противника для задержания.");return false;
        }
        public void DamagePlayer(float amount,Vector3? source=null)
        {amount*=1-AccessoryProtection;if(source.HasValue)amount*=ShieldDamageScale(source.Value,ShieldBlocking);float absorbed=Mathf.Min(Armor,amount*.75f);Armor-=absorbed;Player.Health-=amount-absorbed;}
        public void DrawShot(Vector3 start,Vector3 end,Color color)
        {
            var beam=new GameObject("Bullet tracer");var line=beam.AddComponent<LineRenderer>();line.positionCount=2;line.SetPosition(0,start);line.SetPosition(1,end);line.startWidth=.035f;line.endWidth=.01f;line.material=Resources.Load<Material>("ShotBase");line.startColor=color;line.endColor=new Color(color.r,color.g,color.b,0);Destroy(beam,.08f);
        }
        void SpawnMissionEnemies()
        {
            ClearMissionEnemies();if(!Armed || !ActiveMission.Combat)return;
            var point=ActiveMission.Points[0];int count=RoleIndex==3||RoleIndex==2?8:6;
            for(int i=0;i<count;i++)
            {
                var enemy=HostileAgent.Spawn(this,point+new Vector3(-9+i*7,1,7),IsLaw||RoleIndex==3?"Вооружённый подозреваемый":"Вооружённый противник",false);enemy.MissionEnemy=true;
            }
            Notify("В районе "+count+" вооружённых противников. Нейтрализуй их; полиция может применять тайзер и наручники.");
        }
        void ClearMissionEnemies()
        {foreach(var enemy in Enemies)if(enemy && enemy.MissionEnemy && enemy.gameObject!=transportSuspect)Destroy(enemy.gameObject);Enemies.RemoveAll(e=>!e || e.MissionEnemy);}
        bool MissionThreatsRemain {get{foreach(var enemy in Enemies)if(enemy && enemy.MissionEnemy && !enemy.Neutralized)return true;return false;}}
        void DrawCombatHud(float w,float h)
        {
            if(ShieldEquipped && !Player.Vehicle)Label(w-330,h-205,310,30,ShieldBlocking?"ЩИТ: БЛОК СПЕРЕДИ":"ЩИТ · удерживай ПКМ для блока",small);
            RectBox(new Rect(255,h-129,200,5),new Color(.1f,.2f,.3f));RectBox(new Rect(255,h-129,200*Mathf.Clamp01(Armor/MaxArmor),5),new Color(.35f,.7f,1));Label(255,h-155,320,25,"БРОНЯ  "+Mathf.CeilToInt(Armor),small);
            if(!Armed || ServiceSelected){Label(w-390,h-170,365,35,Tools[RoleIndex][ToolIndex],heading);Label(w-390,h-138,365,30,"F / ЛКМ — применить · I — снаряжение · K — карьера",small);return;}
            Label(w-330,h-170,310,32,Weapons[WeaponIndex].Name,heading);Label(w-330,h-138,310,30,reloading?"ПЕРЕЗАРЯДКА…":Weapons[WeaponIndex].Capacity>0?magazines[WeaponIndex]+" / "+reserves[WeaponIndex]+"   ·   R — перезарядка":"F — наручники · I — снаряжение",small);
            if(ActiveMission!=null && MissionThreatsRemain)Label(w-308,282,290,50,"ПРОТИВНИКОВ: "+RemainingEnemies+(IsLaw?" · 8 — тайзер · F — арест":""),small);
        }
        void DrawEquipment(float left,float h)
        {
            if(Button(new Rect(left+730,85,340,44),"ИНВЕНТАРЬ / СКИНЫ →"))Toggle("wardrobe");
            Label(left,Armed?190:220,1080,25,"В КОМПЛЕКТЕ У ВСЕХ: личная аптечка · Q · +30 HP · бесплатно, без ограничений по количеству",small);
            if(!Armed){DrawServiceEquipment(left,h);return;}
            Label(left,90,700,45,"СНАРЯЖЕНИЕ / "+Roles[RoleIndex].Name.ToUpper(),heading);
            Label(left,145,1050,45,"1 — пистолет · 2 — Deagle · 3 — Uzi · 4 — MP5 · 5 — M4 · 6 — AK-47 · 7 — дробовик\nПолиция / ФБР / охрана: 8 — тайзер · 9 — дубинка · 0 или F — наручники"+(RoleIndex==1||RoleIndex==2?"\nN — мегафон (10-коды) · C — допрос задержанного":"")+(RoleIndex==11?"\nL — продажа лицензий прохожим":""),small);
            if(!IsLaw)Label(left,650,1080,25,"8 / 9 / 0 — полевое снаряжение: "+string.Join(" / ",Tools[RoleIndex]),small);
            if(RoleIndex==13 && !Escaped){Label(left,230,1000,90,"Заключённому недоступны бронежилеты и оружие. Сначала получи освобождение или выберись из тюрьмы.");return;}
            int cost=FreeRoleArmor?0:600;
            if(Button(new Rect(left,215,510,55),"БРОНЕЖИЛЕТ "+MaxArmor+" · "+(cost==0?"СЛУЖЕБНЫЙ":"$600"))){if(Money>=cost){Money-=cost;Armor=MaxArmor;Save();Notify("Бронежилет надет");}else Notify("Недостаточно денег");}
            if(RoleIndex==0 && Button(new Rect(left+540,215,540,55),"МОИ ПОКУПКИ / ПАТРОНЫ"))Toggle("purchases");
            if(Armed && RoleIndex!=0 && Button(new Rect(left+540,215,540,55),"ПОПОЛНИТЬ БОЕКОМПЛЕКТ · БЕСПЛАТНО")){for(int i=0;i<Weapons.Length;i++){magazines[i]=Weapons[i].Capacity;reserves[i]=Weapons[i].Capacity*6;}reloading=false;Notify("Боекомплект пополнен");}
            if(!Armed){Label(left,330,1040,60,"Оружие выдаётся полиции, ФБР, армии, охране тюрьмы, банде и свободному бандиту.");return;}
            var available=new List<int>();for(int index=0;index<Weapons.Length;index++)if(CanUseWeapon(index))available.Add(index);
            weaponsScroll=GUI.BeginScrollView(new Rect(left,300,1080,h-390),weaponsScroll,new Rect(0,0,1050,Mathf.CeilToInt(available.Count/5f)*155));
            for(int slot=0;slot<available.Count;slot++)
            {
                int i=available[slot];var weapon=Weapons[i];float x=slot%5*210,y=slot/5*155;
                RectBox(new Rect(x,y,204,143),new Color(.1f,.16f,.12f));Label(x+12,y+10,180,35,(i>9?"":i==9?"0 · ":(i+1)+" · ")+weapon.Name,body);
                Label(x+12,y+49,180,42,i>=7 && i<=9?"Задержание без убийства":"Урон "+weapon.Damage+" · магазин "+weapon.Capacity,small);
                int chosen=i;if(Button(new Rect(x+10,y+99,184,33),"ВЗЯТЬ")){WeaponIndex=chosen;ServiceSelected=false;reloading=false;Panel="";Time.timeScale=1;SetCursor(true);}
            }GUI.EndScrollView();
            Label(left,h-75,1080,35,"Броня поглощает 75% входящего урона, пока не закончится. Оглушённый противник не стреляет. Подойди на 3 м для наручников.",small);
        }
    }
    public sealed class HostileAgent:MonoBehaviour
    {
        public float Health=100;public bool Neutralized,MissionEnemy,VehicleUnit,Suspect,IsOfficer;public float StunnedUntil;public int Bounty;public float AttackDamage=7;AudioSource gunAudio;
        Transform bodyVisual;CapsuleCollider downedCollider;bool downed;
        public bool Downed=>downed;
        public bool Stunned=>Time.time<StunnedUntil;
        GameRoot game;CharacterController motor;float shotAt;Vector3 wander;
        public static HostileAgent Spawn(GameRoot game,Vector3 position,string name,bool vehicle,int appearanceRole=14)
        {
            var obj=new GameObject(name);obj.transform.position=position;var agent=obj.AddComponent<HostileAgent>();agent.game=game;agent.VehicleUnit=vehicle;
            agent.IsOfficer=appearanceRole==1||appearanceRole==2;agent.gunAudio=obj.AddComponent<AudioSource>();agent.gunAudio.playOnAwake=false;agent.gunAudio.spatialBlend=1;agent.gunAudio.minDistance=5;agent.gunAudio.maxDistance=95;agent.gunAudio.clip=Resources.Load<AudioClip>("Audio/Weapons/M4");agent.gunAudio.volume=.12f;
            agent.motor=obj.AddComponent<CharacterController>();agent.motor.height=1.8f;agent.motor.radius=.35f;agent.motor.center=Vector3.up*.9f;
            CharacterModels.Create(obj.transform,appearanceRole,new Color(.65f,.22f,.18f));
            var gun=WorldBuilder.Box("NPC rifle",Vector3.zero,new Vector3(.12f,.15f,.7f),new Color(.1f,.12f,.13f),null,false);gun.transform.SetParent(obj.transform,false);gun.transform.localPosition=new Vector3(.4f,1.25f,.5f);
            obj.AddComponent<NpcGroundMotor>().Initialize();agent.motor.Move(Vector3.zero);Physics.SyncTransforms();
            game.Enemies.Add(agent);agent.shotAt=Time.time+2*game.Difficulty.Reaction;return agent;
        }
        public static HostileAgent AttachToPatrol(GameRoot game,VehicleMotor car)
        {var agent=car.gameObject.AddComponent<HostileAgent>();agent.game=game;agent.VehicleUnit=true;agent.Health=180;agent.shotAt=Time.time+4*game.Difficulty.Reaction;game.Enemies.Add(agent);return agent;}
        void Update()
        {
            if(!game || !game.Playing || game.Panel!="" || Neutralized)return;
            if(VehicleUnit && game.Wanted==0)return;
            if(Stunned)return;
            if(downed)StandUp();
            Vector3 target=game.Player.Position,delta=target-transform.position;delta.y=0;float distance=delta.magnitude;
            if(motor)
            {
                Vector3 move=Vector3.zero;
                if(Suspect)
                {
                    // A fleeing suspect keeps its distance instead of closing in to fight.
                    if(distance<70)move=-delta.normalized;
                }
                else if(distance<85 && distance>(IsOfficer && (!game.Player.Vehicle || game.Player.Vehicle.Speed<4)?2.2f:15))
                {
                    move=delta.normalized;
                    if(Physics.Raycast(transform.position+Vector3.up,move,2.5f))move=Vector3.Cross(move,Vector3.up);
                }
                GetComponent<NpcGroundMotor>().Move(move*2.6f);
                if(distance<85 && delta.sqrMagnitude>1)transform.rotation=Quaternion.LookRotation(Suspect?-delta:delta);
            }
            if(Suspect)return;
            if(distance>70 || Time.time<shotAt || !HasLineOfSight(target+Vector3.up))return;
            shotAt=Time.time+Random.Range(1.1f,2.1f)*game.Difficulty.ShotInterval;
            var muzzle=transform.position+Vector3.up*1.4f+transform.forward*1.3f;
            game.DrawShot(muzzle,target+Vector3.up,new Color(1,.35f,.15f));
            if(gunAudio && gunAudio.clip)gunAudio.PlayOneShot(gunAudio.clip);
            if(Random.value<game.Difficulty.HitChance)game.ApplyEnemyHit(VehicleUnit?5:AttackDamage,transform.position);
        }
        bool HasLineOfSight(Vector3 target)
        {
            Vector3 origin=transform.position+Vector3.up*1.5f;var hits=Physics.RaycastAll(origin,(target-origin).normalized,Vector3.Distance(origin,target));
            foreach(var hit in hits){if(hit.transform.IsChildOf(transform) || hit.transform==transform || hit.transform.IsChildOf(game.Player.transform))continue;if(game.Player.Vehicle && hit.transform.IsChildOf(game.Player.Vehicle.transform))continue;return false;}return true;
        }
        public void Panic(float duration){StunnedUntil=Mathf.Max(StunnedUntil,Time.time+duration);}
        public void TakeDamage(float amount){if(Neutralized)return;Health-=amount;if(Health<=0){Neutralized=true;if(motor)motor.enabled=false;if(!VehicleUnit)FallDown();if(VehicleUnit){var car=GetComponent<VehicleMotor>();if(car)car.Traffic=false;}if(Bounty>0)game.Money+=Bounty;}}
        public void Stun(float duration){if(Neutralized)return;StunnedUntil=Time.time+duration;if(!VehicleUnit)FallDown();}
        void FallDown()
        {
            if(downed)return;downed=true;bodyVisual=transform.Find("Character");if(motor)motor.enabled=false;
            if(bodyVisual){CharacterModels.FreezePose(bodyVisual);bodyVisual.localRotation=Quaternion.Euler(90,0,0);bodyVisual.localPosition=new Vector3(0,.35f,-.9f);}
            if(!downedCollider){var hitbox=new GameObject("Downed body");hitbox.transform.SetParent(transform,false);downedCollider=hitbox.AddComponent<CapsuleCollider>();downedCollider.direction=2;downedCollider.height=1.8f;downedCollider.radius=.33f;downedCollider.center=Vector3.up*.35f;}downedCollider.enabled=true;
        }
        void StandUp()
        {
            downed=false;if(downedCollider)downedCollider.enabled=false;
            if(bodyVisual){bodyVisual.localPosition=Vector3.zero;bodyVisual.localRotation=Quaternion.identity;var motion=bodyVisual.GetComponent<CharacterMotion>();if(motion)motion.enabled=true;var animation=bodyVisual.GetComponentInChildren<Animation>();if(animation)animation.enabled=true;}
            if(motor)motor.enabled=true;
        }
        public void Arrest(){Neutralized=true;StunnedUntil=float.PositiveInfinity;gameObject.SetActive(false);}
        void OnControllerColliderHit(ControllerColliderHit hit){var car=hit.collider.GetComponentInParent<VehicleMotor>();if(car && car.Speed>25)TakeDamage(car.Speed*1.5f);}
    }
}
