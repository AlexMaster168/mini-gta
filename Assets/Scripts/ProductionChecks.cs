using System;
using System.IO;
using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        void CheckProductionServices()
        {
            string folder=Path.Combine(Application.temporaryCachePath,"sunset-storage-check-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);string path=Path.Combine(folder,"save.json");
            try
            {
                var data=new SaveData{Role=2,Money=100,Health=90};SaveStore.Write(path,data);bool recovered;Require(SaveStore.Read(path,out recovered).Money==100&&!recovered,"Atomic save must read with verified checksum");
                data.Money=200;SaveStore.Write(path,data);File.WriteAllText(path,"{broken");Require(SaveStore.Read(path,out recovered).Money==100&&recovered,"Corrupt primary must recover previous valid backup");
                data.Money=300;SaveStore.Write(path,data);Require(SaveStore.Parse(File.ReadAllText(path+".bak")).Money==100,"Replacing a corrupt file must preserve good backup");
                string modified=File.ReadAllText(path).Replace("\"Money\":300","\"Money\":999");File.WriteAllText(path,modified);Require(SaveStore.Read(path,out recovered).Money==100&&recovered,"Checksum must reject altered JSON");
                string legacy=JsonUtility.ToJson(new SaveData{Role=1,Money=77,Health=100});File.WriteAllText(path,legacy);Require(SaveStore.Read(path,out recovered).Money==77&&!recovered,"Legacy unversioned save must remain compatible");
                string future=JsonUtility.ToJson(new SaveData{SaveVersion=99,Role=1,Health=100});File.WriteAllText(path,future);bool blocked=false;try{SaveStore.Write(path,data);}catch(UnsupportedSaveVersionException){blocked=true;}Require(blocked&&File.ReadAllText(path)==future,"Future save versions must never be overwritten");
                var settings=new UserSettingsData{Master=float.NaN,Music=99,Engine=-10,MouseSensitivity=100,Quality=9,FrameLimit=999,Width=-1,Height=99999};settings.Normalize();Require(settings.Master==.8f&&settings.Music==.6f&&settings.Engine==0&&settings.MouseSensitivity==3&&settings.Quality==2&&settings.FrameLimit==120&&settings.Width==960&&settings.Height==2160,"Invalid settings must be bounded safely");
                string preferences=Path.Combine(folder,"settings.json");settings.MouseSensitivity=1.7f;SaveStore.AtomicWrite(preferences,JsonUtility.ToJson(settings),true);settings.MouseSensitivity=.7f;SaveStore.AtomicWrite(preferences,JsonUtility.ToJson(settings),true);File.WriteAllText(preferences,"broken");Require(Mathf.Approximately(RuntimeSettings.Read(preferences).MouseSensitivity,1.7f),"Corrupted settings must fall back to previous valid file");
                Debug.Log("SUNSET_PRODUCTION_ATOMIC_SAVE_CHECKSUM_BACKUP_LEGACY_FUTURE_SETTINGS_OK");
            }
            finally{foreach(string file in Directory.GetFiles(folder))File.Delete(file);Directory.Delete(folder);}
        }
        void CheckTransportDurability()
        {
            foreach(VehicleKind kind in Enum.GetValues(typeof(VehicleKind)))
            {
                var car=WorldBuilder.SpawnCar(this,new Vector3(3400,2,-1800),"Durability check",Color.gray,false,0,kind);
                float expected=kind==VehicleKind.Tank?2400:kind==VehicleKind.Truck||kind==VehicleKind.Bus?900:kind==VehicleKind.Motorcycle||kind==VehicleKind.Bicycle?360:600;
                Require(car.MaxDurability==expected&&car.Durability==expected,"Every vehicle kind must spawn with doubled durability: "+kind);
                car.GetComponent<VehicleDestruction>().BulletHit(32,car.transform.position+Vector3.up);Require(car.Durability>expected*.95f&&car.Durability<expected,"Ordinary hit must leave most health intact: "+kind);
                car.Durability=car.MaxDurability*.4f;Require(Mathf.Approximately(car.HealthFraction,.4f),"Damage effects must retain proportional thresholds: "+kind);car.Durability=car.MaxDurability;Require(car.HealthFraction==1,"Repair must restore full durability: "+kind);
                Cars.Remove(car);car.gameObject.SetActive(false);Destroy(car.gameObject);
            }
            Debug.Log("SUNSET_ALL_12_VEHICLE_KINDS_DOUBLE_DURABILITY_DAMAGE_REPAIR_OK");
        }
    }
}
