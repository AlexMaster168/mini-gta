using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace SunsetState
{
    public sealed class VehicleBodyDamage:MonoBehaviour
    {
        VehicleMotor car;readonly Dictionary<MeshFilter,Mesh> originals=new Dictionary<MeshFilter,Mesh>();readonly List<Mesh> owned=new List<Mesh>();readonly List<Renderer> glass=new List<Renderer>();
        public int MissingDoors{get;private set;}public bool WindowsBroken{get;private set;}
        bool Supported=>car.Kind!=VehicleKind.Tank&&car.Kind!=VehicleKind.Bicycle&&car.Kind!=VehicleKind.Motorcycle&&car.Kind!=VehicleKind.Boat;
        void Awake(){car=GetComponent<VehicleMotor>();}
        void Update(){var game=GameRoot.Instance;if(game&&game.Playing&&game.Panel=="")Refresh();}
        void Capture()
        {
            if(originals.Count>0)return;
            foreach(var filter in GetComponentsInChildren<MeshFilter>())
            {
                if(!filter.sharedMesh||filter.GetComponent<TextMesh>())continue;
                bool wheel=false;for(var parent=filter.transform;parent&&parent!=transform;parent=parent.parent)if(parent.name.StartsWith("Wheel"))wheel=true;
                if(wheel||filter.GetComponentInParent<RiderVisual>())continue;
                originals[filter]=filter.sharedMesh;var renderer=filter.GetComponent<Renderer>();string name=filter.name.ToLowerInvariant();
                if(renderer&&(name.Contains("glass")||name.Contains("glaz")||name.Contains("window")||name.Contains("windscreen")))glass.Add(renderer);
            }
        }
        public void Refresh()
        {
            if(!Supported)return;if(car.HealthFraction>=.99f){if(WindowsBroken||MissingDoors>0)Restore();return;}
            if(car.HealthFraction>.7f)return;Capture();
            WindowsBroken=true;foreach(var pane in glass)if(pane)pane.enabled=false;
            int target=car.HealthFraction<.15f?4:car.HealthFraction<.25f?3:car.HealthFraction<.4f?2:car.HealthFraction<.55f?1:0;
            while(MissingDoors<target){RemoveDoor(MissingDoors);MissingDoors++;}
        }
        public void Restore()
        {foreach(var entry in originals)if(entry.Key)entry.Key.sharedMesh=entry.Value;foreach(var pane in glass)if(pane)pane.enabled=true;foreach(var mesh in owned)if(mesh)Destroy(mesh);owned.Clear();MissingDoors=0;WindowsBroken=false;}
        void RemoveDoor(int index)
        {
            float side=index%2==0?-1:1;bool front=index<2;var size=car.Dimensions;
            var box=new Bounds(new Vector3(side*size.x*.46f,size.y*.43f,(front?.12f:-.13f)*size.z),new Vector3(size.x*.38f,size.y*.42f,size.z*.24f));
            foreach(var entry in originals)
            {
                var filter=entry.Key;if(!filter||!filter.GetComponent<Renderer>().enabled)continue;
                var source=filter.sharedMesh;var toCar=transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                var kept=new List<Vertex>();var removed=new List<Vertex>();var positions=source.vertices;var normals=source.normals;var uvs=source.uv;var triangles=source.triangles;
                for(int t=0;t<triangles.Length;t+=3)
                {
                    var polygon=new List<Vertex>(3);for(int v=0;v<3;v++){int id=triangles[t+v];polygon.Add(new Vertex{P=toCar.MultiplyPoint3x4(positions[id]),N=normals.Length>id?normals[id]:Vector3.up,UV=uvs.Length>id?uvs[id]:Vector2.zero});}
                    Bounds bounds=new Bounds(polygon[0].P,Vector3.zero);bounds.Encapsulate(polygon[1].P);bounds.Encapsulate(polygon[2].P);
                    if(!bounds.Intersects(box)){Triangulate(polygon,kept);continue;}
                    for(int plane=0;plane<6&&polygon.Count>=3;plane++)
                    {int axis=plane/2;bool greater=plane%2==0;float edge=greater?box.min[axis]:box.max[axis];Triangulate(Clip(polygon,axis,edge,!greater),kept);polygon=Clip(polygon,axis,edge,greater);}
                    Triangulate(polygon,removed);
                }
                if(removed.Count==0)continue;
                var mesh=MakeMesh(kept,toCar.inverse);filter.sharedMesh=mesh;if(owned.Remove(source))Destroy(source);owned.Add(mesh);
                var debris=new GameObject("Detached door panel");debris.transform.SetPositionAndRotation(transform.position,transform.rotation);var debrisMesh=MakeMesh(removed,Matrix4x4.identity);debris.AddComponent<MeshFilter>().sharedMesh=debrisMesh;debris.AddComponent<MeshRenderer>().sharedMaterial=filter.GetComponent<Renderer>().sharedMaterial;
                debris.AddComponent<LooseVehiclePanel>().Initialize(debrisMesh,car.Body.linearVelocity+transform.right*side*3+Vector3.up*2);
            }
        }
        struct Vertex{public Vector3 P,N;public Vector2 UV;public static Vertex Lerp(Vertex a,Vertex b,float t)=>new Vertex{P=Vector3.Lerp(a.P,b.P,t),N=Vector3.Lerp(a.N,b.N,t).normalized,UV=Vector2.Lerp(a.UV,b.UV,t)};}
        static List<Vertex> Clip(List<Vertex> input,int axis,float edge,bool greater)
        {
            var output=new List<Vertex>();if(input.Count==0)return output;var a=input[input.Count-1];bool aIn=greater?a.P[axis]>=edge:a.P[axis]<=edge;
            foreach(var b in input){bool bIn=greater?b.P[axis]>=edge:b.P[axis]<=edge;if(aIn!=bIn)output.Add(Vertex.Lerp(a,b,(edge-a.P[axis])/(b.P[axis]-a.P[axis])));if(bIn)output.Add(b);a=b;aIn=bIn;}return output;
        }
        static void Triangulate(List<Vertex> polygon,List<Vertex> output){for(int i=1;i+1<polygon.Count;i++){output.Add(polygon[0]);output.Add(polygon[i]);output.Add(polygon[i+1]);}}
        static Mesh MakeMesh(List<Vertex> data,Matrix4x4 toLocal)
        {
            var p=new Vector3[data.Count];var n=new Vector3[data.Count];var uv=new Vector2[data.Count];var indices=new int[data.Count];for(int i=0;i<data.Count;i++){p[i]=toLocal.MultiplyPoint3x4(data[i].P);n[i]=data[i].N;uv[i]=data[i].UV;indices[i]=i;}
            var mesh=new Mesh{name="Damaged vehicle panel",indexFormat=IndexFormat.UInt32};mesh.vertices=p;mesh.normals=n;mesh.uv=uv;mesh.triangles=indices;mesh.RecalculateBounds();return mesh;
        }
        void OnDestroy(){foreach(var mesh in owned)if(mesh)Destroy(mesh);}
    }
    public sealed class LooseVehiclePanel:MonoBehaviour
    {
        Mesh mesh;Vector3 velocity;float remaining=5;
        public void Initialize(Mesh geometry,Vector3 speed){mesh=geometry;velocity=speed;}
        void Update(){float dt=Time.deltaTime;velocity+=Vector3.down*9.81f*dt;transform.position+=velocity*dt;if(transform.position.y<.15f){transform.position=new Vector3(transform.position.x,.15f,transform.position.z);velocity=Vector3.zero;}remaining-=dt;if(remaining<=0)Destroy(gameObject);}
        void OnDestroy(){if(mesh)Destroy(mesh);}
    }
}
