using UnityEngine;
namespace SunsetState
{
    public static class BlackMarket
    {
        public static readonly Vector3[] Points=new Vector3[2];
        public static readonly GameObject[] Vendors=new GameObject[2];
        public static void Build(Transform root)
        {
            var centers=new[]{new Vector3(-820,1,-1160),ViceCityIsland.Center+new Vector3(570,0,210)};
            for(int i=0;i<centers.Length;i++)
            {
                var center=SpawnSafety.VehiclePosition(centers[i],new Vector3(18,5,18),0);
                var stall=new GameObject("Чёрный рынок · Гуррам").transform;stall.SetParent(root,false);
                WorldBuilder.Box("Market yard",center+Vector3.down*.1f,new Vector3(22,.12f,22),new Color(.22f,.23f,.20f),stall);
                WorldBuilder.Box("Market canopy",center+new Vector3(0,3.4f,5),new Vector3(12,.16f,7),new Color(.16f,.20f,.17f),stall);
                foreach(int side in new[]{-1,1})WorldBuilder.Box("Market post",center+new Vector3(side*5.7f,1.6f,5),new Vector3(.15f,3.2f,.15f),Color.gray,stall);
                WorldBuilder.Box("Market counter",center+new Vector3(0,.7f,5),new Vector3(8,1.4f,1.2f),new Color(.32f,.24f,.17f),stall);
                for(int box=0;box<3;box++)WorldBuilder.Box("Market supplies",center+new Vector3(-4+box*3,.6f,8),new Vector3(1.6f,1.2f,1.6f),new Color(.32f,.34f,.25f),stall);
                var actor=new GameObject("Гуррам · снятие розыска");actor.transform.SetParent(stall,true);
                Vector3 requested=center+new Vector3(0,0,2);actor.transform.position=SpawnSafety.TryPlayerPosition(requested,out var safe)?safe:requested;
                actor.transform.rotation=Quaternion.Euler(0,180,0);CharacterModels.Create(actor.transform,16,new Color(.25f,.22f,.18f));actor.AddComponent<NeutralActor>();
                var collider=actor.AddComponent<CapsuleCollider>();collider.height=1.8f;collider.radius=.3f;collider.center=Vector3.up*.9f;
                Points[i]=actor.transform.position;Vendors[i]=actor;
                InteriorSystem.Label(center+new Vector3(0,3,3.9f),"ГУРРАМ · ЧЁРНЫЙ РЫНОК · E",stall,.06f);
            }
            WorldBuilder.Box("Black market access road",new Vector3(-800,.10f,-980),new Vector3(12,.14f,360),new Color(.20f,.23f,.24f),root);
            WorldBuilder.Box("Black market driveway",new Vector3(-810,.10f,-1160),new Vector3(32,.14f,12),new Color(.20f,.23f,.24f),root);
        }
    }
    public sealed partial class GameRoot
    {
        public int WantedClearanceCost=>Mathf.Clamp(Wanted,0,5)*500;
        int NearbyBlackMarket()
        {
            if(Player.Vehicle||InInterior)return -1;
            for(int i=0;i<BlackMarket.Points.Length;i++)if(BlackMarket.Vendors[i]&&NpcEffects.Alive(BlackMarket.Vendors[i])&&Vector3.Distance(Player.Position,BlackMarket.Points[i])<4)return i;
            return -1;
        }
        bool TryBlackMarketAction(){if(NearbyBlackMarket()<0)return false;Toggle("black-market");return true;}
        bool BuyWantedClearance()
        {
            if(NearbyBlackMarket()<0){Notify("Подойди к Гурраму на чёрном рынке и выйди из машины.");return false;}
            if(Wanted<=0){Notify("Гуррам: тебя сейчас не ищут. Платить не нужно.");return false;}
            if(RoleIndex==13&&!Escaped){Notify("Сначала нужно выбраться из тюрьмы.");return false;}
            int price=WantedClearanceCost;if(Money<price){Notify("Гуррам: нужно $"+price+". Не хватает $"+(price-Money));return false;}
            Money-=price;Wanted=0;wantedTimer=0;arrest=0;footArrest=0;nextResponse=0;
            ClearWantedResponse();
            if(pursuer){var car=pursuer;pursuer=null;DismissWantedCar(car);}
            PhoneMessage("Гуррам","Розыск снят. Патрули прекратили преследование. Новое преступление снова привлечёт полицию.");
            Panel="";Time.timeScale=1;SetCursor(true);Notify("Гуррам снял розыск. Полиция и ФБР прекратили преследование. −$"+price);Save();return true;
        }
        void DismissWantedCar(VehicleMotor car)
        {
            if(!car)return;car.SetEmergency(false);car.AIControls=null;var agent=car.GetComponent<HostileAgent>();if(agent){agent.enabled=false;Enemies.Remove(agent);Destroy(agent);}
            if(car.Driver==Player)return;Cars.Remove(car);car.gameObject.SetActive(false);Destroy(car.gameObject);
        }
        void DrawBlackMarket(float left,float h)
        {
            Label(left,90,1040,50,"ЧЁРНЫЙ РЫНОК · ГУРРАМ",heading);
            Label(left,165,1040,100,"«Помогу исчезнуть из ориентировок. Цена — $500 за звезду. После сделки полиция и ФБР отзовут преследование. Если снова нарушишь закон, розыск вернётся».\nРозыск: "+Wanted+" · наличные: $"+Money,body);
            if(Wanted>0){if(Button(new Rect(left,320,1040,70),"СНЯТЬ ВЕСЬ РОЗЫСК · $"+WantedClearanceCost))BuyWantedClearance();}
            else Label(left,320,1040,60,"Розыска нет. Услуга не требуется, деньги не списываются.",heading);
            if(Button(new Rect(left,430,1040,60),"ПОКАЗАТЬ РЫНОК НА GPS"))SetGps(BlackMarket.Points[OnVice?1:0],"Чёрный рынок · Гуррам");
        }
    }
}
