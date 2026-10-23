using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        public int Contraband;
        void TakeHideoutSupplies(int item)
        {
            if(!InInterior || insideBuilding.Role!=RoleIndex || (RoleIndex!=14 && RoleIndex!=9))return;
            if(item==0){for(int i=0;i<Weapons.Length;i++){magazines[i]=Weapons[i].Capacity;reserves[i]=Weapons[i].Capacity*6;}}
            if(item==1)Armor=MaxArmor;
            if(item==2)Player.Health=100;
            if(item==3)Contraband=Mathf.Min(20,Contraband+5);
            Save();Notify("Припасы получены бесплатно.");
        }
        void DrawHideout(float left,float h)
        {
            Label(left,85,1050,50,"ОБЩАГ БАНДИТА / СХРОН",heading);
            Label(left,150,1000,70,"Бесплатные припасы для своей группировки.\nКонтрабанда в инвентаре: "+Contraband+" / 20 (игровые пакеты)",body);
            string[] actions={"ПОПОЛНИТЬ ПАТРОНЫ","ВЗЯТЬ БРОНЕЖИЛЕТ","АПТЕЧКА · ВОССТАНОВИТЬ ЗДОРОВЬЕ","ВЗЯТЬ 5 ПАКЕТОВ КОНТРАБАНДЫ"};
            for(int i=0;i<actions.Length;i++)if(Button(new Rect(left,250+i*65,650,50),actions[i]))TakeHideoutSupplies(i);
            if(Button(new Rect(left+700,250,340,60),"ВЫБРАТЬ ОРУЖИЕ →"))Toggle("equipment");
        }
    }
}
