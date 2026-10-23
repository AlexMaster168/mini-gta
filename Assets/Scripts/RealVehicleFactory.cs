using UnityEngine;

namespace SunsetState
{
    public static class RealVehicleFactory
    {
        public static VehicleMotor Spawn(GameRoot game,Vector3 position,string title,Color color,bool traffic,float yaw,VehicleKind kind)
        {
            string key=kind==VehicleKind.Bicycle?"Bicycle":kind==VehicleKind.Motorcycle?"Motorcycle":kind==VehicleKind.Truck?"Truck":kind==VehicleKind.Bus?"Bus":kind==VehicleKind.Helicopter?"Helicopter":kind==VehicleKind.Boat?"Boat":kind==VehicleKind.Tank?"Tank":kind==VehicleKind.SUV?"SUV":"Sedan";
            if(title.Contains("Police"))key=kind==VehicleKind.SUV?"PoliceSUV":kind==VehicleKind.Sedan?"PoliceSedan":key;
            if(title.Contains("Ambulance") && kind==VehicleKind.Truck)key="Ambulance";
            if(title.Contains("Fire Engine"))key="FireEngine";
            if(title.Contains("Service Truck"))key="TowTruck";
            var prefab=Resources.Load<GameObject>("DriveModels/"+key);if(!prefab)return null;
            var root=new GameObject(title);root.transform.SetPositionAndRotation(new Vector3(position.x,kind==VehicleKind.Boat?.3f:position.y+.4f,position.z),Quaternion.Euler(0,yaw,0));
            var visual=Object.Instantiate(prefab,root.transform,false);visual.name="Detailed model";
            ModernCarStyle.Apply(visual,kind,title,color);
            Vector3 size=visual.GetComponent<VehicleModelData>().Dimensions;
            var car=root.AddComponent<VehicleMotor>();car.Title=title;car.Kind=kind;car.Traffic=traffic;car.Dimensions=size;
            var collision=root.AddComponent<BoxCollider>();
            if(kind==VehicleKind.Helicopter){collision.center=new Vector3(0,1.4f,0);collision.size=new Vector3(2.6f,2.5f,5);car.Dimensions=new Vector3(2.8f,3.3f,9);}
            else if(kind==VehicleKind.Boat){collision.center=new Vector3(0,.6f,0);collision.size=new Vector3(size.x*.8f,.8f,size.z*.85f);}
            else{collision.center=new Vector3(0,Mathf.Max(.8f,size.y*.55f),0);collision.size=new Vector3(size.x*.85f,Mathf.Max(.55f,size.y*.65f),size.z*.85f);}
            if(kind==VehicleKind.Helicopter)
            {
                // Separate spinning rotor above the static detailed airframe.
                var rotor=WorldBuilder.Box("Animated rotor",Vector3.zero,new Vector3(8,.06f,.2f),new Color(.1f,.12f,.13f),null,false);rotor.transform.SetParent(root.transform,false);rotor.transform.localPosition=new Vector3(0,3.35f,0);car.Rotor=rotor.transform;
            }
            if(kind==VehicleKind.Motorcycle)
            {
                var rider=CharacterModels.Create(root.transform,0,color);rider.localPosition=new Vector3(0,.05f,-.2f);rider.localScale*=.85f;CharacterModels.FreezePose(rider,true);rider.gameObject.AddComponent<RiderVisual>();
            }
            // Preserve authored textures; only clear-coat body paint gets a mild fleet tint.
            foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>())
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
            var lod=visual.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.015f,visual.GetComponentsInChildren<Renderer>())});lod.RecalculateBounds();
            car.Initialize();game.Cars.Add(car);if(!car.OnWater)car.GetComponent<VehicleRecovery>().ValidateSpawn();return car;
        }
    }
}
