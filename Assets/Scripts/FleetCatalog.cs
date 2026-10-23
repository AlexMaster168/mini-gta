using UnityEngine;

namespace SunsetState
{
    public sealed class VehicleEntry
    {
        public string Name;public VehicleKind Kind;public string Info;
        public VehicleEntry(string name,VehicleKind kind,string info=""){Name=name;Kind=kind;Info=info;}
    }
    public static class FleetCatalog
    {
        public static readonly VehicleEntry[] Personal={
            new VehicleEntry("Ferrari F8 · стилизация",VehicleKind.Supercar,"Низкое спортивное купе · авторская модель по мотивам"),
            new VehicleEntry("Tesla Model S · стилизация",VehicleKind.Electric,"Электроседан · быстрый старт, тихий привод"),
            new VehicleEntry("BMW M5 · стилизация",VehicleKind.Sport,"Спортивный седан"),
            new VehicleEntry("Mercedes E63 · стилизация",VehicleKind.Sport,"Мощный представительский седан"),
            new VehicleEntry("Audi RS6 · стилизация",VehicleKind.SUV,"Быстрый дорожный автомобиль"),
            new VehicleEntry("Porsche 911 · стилизация",VehicleKind.Supercar,"Компактное спортивное купе"),
            new VehicleEntry("Lamborghini Huracan · стилизация",VehicleKind.Supercar,"Низкое купе с большим антикрылом"),
            new VehicleEntry("BMW X5 · стилизация",VehicleKind.SUV,"Представительский внедорожник"),
            new VehicleEntry("Mercedes G · стилизация",VehicleKind.SUV,"Тяжёлый служебный внедорожник"),
            new VehicleEntry("Premier · седан",VehicleKind.Sedan,"Универсальный городской автомобиль"),
            new VehicleEntry("Sultan · спорткар",VehicleKind.Sport,"Быстрый разгон и высокая скорость"),
            new VehicleEntry("Ranger · внедорожник",VehicleKind.SUV,"Высокий кузов и полный размер"),
            new VehicleEntry("Roadster · мотоцикл",VehicleKind.Motorcycle,"Два колеса, быстрый разгон"),
            new VehicleEntry("City Bike · велосипед",VehicleKind.Bicycle,"Педальный транспорт без двигателя"),
            new VehicleEntry("Hauler · грузовик",VehicleKind.Truck,"Грузовой кузов, плавное управление"),
            new VehicleEntry("Cityline · автобус",VehicleKind.Bus,"Большой городской автобус"),
            new VehicleEntry("Sparrow · вертолёт",VehicleKind.Helicopter,"Пробел — вверх · Ctrl — вниз"),
            new VehicleEntry("Coastline · катер",VehicleKind.Boat,"Прогулки вдоль западного побережья")};
        public static VehicleEntry[] ForRole(int role)
        {
            var list=new System.Collections.Generic.List<VehicleEntry>(BaseForRole(role));
            if(list.Count==0)return list.ToArray();
            if(!list.Exists(e=>e.Kind==VehicleKind.Motorcycle))list.Add(new VehicleEntry("Служебный мотоцикл",VehicleKind.Motorcycle));
            list.Add(new VehicleEntry("Служебный велосипед",VehicleKind.Bicycle));
            list.Add(new VehicleEntry("Ferrari Response · стилизация",VehicleKind.Supercar));list.Add(new VehicleEntry("Tesla Service · стилизация",VehicleKind.Electric));return list.ToArray();
        }
        static VehicleEntry[] BaseForRole(int role)
        {
            switch(role)
            {
                case 1:return new[]{new VehicleEntry("Police Cruiser",VehicleKind.Sedan),new VehicleEntry("Police Ranger",VehicleKind.SUV),new VehicleEntry("Police Moto",VehicleKind.Motorcycle),new VehicleEntry("Police Air",VehicleKind.Helicopter),new VehicleEntry("Police Rhino · танк",VehicleKind.Tank,"Тяжёлая бронетехника для штурма на максимальном уровне розыска")};
                case 2:return new[]{new VehicleEntry("FBI Unmarked",VehicleKind.Sedan),new VehicleEntry("FBI Tactical",VehicleKind.SUV),new VehicleEntry("FBI Air",VehicleKind.Helicopter),new VehicleEntry("FBI Rhino · танк",VehicleKind.Tank,"Бронированный штурмовой танк специального назначения")};
                case 3:return new[]{new VehicleEntry("Army Patriot",VehicleKind.SUV),new VehicleEntry("Army Barracks",VehicleKind.Truck),new VehicleEntry("Army Transport Air",VehicleKind.Helicopter),new VehicleEntry("Army Rhino · танк",VehicleKind.Tank,"Министерство обороны · основная боевая бронетехника")};
                case 4:return new[]{new VehicleEntry("Ambulance",VehicleKind.Truck),new VehicleEntry("EMS Response",VehicleKind.SUV),new VehicleEntry("Air Ambulance",VehicleKind.Helicopter)};
                case 5:return new[]{new VehicleEntry("Fire Engine",VehicleKind.Truck),new VehicleEntry("Rescue Command",VehicleKind.SUV)};
                case 6:return new[]{new VehicleEntry("Taxi Cab",VehicleKind.Sedan),new VehicleEntry("Taxi Family",VehicleKind.SUV)};
                case 7:return new[]{new VehicleEntry("Logistics Hauler",VehicleKind.Truck),new VehicleEntry("Logistics Ranger",VehicleKind.SUV)};
                case 8:return new[]{new VehicleEntry("Service Truck",VehicleKind.Truck),new VehicleEntry("Roadside Assistance",VehicleKind.SUV)};
                case 9:return new[]{new VehicleEntry("Grove Lowrider",VehicleKind.Sedan),new VehicleEntry("Grove Moto",VehicleKind.Motorcycle),new VehicleEntry("Grove Sport",VehicleKind.Sport)};
                case 10:return new[]{new VehicleEntry("Mayor Executive",VehicleKind.Sedan),new VehicleEntry("Mayor Security",VehicleKind.SUV)};
                case 11:return new[]{new VehicleEntry("Driving School",VehicleKind.Sedan),new VehicleEntry("Training Moto",VehicleKind.Motorcycle)};
                case 15:return new[]{new VehicleEntry("Legal Executive",VehicleKind.Sedan)};
                case 16:return new[]{new VehicleEntry("Mafia Executive",VehicleKind.Sedan),new VehicleEntry("Mafia Transport",VehicleKind.SUV),new VehicleEntry("Mafia Sport",VehicleKind.Sport)};
                case 17:return new[]{new VehicleEntry("Governor Executive",VehicleKind.Sedan),new VehicleEntry("Governor Security",VehicleKind.SUV)};
                case 12:return new[]{new VehicleEntry("Prison Patrol",VehicleKind.SUV),new VehicleEntry("Prison Transport",VehicleKind.Bus),new VehicleEntry("Prison Response",VehicleKind.Sedan)};
                default:return new VehicleEntry[0];
            }
        }
    }
    public sealed partial class GameRoot
    {
        Vector2 garageScroll;readonly System.Collections.Generic.Queue<VehicleMotor> spawnedVehicles=new System.Collections.Generic.Queue<VehicleMotor>();
        void DrawGarage(float left,float h)
        {
            bool faction=Panel=="garage";
            var entries=faction?FleetCatalog.ForRole(RoleIndex):FleetCatalog.Personal;
            Label(left,85,780,45,faction?"СЛУЖЕБНЫЙ ТРАНСПОРТ / "+Roles[RoleIndex].Name.ToUpper():"ЛИЧНЫЙ ТРАНСПОРТ",heading);
            if(Button(new Rect(left+800,85,260,42),"СКЕЙТ / СНОУБОРД · U"))Toggle("accessories");
            if(Button(new Rect(left,145,340,42),"G · СЛУЖЕБНЫЙ ГАРАЖ"))Toggle("garage");
            if(Button(new Rect(left+355,145,340,42),"H · ЛИЧНЫЙ ГАРАЖ"))Toggle("personal-garage");
            if(Button(new Rect(left+710,145,350,42),"ВЫЗВАТЬ / УБРАТЬ ДОСКУ · U")){Panel="";Time.timeScale=1;SetCursor(true);Player.ToggleBoard();}
            if(RoleIndex==13 && !Escaped){Label(left,200,1000,90,"Заключённым недоступен транспорт. Выполни побег или получи освобождение через задания (J).");return;}
            if(entries.Length==0){Label(left,220,900,50,"Служебного транспорта для этой роли пока нет.");return;}
            garageScroll=GUI.BeginScrollView(new Rect(left,200,1080,h-245),garageScroll,new Rect(0,0,1050,Mathf.CeilToInt(entries.Length/4f)*190));
            for(int i=0;i<entries.Length;i++)
            {
                var entry=entries[i];float x=i%4*263,y=i/4*190;
                RectBox(new Rect(x,y,250,176),new Color(.1f,.16f,.12f));GUI.color=faction?Roles[RoleIndex].Color:Roles[0].Color;
                Label(x+15,y+12,220,48,entry.Name,body);GUI.color=Color.white;Label(x+15,y+66,220,53,entry.Info==""?(faction?"Служебный транспорт\n"+Roles[RoleIndex].Name:"Личный транспорт"):entry.Info,small);
                if(Button(new Rect(x+12,y+126,226,36),"ПОЛУЧИТЬ И СЕСТЬ →"))SpawnFromGarage(entry,faction);
            }GUI.EndScrollView();
        }
        void SpawnFromGarage(VehicleEntry entry,bool faction)
        {
            if(RoleIndex==13 && !Escaped){Notify("Сначала нужно выбраться из тюрьмы.");return;}
            if(Player.Vehicle && Player.Vehicle.Flying && Player.Vehicle.transform.position.y>4){Notify("Сначала посади текущий вертолёт.");return;}
            if(RoleIndex==13 && Escaped && entry.Kind==VehicleKind.Helicopter){Notify("Во время розыска вертолёт недоступен.");return;}
            Vector3 pos=Player.Position;
            if(entry.Kind==VehicleKind.Boat)pos=new Vector3(-1020,1,210);
            else
            {
                // Always use an open intersection: large vehicles cannot spawn in a building.
                Vector3 city=StateBuilder.GarageCenter(pos);
                float grid=city==Vector3.zero?200:160;
                pos.x=city.x+Mathf.Clamp(Mathf.Round((pos.x-city.x)/grid)*grid,-grid*3,grid*3);pos.z=city.z+Mathf.Clamp(Mathf.Round((pos.z-city.z)/grid)*grid,-grid*3,grid*3);pos.y=2;
                int attempts=0;while(Physics.CheckBox(pos+Vector3.up*2,new Vector3(4,1.5f,8)) && attempts++<12)pos.z+=25;
            }
            Player.Teleport(pos+Vector3.right*9);
            var car=WorldBuilder.SpawnCar(this,pos,entry.Name,faction?Roles[RoleIndex].Color:new Color(.68f,.35f,.22f),false,0,entry.Kind);
            car.Faction=faction?RoleIndex:-1;if(faction)WorldBuilder.AddServiceDetails(car,RoleIndex);
            spawnedVehicles.Enqueue(car);while(spawnedVehicles.Count>5){var old=spawnedVehicles.Dequeue();if(old){Cars.Remove(old);Destroy(old.gameObject);}}
            Player.Enter(car);Panel="";Time.timeScale=1;SetCursor(true);
            Notify(entry.Name+" · "+(faction?"СЛУЖЕБНЫЙ · "+Roles[RoleIndex].Name:"Личный транспорт")+(entry.Kind==VehicleKind.Tank?" · танк":""));
        }
    }
}
