using System.Collections.Generic;
using UnityEngine;
namespace SunsetState
{
    public sealed class WorldDestructible:MonoBehaviour
    {
        public float Health;public bool Destroyed {get;private set;}public BoxCollider Shape {get;private set;}
        readonly List<GameObject> facade=new List<GameObject>();
        public void Initialize(float health){Health=health;Shape=GetComponent<BoxCollider>();}
        public static void Configure(GameObject obj,string name,Vector3 size)
        {
            bool building=name=="City building"||name=="Casino district building"||name=="San Fierro townhouse"||name=="Research lab"||name=="Field hospital"||WorldBuilder.Places.Exists(p=>p.Name==name);
            bool prop=name=="Supply crate"||name=="Fuel barrel"||name=="Outpost cover"||name=="Tree trunk";
            if(building||prop)obj.AddComponent<WorldDestructible>().Initialize(building?600:90);
        }
        public static void BindWorld(Transform world)
        {
            var surfaces=new List<MeshRenderer>();foreach(var renderer in world.GetComponentsInChildren<MeshRenderer>()){string label=renderer.name.ToLowerInvariant();if(label.Contains("roof")||label.Contains("windows")||label.Contains("crown")||label.Contains("окно фасада"))surfaces.Add(renderer);}
            foreach(var item in world.GetComponentsInChildren<WorldDestructible>())
            {
                Bounds bounds=item.Shape.bounds;
                foreach(var renderer in surfaces)
                {
                    Vector3 p=renderer.bounds.center;if(Mathf.Abs(p.x-bounds.center.x)<bounds.extents.x+1.5f && Mathf.Abs(p.z-bounds.center.z)<bounds.extents.z+1.5f && p.y>=bounds.min.y && p.y<=bounds.max.y+4)item.facade.Add(renderer.gameObject);
                }
            }
        }
        public void Damage(float amount)
        {
            if(Destroyed)return;Health=Mathf.Max(0,Health-amount);if(Health>0)return;Destroyed=true;var bounds=Shape.bounds;Shape.enabled=false;
            foreach(var renderer in GetComponents<Renderer>())renderer.enabled=false;
            foreach(var part in facade)if(part){foreach(var collider in part.GetComponents<Collider>())collider.enabled=false;foreach(var renderer in part.GetComponents<Renderer>())renderer.enabled=false;}
            InteriorSystem.Entrances.RemoveAll(e=>e.Shell==gameObject);
            var rubble=WorldBuilder.Box("Rubble",new Vector3(transform.position.x,bounds.min.y+.12f,transform.position.z),new Vector3(Mathf.Min(12,transform.localScale.x),.24f,Mathf.Min(10,transform.localScale.z)),new Color(.25f,.23f,.2f),null,false);
            VehicleDestruction.ExplosionEffect(transform.position,false);
        }
    }
}
