using UnityEngine;
namespace SunsetState
{
    public static class ConvenienceStores
    {
        public static readonly Vector3[] Locations={new Vector3(950,1,60),StateBuilder.SanFierro+new Vector3(650,0,0),StateBuilder.LasVenturas+new Vector3(650,0,0)};
        public static void Build(Transform parent)
        {
            for(int i=0;i<Locations.Length;i++)
            {
                Vector3 p=SpawnSafety.VehiclePosition(Locations[i],new Vector3(18,6,15),0);Locations[i]=p;
                var shop=WorldBuilder.Box("Магазин 24/7",p+Vector3.up*2.6f,new Vector3(16,5.2f,12),new Color(.62f,.6f,.47f),parent);shop.AddComponent<WorldDestructible>().Initialize(600);
                WorldBuilder.Box("24/7 roof",p+Vector3.up*5.35f,new Vector3(17,.3f,13),new Color(.12f,.3f,.22f),parent);
                WorldBuilder.Box("Store window",p+new Vector3(-4,2,-6.05f),new Vector3(5,2,.1f),new Color(.2f,.45f,.48f),parent,false);
                InteriorSystem.Label(p+new Vector3(0,4,-6.2f),"24 / 7 · ПРОДУКТЫ / ОРУЖИЕ",parent,.055f);
                InteriorSystem.Register(shop,new Vector3(16,5.2f,12),"Магазин 24/7");InteriorSystem.Entrances[InteriorSystem.Entrances.Count-1].ShopId=i;
                WorldBuilder.Box("Store parking",p+new Vector3(0,-.1f,-17),new Vector3(24,.12f,18),new Color(.22f,.24f,.24f),parent);
            }
        }
    }
    public sealed partial class GameRoot
    {
        public int OwnedGunMask;public int[] Purchases=new int[6];float drunkUntil;Vector2 shopScroll;
        public bool Drunk=>Time.time<drunkUntil;
        static readonly string[] Goods={"Еда","Вода","Пиво","Большая аптечка","Ремкомплект","Бронежилет","Пистолет","Desert Eagle","Uzi","Дробовик","Патроны"};
        static readonly int[] Prices={55,30,80,150,250,600,800,1800,1600,1400,120};
        static readonly string[] GoodInfo={"Восстановить 20 HP","5 HP и уменьшение опьянения","Временное лёгкое покачивание камеры","Восстановить 60 HP","Починить ближайший транспорт","Пополнить броню до максимума","Личное гражданское оружие","Личное гражданское оружие","Личное гражданское оружие","Личное гражданское оружие","Три магазина к купленному оружию"};
        bool InStore=>InInterior && insideBuilding.ShopId>=0;
        bool CanUseWeapon(int index)
        {
            if(index<0||index>=Weapons.Length)return false;
            if(RoleIndex==0)return (OwnedGunMask&(1<<index))!=0;
            return (IsLaw||RoleIndex==3||RoleIndex==9||RoleIndex==14||RoleIndex==16) && (index<7||index>9||IsLaw);
        }
        int FirstOwnedWeapon(){for(int i=0;i<Weapons.Length;i++)if((OwnedGunMask&(1<<i))!=0)return i;return 0;}
        bool BuyGood(int index)
        {
            if(!InStore || index<0||index>=Goods.Length)return false;
            if(index>=6 && RoleIndex!=0 && RoleIndex!=9 && RoleIndex!=14 && RoleIndex!=16){Notify("Личное оружие продаётся гражданскому и криминальным ролям. Служебное оснащение — I.");return false;}
            if(Money<Prices[index]){Notify("Недостаточно денег.");return false;}
            if(index==10 && OwnedGunMask==0){Notify("Сначала купи личное оружие.");return false;}
            if(index<6 && Purchases[index]>=99){Notify("Этот предмет уже в максимальном количестве.");return false;}
            if(index>=6 && index<=9)
            {
                int weapon=index==6?0:index==7?1:index==8?2:6;
                if((OwnedGunMask&(1<<weapon))!=0){Notify("Уже куплено. Можно докупить патроны.");return false;}
                OwnedGunMask|=1<<weapon;WeaponIndex=weapon;ServiceSelected=false;magazines[weapon]=Weapons[weapon].Capacity;reserves[weapon]=Weapons[weapon].Capacity*3;
            }
            else if(index==10){for(int weapon=0;weapon<Weapons.Length;weapon++)if((OwnedGunMask&(1<<weapon))!=0)reserves[weapon]+=Weapons[weapon].Capacity*3;}
            else Purchases[index]++;
            Money-=Prices[index];Save();Notify("Куплено: "+Goods[index]+" · −$"+Prices[index]);return true;
        }
        bool UsePurchase(int item)
        {
            if(item<0||item>=Purchases.Length||Purchases[item]<=0)return false;
            if(item==0)Player.Health=Mathf.Min(100,Player.Health+20);
            if(item==1){Player.Health=Mathf.Min(100,Player.Health+5);drunkUntil=Mathf.Max(Time.time,drunkUntil-15);}
            if(item==2)drunkUntil=Time.time+30;
            if(item==3)Player.Health=Mathf.Min(100,Player.Health+60);
            if(item==4){var vehicle=Player.Vehicle?Player.Vehicle:NearestVehicle(Player.Position,8);if(!vehicle || vehicle.Durability<=0){Notify("Нужен исправимый транспорт рядом на улице.");return false;}vehicle.Durability=100;}
            if(item==5)Armor=MaxArmor;
            Purchases[item]--;Save();Notify("Использовано: "+Goods[item]);return true;
        }
        void DrawShop(float left,float h)
        {
            Label(left,85,700,50,"МАГАЗИН 24/7 · $"+Money,heading);
            if(Button(new Rect(left+750,85,320,45),"МОИ ПОКУПКИ →"))Toggle("purchases");
            Label(left,145,1050,40,"Еда, напитки, снаряжение и личное оружие. Предметы используются через I → Инвентарь → Мои покупки.",small);
            shopScroll=GUI.BeginScrollView(new Rect(left,195,1080,h-240),shopScroll,new Rect(0,0,1050,Mathf.CeilToInt(Goods.Length/2f)*130));
            for(int i=0;i<Goods.Length;i++){float x=i%2*530,y=i/2*130;RectBox(new Rect(x,y,515,118),new Color(.1f,.16f,.12f));Label(x+16,y+10,480,32,Goods[i],heading);Label(x+16,y+47,310,57,GoodInfo[i],small);if(Button(new Rect(x+338,y+66,160,40),"КУПИТЬ $"+Prices[i]))BuyGood(i);}GUI.EndScrollView();
        }
        void DrawPurchases(float left,float h)
        {
            Label(left,85,1000,50,"МОИ ПОКУПКИ",heading);
            for(int item=0;item<6;item++){float y=165+item*70;Label(left,y,650,35,Goods[item]+" × "+Purchases[item]+" · "+GoodInfo[item],body);if(Button(new Rect(left+750,y,310,45),"ИСПОЛЬЗОВАТЬ"))UsePurchase(item);}
            Label(left,620,1050,50,"Покупки и гражданское оружие сохраняются. За новыми припасами — в магазин 24/7, отмеченный на карте.",small);
        }
        void LoadShop(int mask,int[] items,int[] loaded,int[] reserve)
        {
            OwnedGunMask=mask&((1<<0)|(1<<1)|(1<<2)|(1<<6));Purchases=new int[6];if(items!=null)for(int i=0;i<Mathf.Min(6,items.Length);i++)Purchases[i]=Mathf.Clamp(items[i],0,99);
            if(loaded!=null && reserve!=null)for(int i=0;i<Weapons.Length;i++){if(i<loaded.Length)magazines[i]=Mathf.Clamp(loaded[i],0,Weapons[i].Capacity);if(i<reserve.Length)reserves[i]=Mathf.Clamp(reserve[i],0,9999);}
        }
    }
}
