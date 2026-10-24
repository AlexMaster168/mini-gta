using System;
using System.Collections.Generic;
using UnityEngine;
namespace SunsetState
{
    public static class ShootingRanges
    {
        public static readonly Vector3[] Centers=new Vector3[2];
        public static readonly Vector3[] Terminals=new Vector3[2];
        public static readonly List<RangeTarget> Targets=new List<RangeTarget>();
        public static void Build(Transform root)
        {
            Targets.Clear();
            for(int range=0;range<2;range++)
            {
                Vector3 requested=range==0?new Vector3(250,1,-1080):FactionDistricts.ArmyGate+new Vector3(75,0,80);
                Vector3 center=SpawnSafety.VehiclePosition(requested,new Vector3(28,5,48),0);Centers[range]=center;Terminals[range]=center+new Vector3(-10,0,-16);
                var yard=new GameObject(range==0?"Public shooting range":"Army shooting range").transform;yard.SetParent(root,false);
                WorldBuilder.Box("Range floor",center+Vector3.down*.1f,new Vector3(28,.15f,48),new Color(.28f,.3f,.28f),yard);
                WorldBuilder.Box("Ballistic backstop",center+new Vector3(0,2.5f,22),new Vector3(28,5,1),new Color(.25f,.27f,.25f),yard);
                foreach(int side in new[]{-1,1})WorldBuilder.Box("Range side safety wall",center+new Vector3(side*14,2,0),new Vector3(.4f,4,45),new Color(.3f,.32f,.3f),yard);
                WorldBuilder.Box("Firing line",center+new Vector3(0,.04f,-10),new Vector3(25,.06f,.25f),Color.yellow,yard);
                WorldBuilder.Box("Range instructor desk",Terminals[range]+Vector3.up*.65f,new Vector3(2,1.3f,1),new Color(.25f,.3f,.22f),yard);
                InteriorSystem.Label(Terminals[range]+Vector3.up*2.2f,range==0?"ТИР ДЛЯ ВСЕХ · E":"АРМЕЙСКИЙ ТИР · E",yard,.08f);
                for(int lane=0;lane<5;lane++)
                {
                    Vector3 position=center+new Vector3((lane-2)*4,1.5f,8+lane%3*5);
                    WorldBuilder.Box("Target stand",position+Vector3.down*.8f,new Vector3(.12f,1.6f,.12f),Color.gray,yard);
                    var board=WorldBuilder.Box("Training target",position,new Vector3(1.2f,1.7f,.16f),new Color(.8f,.15f,.12f),yard);var target=board.AddComponent<RangeTarget>();target.Range=range;Targets.Add(target);
                    var bullseye=WorldBuilder.Box("Target center",position+Vector3.back*.095f,new Vector3(.42f,.55f,.04f),Color.white,yard,false);
                }
            }
        }
    }
    public sealed class RangeTarget:MonoBehaviour
    {
        public int Range;float readyAt;
        public bool Hit()
        {
            var game=GameRoot.Instance;if(!game||!game.TrainingActive||game.TrainingRange!=Range||Time.time<readyAt)return false;
            readyAt=Time.time+.4f;game.RegisterTrainingHit();GetComponent<Renderer>().sharedMaterial=WorldBuilder.Mat(new Color(.25f,.65f,.3f));return true;
        }
        void Update(){if(Time.time>=readyAt)GetComponent<Renderer>().sharedMaterial=WorldBuilder.Mat(new Color(.8f,.15f,.12f));}
        public void ResetTarget(){readyAt=0;}
    }
    public sealed partial class GameRoot
    {
        public int ShootingXP;
        public int ShootingLevel=>Mathf.Clamp(ShootingXP/100,0,10);
        public float ShootingSkillAccuracy=>1-ShootingLevel*.015f;
        public bool TrainingActive{get;private set;}
        public int TrainingRange{get;private set;}
        int trainingWeapon,trainingHits,trainingShots,previousWeapon;bool previousService;float trainingEnds;
        int[] previousMagazines,previousReserves;
        int[] SavedMagazines=>TrainingActive?previousMagazines:magazines;
        int[] SavedReserves=>TrainingActive?previousReserves:reserves;
        bool TrainingWeaponAllowed(int index)=>TrainingActive&&index==trainingWeapon;
        int NearbyRange()
        {if(Player.Vehicle||InInterior)return -1;for(int i=0;i<2;i++)if(Vector3.Distance(Player.Position,ShootingRanges.Terminals[i])<4)return i;return -1;}
        bool TryRangeAction()
        {if(TrainingActive){StopTraining(true);return true;}if(NearbyRange()<0)return false;Toggle("shooting-range");return true;}
        bool StartTraining(int range,int weapon)
        {
            if(TrainingActive||range<0||range>1||NearbyRange()!=range||Wanted>0||range==1&&RoleIndex!=3)return false;
            bool allowed=range==0?(weapon==0||weapon==3||weapon==5):(weapon==4||weapon==10||weapon==11);if(!allowed)return false;
            previousWeapon=WeaponIndex;previousService=ServiceSelected;previousMagazines=(int[])magazines.Clone();previousReserves=(int[])reserves.Clone();
            TrainingRange=range;trainingWeapon=weapon;TrainingActive=true;trainingHits=0;trainingShots=0;trainingEnds=Time.time+60;WeaponIndex=weapon;ServiceSelected=false;reloading=false;nextShot=0;
            magazines[weapon]=Weapons[weapon].Capacity;reserves[weapon]=Weapons[weapon].Capacity*6;
            foreach(var target in ShootingRanges.Targets)if(target&&target.Range==range)target.ResetTarget();
            Player.Teleport(ShootingRanges.Centers[range]+new Vector3(0,.5f,-15));Panel="";Time.timeScale=1;SetCursor(true);Notify("Тренировка: 30 выстрелов / 60 секунд. Стреляй по красным мишеням. E — закончить.");return true;
        }
        void StopTraining(bool award)
        {
            if(!TrainingActive)return;int hits=trainingHits;TrainingActive=false;magazines=previousMagazines;reserves=previousReserves;previousMagazines=null;previousReserves=null;WeaponIndex=previousWeapon;ServiceSelected=previousService;reloading=false;nextShot=0;
            if(award){int xp=hits*3+(hits>=15?15:0);ShootingXP=Mathf.Clamp(ShootingXP+xp,0,1000);Notify("Тир: "+hits+" / "+trainingShots+" попаданий · +"+xp+" опыта · навык "+ShootingLevel+" / 10");Save();}
        }
        void UpdateTraining()
        {
            if(!TrainingActive)return;Vector3 local=Player.Position-ShootingRanges.Centers[TrainingRange];
            if(Player.Vehicle||InInterior||Mathf.Abs(local.x)>14||local.z< -24||local.z>22||Time.time>=trainingEnds||trainingShots>=30)StopTraining(true);
        }
        bool AtFiringLine=>!TrainingActive||(Player.Position-ShootingRanges.Centers[TrainingRange]).z<=-10;
        public void RegisterTrainingHit(){if(TrainingActive&&AtFiringLine)trainingHits++;}
        void DrawRangePanel(float left,float h)
        {
            int range=NearbyRange();Label(left,100,1040,45,range==1?"АРМЕЙСКИЙ ТИР":"ОБЩЕСТВЕННЫЙ ТИР",heading);
            Label(left,170,1040,90,"30 выстрелов за 60 секунд. Попадание: +3 опыта; от 15 попаданий: +15 бонусом.\nНавык "+ShootingLevel+" / 10 · опыт "+ShootingXP+" / 1000 · разброс −"+(ShootingLevel*1.5f).ToString("0.#")+"%. Учебное оружие возвращается после занятия.",body);
            if(range<0){Label(left,300,1040,50,"Подойди к стойке тира пешком.",body);return;}
            if(range==1&&RoleIndex!=3){Label(left,300,1040,70,"Армейский тир доступен военнослужащим. Общий тир открыт для всех ролей.",body);return;}
            if(Wanted>0){Label(left,300,1040,70,"Сначала сними розыск. Во время погони занятие недоступно.",body);return;}
            int[] choices=range==0?new[]{0,3,5}:new[]{4,11,10};for(int i=0;i<choices.Length;i++){int weapon=choices[i];if(Button(new Rect(left,300+i*85,1040,65),"ТРЕНИРОВКА · "+Weapons[weapon].Name.ToUpper()+" · БЕСПЛАТНО"))StartTraining(range,weapon);}
        }
        void DrawTrainingHud()
        {if(TrainingActive)Label(420,110,500,80,"ТИР: "+trainingHits+" попаданий / "+trainingShots+" выстрелов\nОсталось "+Mathf.CeilToInt(trainingEnds-Time.time)+" с · E — закончить\nОгневой рубеж — за жёлтой линией",small);}
        void CheckShootingRanges()
        {
            SelectRole(4);Wanted=0;Player.Teleport(ShootingRanges.Terminals[0]+Vector3.back*2);int[] original=(int[])magazines.Clone();int skill=ShootingXP;
            Require(StartTraining(0,0)&&Armed&&CanUseWeapon(0)&&!CanUseWeapon(4),"Any peaceful role must borrow only selected training weapon");
            var target=ShootingRanges.Targets.Find(t=>t.Range==0);Physics.SyncTransforms();Camera.main.transform.position=Player.Position+Vector3.up*1.4f-Vector3.forward*4;Camera.main.transform.LookAt(target.transform.position);nextShot=0;Fire();Require(trainingShots==1&&trainingHits==1,"Practice bullet must hit actual target and update score");
            Save();var saved=JsonUtility.FromJson<SaveData>(System.IO.File.ReadAllText(SavePath));Require(saved.LoadedAmmo[0]==original[0],"Training ammo must never leak into saved inventory");
            Require(!target.Hit(),"Same target must reject immediate repeated scoring");StopTraining(true);Require(!TrainingActive&&!Armed&&ShootingXP==skill+3&&magazines[0]==original[0],"Leaving range must award XP and return loan weapon and ammo");
            Save();ShootingXP=0;Load();Require(ShootingXP==skill+3,"Shooting skill must persist");
            Player.Teleport(ShootingRanges.Terminals[1]);Require(!StartTraining(1,4),"Army range must reject nonmilitary role");SelectRole(3);Wanted=0;Player.Teleport(ShootingRanges.Terminals[1]);Require(StartTraining(1,11)&&CanUseWeapon(11),"Army must receive machine gun training");
            Player.Teleport(ShootingRanges.Centers[1]+Vector3.right*40);UpdateTraining();Require(!TrainingActive,"Leaving practice area must return loan weapon immediately");
            SelectRole(0);Wanted=2;Player.Teleport(ShootingRanges.Terminals[0]);Require(!StartTraining(0,0),"Wanted players must not use range as sanctuary");Wanted=0;
            ShootingXP=1000;Require(Mathf.Approximately(ShootingSkillAccuracy,.85f),"Maximum shooting skill must reduce spread by 15 percent");ShootingXP=0;SelectRole(0);Debug.Log("SUNSET_PUBLIC_ARMY_RANGES_REAL_TARGET_SCORE_SKILL_SAVE_LOAN_RETURN_ACCESS_OK");
        }
    }
}
