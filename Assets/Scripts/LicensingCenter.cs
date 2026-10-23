using UnityEngine;
namespace SunsetState
{
    public sealed class RoadServiceObject:MonoBehaviour
    {
        public bool Road,Broken;public string Advice;Renderer surface;TextMesh label;
        public void Initialize(bool road,bool broken,string advice)
        {Road=road;Advice=advice;surface=GetComponent<Renderer>();label=GetComponentInChildren<TextMesh>();SetBroken(broken);}
        public void SetBroken(bool value)
        {Broken=value;if(surface)surface.sharedMaterial=WorldBuilder.Mat(value?new Color(.25f,.17f,.11f):Road?new Color(.22f,.24f,.25f):new Color(.2f,.4f,.65f));if(label)label.text=value?"ПОВРЕЖДЕНО · РЕМОНТ":"60 · ПДД";}
        public static void Build(Transform world)
        {
            Vector3[] points={new Vector3(210,0,200),new Vector3(-190,0,0),StateBuilder.SanFierro+new Vector3(170,-1,0),StateBuilder.LasVenturas+new Vector3(10,-1,160),StateBuilder.NorthDistrict+new Vector3(170,-1,0)};
            for(int i=0;i<points.Length;i++)
            {
                Vector3 p=points[i];WorldBuilder.Box("Sign post",p+Vector3.up*1.5f,new Vector3(.13f,3,.13f),Color.gray,world);
                var sign=WorldBuilder.Box("Road sign",p+Vector3.up*2.7f,new Vector3(1.1f,1.1f,.08f),Color.blue,world);InteriorSystem.Label(p+new Vector3(0,2.7f,-.1f),"60 · ПДД",sign.transform,.025f);sign.AddComponent<RoadServiceObject>().Initialize(false,i%2==0,"В городе держи безопасную скорость; перед поворотом снижай скорость и уступай пешеходам.");
                var patch=WorldBuilder.Box("Road surface repair",p+new Vector3(-5,.13f,8),new Vector3(3,.025f,5),Color.gray,world,false);patch.AddComponent<RoadServiceObject>().Initialize(true,i%2==1,"Перед дорожными работами снизь скорость и объезжай ремонтируемый участок по свободной полосе.");
            }
        }
    }
    public sealed partial class GameRoot
    {
        public int PersonalLicenses;RoadServiceObject roadRepairTarget;float nextRoadAdvice,nextOfficeLicense;
        bool PurchaseLicense(int id)
        {
            if(!InInterior || insideBuilding.Role!=11 || id<0||id>=3)return false;
            if((PersonalLicenses&(1<<id))!=0){Notify("Эта лицензия уже получена.");return false;}
            if(Money<LicensePrices[id]){Notify("Недостаточно денег на оформление.");return false;}
            Money-=LicensePrices[id];PersonalLicenses|=1<<id;Save();Notify("Выдана: "+LicenseNames[id]);return true;
        }
        void DrawLicenseCenter(float left,float h)
        {
            Label(left,85,1000,45,"ЦЕНТР ЛИЦЕНЗИРОВАНИЯ / ДОРОЖНАЯ СЛУЖБА",heading);
            for(int id=0;id<3;id++)
            {float y=160+id*90;Label(left,y,700,30,LicenseNames[id],body);if(Button(new Rect(left+730,y,340,44),(PersonalLicenses&(1<<id))!=0?"ПОЛУЧЕНА":"ОФОРМИТЬ · $"+LicensePrices[id]))PurchaseLicense(id);}
            Label(left,450,1030,75,"ПАМЯТКА ПДД: STOP — полная остановка · треугольник — уступи дорогу.\nУ перекрёстка снижай скорость. Перед работами держи дистанцию и выбирай свободную полосу.\nНа карте M показаны дороги и маршрут к текущей цели.",body);
            if(RoleIndex==11)
            {
                if(Button(new Rect(left,565,520,52),"ВЫДАТЬ ЛИЦЕНЗИЮ ПОСЕТИТЕЛЮ · +$250"))
                {if(Time.unscaledTime>=nextOfficeLicense){nextOfficeLicense=Time.unscaledTime+15;Money+=250;AwardXP(30);Save();Notify("Посетитель получил водительскую лицензию.");}else Notify("Следующий посетитель ещё не подошёл.");}
                if(Button(new Rect(left+550,565,520,52),"ВЫЕЗД НА РЕМОНТ ЗНАКА / ДОРОГИ"))StartRoadServiceJob();
            }
        }
        void StartRoadServiceJob()
        {
            if(RoleIndex!=11)return;RoadServiceObject target=null;float best=float.MaxValue;
            foreach(var item in FindObjectsByType<RoadServiceObject>(FindObjectsSortMode.None))if(item.Broken){float d=Vector3.Distance(item.transform.position,SavePosition);if(d<best){best=d;target=item;}}
            if(!target){Notify("Повреждённых знаков и участков пока нет.");return;}
            var mission=new Mission(target.Road?"Ремонт дорожного участка":"Восстановление дорожного знака","Прибудь на место, выйди из машины и используй дорожный ремкомплект: 9, затем F. После ремонта вернись в центр.",550,false,new[]{new Vector3(target.transform.position.x,1,target.transform.position.z),WorldBuilder.Places[11].Position},"9 — дорожный ремкомплект · F — ремонт","Вернись с отчётом в центр лицензирования");
            ActivateMission(mission);roadRepairTarget=target;
        }
        bool RepairTraffic()
        {
            if(RoleIndex!=11)return false;RoadServiceObject target=null;float best=7;
            foreach(var item in FindObjectsByType<RoadServiceObject>(FindObjectsSortMode.None))if(item.Broken){float d=Vector3.Distance(Player.Position,item.transform.position);if(d<best){best=d;target=item;}}
            if(!target)return false;target.SetBroken(false);AwardXP(25);Notify(target.Road?"Дорожное покрытие восстановлено.":"Дорожный знак восстановлен.");Save();return true;
        }
        void UpdateRoadAdvice()
        {
            if(RoleIndex!=11 || !Player.Vehicle || Time.unscaledTime<nextRoadAdvice)return;
            foreach(var sign in FindObjectsByType<RoadServiceObject>(FindObjectsSortMode.None))if(Vector3.Distance(Player.Position,sign.transform.position)<65){Notify(sign.Advice+(sign.Broken?" Рядом нужен ремонт: 9 → F.":""));nextRoadAdvice=Time.unscaledTime+45;break;}
        }
    }
}
