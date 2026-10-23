using UnityEngine;
namespace SunsetState
{
    public static class StateBank
    {
        public static readonly Vector3 Location=new Vector3(430,1,-400);public static BuildingEntrance Entrance;
        public static void Build(Transform parent)
        {
            Vector3 p=SpawnSafety.VehiclePosition(Location,new Vector3(22,9,18),0);
            var shell=WorldBuilder.Box("State Bank",p+Vector3.up*4,new Vector3(22,8,18),new Color(.67f,.68f,.62f),parent);shell.AddComponent<WorldDestructible>().Initialize(1500);
            InteriorSystem.Register(shell,new Vector3(22,8,18),"Государственный банк");Entrance=InteriorSystem.Entrances[InteriorSystem.Entrances.Count-1];
            for(int i=-2;i<=2;i++)WorldBuilder.Box("Bank column",p+new Vector3(i*4,3,-10),new Vector3(.8f,6,.8f),new Color(.84f,.83f,.74f),parent);
            InteriorSystem.Label(p+new Vector3(0,7,-10.5f),"STATE BANK",parent,.13f);
        }
    }
    public sealed partial class GameRoot
    {
        public int BankBalance;float vaultHold;bool vaultLooted;Mission bankHeist;
        bool InBank=>InInterior&&insideBuilding==StateBank.Entrance;
        void DrawBank(float left,float h)
        {
            Label(left,90,1040,50,"ГОСУДАРСТВЕННЫЙ БАНК",heading);Label(left,160,1040,70,"Наличные: $"+Money+" · вклад: $"+BankBalance,body);
            if(Button(new Rect(left,265,500,65),"ВНЕСТИ $500")&&Money>=500){Money-=500;BankBalance+=500;Save();}
            if(Button(new Rect(left+530,265,500,65),"СНЯТЬ $500")&&BankBalance>=500){BankBalance-=500;Money+=500;Save();}
            if(RoleIndex==9||RoleIndex==14||RoleIndex==16){if(Button(new Rect(left,380,1030,70),"НАЧАТЬ ОГРАБЛЕНИЕ · охрана, розыск, хранилище"))StartBankHeist();}
            else Label(left,390,1030,100,"Банк охраняется. Полиция и ФБР получают вызовы на вооружённое ограбление через L.",body);
        }
        void StartBankHeist()
        {
            if(!InBank||!(RoleIndex==9||RoleIndex==14||RoleIndex==16))return;
            vaultHold=0;vaultLooted=false;bankHeist=new Mission("Ограбление государственного банка","Нейтрализуй охрану. У сейфа справа удерживай E 8 секунд, затем покинь банк и скройся в укрытии.",6000,false,new[]{interiorOrigin+new Vector3(7,1,5),StateBank.Entrance.Door,WorldBuilder.Places[RoleIndex].Position},"Хранилище: удерживай E 8 секунд","Покинь банк","Доставь добычу в укрытие");ActivateMission(bankHeist);Wanted=5;wantedTimer=0;
            for(int i=0;i<3;i++){var enemy=HostileAgent.Spawn(this,interiorOrigin+new Vector3(-4+i*4,0,-1),"Полиция · защита банка",false,1);enemy.MissionEnemy=true;}
            Notify("Сигнал тревоги! Полиция защищает банк. Сейф: E после нейтрализации охраны.");
        }
        bool BankStageBlocked
        {
            get{if(ActiveMission==null||ActiveMission!=bankHeist||Step!=0||vaultLooted)return false;
                if(InBank&&Vector3.Distance(Player.Position,ActiveMission.Points[0])<3&&Input.GetKey(KeyCode.E)&&!MissionThreatsRemain)vaultHold+=Time.deltaTime;else vaultHold=0;
                if(vaultHold>=8){vaultLooted=true;Notify("Добыча взята. Покинь банк и доберись до укрытия.");return false;}return true;}
        }
    }
}
