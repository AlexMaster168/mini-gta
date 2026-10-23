using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SunsetState.Editor
{
    public static class VehicleModelBaker
    {
        public static void BakeAll()
        {
            string source="Assets/ThirdParty/Vehicles";
            if(!Directory.Exists(source))return;
            Directory.CreateDirectory("Assets/Resources/DriveModels");Directory.CreateDirectory("Assets/VehicleMeshes");
            foreach(string path in Directory.GetFiles(source,"*.glb"))Bake(path);
        }
        static void Bake(string path)
        {
            string key=Path.GetFileNameWithoutExtension(path),prefabPath="Assets/Resources/DriveModels/"+key+".prefab";
            // Once baked, source updates explicitly remove the matching generated prefab.
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if(existing && existing.GetComponent<VehicleModelData>().Version>=6)return;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!source)throw new System.Exception("GLB import unavailable: "+path);
            var instance=Object.Instantiate(source);instance.name=key;
            Bounds initial=BoundsOf(instance);if(initial.size.x>initial.size.z*1.2f)instance.transform.rotation=Quaternion.Euler(0,90,0);
            // The engine drives along transform.forward (+Z), so the headlights must end up on the +Z side.
            Vector3 tail=GroupCenter(instance,"redlens","taillight","tail light");
            Vector3 front=GroupCenter(instance,"clearlens","headlight");
            if(tail.sqrMagnitude>0 && front.sqrMagnitude>0 && tail.z>front.z)instance.transform.rotation=instance.transform.rotation*Quaternion.Euler(0,180,0);
            Bounds bounds=BoundsOf(instance);instance.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            bounds=BoundsOf(instance);
            var output=new GameObject(key);var data=output.AddComponent<VehicleModelData>();data.Dimensions=bounds.size;data.Version=6;
            bool wheeled=key!="Helicopter" && key!="Boat";bool bike=key=="Motorcycle";int wheelCount=bike?2:4;
            var centers=new Vector3[wheelCount];var radii=new float[wheelCount];var counts=new int[wheelCount];
            var tireBounds=new Bounds[wheelCount];
            foreach(var r in instance.GetComponentsInChildren<MeshRenderer>())
            {
                string n=r.name.ToLowerInvariant();if(!n.Contains("tyre") && !n.Contains("tire"))continue;
                Bounds tire=LiveBounds(r);
                int wheel=bike?(tire.center.z>0?0:1):(tire.center.z>0?0:2)+(tire.center.x>0?1:0);
                if(counts[wheel]==0)tireBounds[wheel]=tire;else tireBounds[wheel].Encapsulate(tire);counts[wheel]++;
            }
            for(int i=0;i<wheelCount;i++){if(counts[i]>0){centers[i]=tireBounds[i].center;radii[i]=tireBounds[i].extents.y;}else{radii[i]=bike?.34f:.35f;centers[i]=new Vector3(bike?0:(i%2==0?-1:1)*bounds.size.x*.4f,radii[i],(bike?i==0:i<2)?bounds.size.z*.3f:-bounds.size.z*.3f);}}
            if(wheeled){data.WheelCenters=centers;data.WheelRadii=radii;}
            var groups=new Dictionary<string,Dictionary<Material,List<CombineInstance>>>();
            foreach(var filter in instance.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>();if(!renderer || !filter.sharedMesh)continue;
                for(int s=0;s<filter.sharedMesh.subMeshCount;s++)
                {
                    var materials=renderer.sharedMaterials;if(materials.Length==0)continue;Material material=materials[Mathf.Min(s,materials.Length-1)];
                    if(!material)continue;material.enableInstancing=true;
                    string n=filter.name.ToLowerInvariant();bool wheelPart=wheeled && !n.Contains("steering") && !n.Contains("wheelhouse") && (n.Contains("tyre")||n.Contains("tire")||(n.Contains("_rim")||n.Contains(" rim")||n.StartsWith("rim"))||n.Contains("brake rotor")||n.Contains("brake caliper")||n.Contains("wheel hub"));
                    int wheel=bike?(LiveBounds(renderer).center.z>0?0:1):(LiveBounds(renderer).center.z>0?0:2)+(LiveBounds(renderer).center.x>0?1:0);
                    string group=wheelPart?"Wheel"+wheel:"Body";
                    if(!groups.TryGetValue(group,out var materialsGroup)){materialsGroup=new Dictionary<Material,List<CombineInstance>>();groups[group]=materialsGroup;}
                    if(!materialsGroup.TryGetValue(material,out var list)){list=new List<CombineInstance>();materialsGroup[material]=list;}
                    list.Add(new CombineInstance{mesh=filter.sharedMesh,subMeshIndex=s,transform=(wheelPart?Matrix4x4.Translate(-centers[wheel]):Matrix4x4.identity)*filter.transform.localToWorldMatrix});
                }
            }
            int index=0;
            foreach(var group in groups)
            {
                var section=new GameObject(group.Key);section.transform.SetParent(output.transform,false);if(group.Key.StartsWith("Wheel"))section.transform.localPosition=centers[int.Parse(group.Key.Substring(5))];
                foreach(var pair in group.Value){
                var mesh=new Mesh{name=key+"_"+index,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh.RecalculateBounds();
                string meshPath="Assets/VehicleMeshes/"+key+"_v2_"+index+".asset";if(File.Exists(meshPath))AssetDatabase.DeleteAsset(meshPath);AssetDatabase.CreateAsset(mesh,meshPath);
                var part=new GameObject(pair.Key.name);part.transform.SetParent(section.transform,false);part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=pair.Key;index++;}
            }
            PrefabUtility.SaveAsPrefabAsset(output,prefabPath);Object.DestroyImmediate(output);Object.DestroyImmediate(instance);
            Debug.Log("BAKED_REAL_VEHICLE "+key+" · "+index+" material groups · "+bounds.size);
        }
        static Bounds BoundsOf(GameObject root){var renderers=root.GetComponentsInChildren<Renderer>();Bounds bounds=renderers.Length>0?LiveBounds(renderers[0]):new Bounds(root.transform.position,Vector3.one);foreach(var r in renderers)bounds.Encapsulate(LiveBounds(r));return bounds;}
        static Bounds LiveBounds(Renderer r)
        {
            var filter=r.GetComponent<MeshFilter>();var mesh=filter?filter.sharedMesh:null;
            if(!mesh)return new Bounds(r.transform.position,Vector3.zero);
            var local=mesh.bounds;var l2w=r.transform.localToWorldMatrix;
            var bounds=new Bounds(l2w.MultiplyPoint3x4(local.center),Vector3.zero);
            for(int i=0;i<8;i++)
            {
                var corner=new Vector3(i%2==0?local.min.x:local.max.x,i<4?local.min.y:local.max.y,(i%4)<2?local.min.z:local.max.z);
                bounds.Encapsulate(l2w.MultiplyPoint3x4(corner));
            }
            return bounds;
        }
        static Vector3 GroupCenter(GameObject root,params string[] keys)
        {
            Vector3 sum=Vector3.zero;int count=0;
            foreach(var r in root.GetComponentsInChildren<MeshRenderer>())
            {
                if(!r.sharedMaterial)continue;string material=r.sharedMaterial.name.ToLowerInvariant();
                foreach(var key in keys)if(material.Contains(key)){sum+=LiveBounds(r).center;count++;break;}
            }
            return count>0?sum/count:Vector3.zero;
        }
    }
}
