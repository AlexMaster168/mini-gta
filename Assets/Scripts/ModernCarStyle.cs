using System.Collections.Generic;
using UnityEngine;
namespace SunsetState
{
    // Original variations of the locally licensed sedan/SUV; these are not manufacturer CAD models.
    public sealed class ModernCarStyle:MonoBehaviour
    {
        readonly List<Mesh> owned=new List<Mesh>();readonly List<Material> materials=new List<Material>();public int DeformedMeshes=>owned.Count;
        public static void Apply(GameObject visual,VehicleKind kind,string title,Color paint)
        {
            bool super=kind==VehicleKind.Supercar,electric=kind==VehicleKind.Electric;
            if(!super&&!electric&&!title.Contains("стилизация"))return;
            var style=visual.AddComponent<ModernCarStyle>();float compression=super?.55f:electric?.85f:1;
            var data=visual.GetComponent<VehicleModelData>();
            foreach(var filter in visual.GetComponentsInChildren<MeshFilter>())
            {
                if(!filter.transform.parent||filter.transform.parent.name!="Body")continue;var mesh=Instantiate(filter.sharedMesh);var vertices=mesh.vertices;
                for(int i=0;i<vertices.Length;i++)if(vertices[i].y>.5f)vertices[i].y=.5f+(vertices[i].y-.5f)*compression;
                mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateNormals();filter.sharedMesh=mesh;style.owned.Add(mesh);
            }
            foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>())if(renderer.name.ToLowerInvariant().Contains("paint")){var material=new Material(renderer.sharedMaterial);if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",paint);if(material.HasProperty("_Color"))material.SetColor("_Color",paint);renderer.sharedMaterial=material;style.materials.Add(material);}
            data.Dimensions=new Vector3(data.Dimensions.x,.5f+(data.Dimensions.y-.5f)*compression,data.Dimensions.z);
            float front=data.Dimensions.z*.48f;
            Add(visual.transform,"LED running lights L",new Vector3(-.66f,.78f,front),new Vector3(.51f,.035f,.035f),new Color(.72f,.9f,1));
            Add(visual.transform,"LED running lights R",new Vector3(.66f,.78f,front),new Vector3(.51f,.035f,.035f),new Color(.72f,.9f,1));
            if(super){Add(visual.transform,"Rear wing",new Vector3(0,1.04f,-front*.8f),new Vector3(1.85f,.08f,.42f),Color.black);foreach(int side in new[]{-1,1})Add(visual.transform,"Wing support",new Vector3(side*.55f,.89f,-front*.8f),new Vector3(.07f,.3f,.13f),Color.black);}
            if(electric)Add(visual.transform,"Electric smooth front",new Vector3(0,.64f,front),new Vector3(1.1f,.23f,.035f),new Color(.17f,.2f,.23f));
            else if(title.Contains("BMW"))foreach(int side in new[]{-1,1})Add(visual.transform,"Twin grille",new Vector3(side*.16f,.67f,front),new Vector3(.25f,.22f,.04f),Color.black);
            else Add(visual.transform,"Sport grille",new Vector3(0,.63f,front),new Vector3(.83f,.22f,.04f),Color.black);
        }
        static void Add(Transform parent,string name,Vector3 position,Vector3 size,Color color){var obj=WorldBuilder.Box(name,Vector3.zero,size,color,null,false);obj.transform.SetParent(parent,false);obj.transform.localPosition=position;}
        void OnDestroy(){foreach(var mesh in owned)if(mesh)Destroy(mesh);foreach(var material in materials)if(material)Destroy(material);}
    }
}
