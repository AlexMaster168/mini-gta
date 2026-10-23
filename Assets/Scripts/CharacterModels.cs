using UnityEngine;
namespace SunsetState
{
    public static class CharacterModels
    {
        public static Transform Create(Transform parent,int role,Color color,bool female=false,int variant=0)
        {
            string key=role==1||role==12?"Suit_Male":role==3?"Soldier_Male":role==4?"Doctor_Male_Young":role==5||role==7||role==8?"Worker_Male":role==2||role==10||role==11||role==15||role==16||role==17?"Suit_Male":"Casual_Male";
            if(female)key=key.Replace("_Male","_Female");
            if(key=="Casual_Male" || key=="Casual_Female"){string gender=female?"Female":"Male";if(variant%3>0)key="Casual"+(variant%3+1)+"_"+gender;}
            var prefab=Resources.Load<GameObject>("Characters/"+key);
            if(!prefab){var fallback=GameObject.CreatePrimitive(PrimitiveType.Capsule);Object.Destroy(fallback.GetComponent<Collider>());fallback.transform.SetParent(parent,false);fallback.transform.localPosition=Vector3.up*.9f;fallback.transform.localScale=new Vector3(.65f,.9f,.65f);fallback.GetComponent<Renderer>().sharedMaterial=WorldBuilder.Mat(color);return fallback.transform;}
            // Keep normalization outside the FBX root: legacy animation can overwrite its scale.
            var wrapper=new GameObject("Character").transform;
            var identity=wrapper.gameObject.AddComponent<CharacterAppearance>();identity.AssetKey=key;identity.Role=role;identity.Female=female;
            var model=Object.Instantiate(prefab,wrapper,false);model.name="Animated human";
            var animation=model.GetComponentInChildren<Animation>();
            if(animation){animation.playAutomatically=false;foreach(AnimationState state in animation)if(state.name.ToLowerInvariant().Contains("idle")){state.wrapMode=WrapMode.Loop;state.clip.SampleAnimation(model,0);animation.Play(state.name);break;}}
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;

            {
                var owned=new System.Collections.Generic.List<Material>();
                foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var materials=renderer.sharedMaterials;
                    for(int i=0;i<materials.Length;i++)if(materials[i] && (materials[i].name.Contains("Skin") || (role==1 || role==13 || role==9 || role==14 || role==5 || role==6 || role==8 || role==12) && materials[i].name.Contains("Shirt") || role==13 && materials[i].name.Contains("Pants")))
                    {var uniform=new Material(materials[i]);uniform.color=materials[i].name.Contains("Skin")?new Color(.72f,.51f,.38f):role==1?new Color(.08f,.18f,.34f):role==13||role==5?new Color(1,.36f,.06f):role==9?new Color(.18f,.48f,.2f):role==6?new Color(.95f,.75f,.15f):role==8?new Color(.15f,.35f,.6f):role==12?new Color(.3f,.35f,.4f):new Color(.32f,.12f,.13f);materials[i]=uniform;owned.Add(uniform);}
                    renderer.sharedMaterials=materials;
                }
                wrapper.gameObject.AddComponent<CharacterUniform>().Materials=owned.ToArray();
            }
            Bounds bounds=GeometryBounds(model);
            wrapper.localScale=Vector3.one*(1.8f/Mathf.Max(.01f,bounds.size.y));
            bounds=GeometryBounds(model);
            model.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            wrapper.SetParent(parent,false);
            foreach(var t in wrapper.GetComponentsInChildren<Transform>())t.gameObject.layer=parent.gameObject.layer;
            wrapper.gameObject.AddComponent<CharacterMotion>();UniformDetails.Apply(wrapper,role);return wrapper;
        }
        public static Bounds GeometryBounds(GameObject model)
        {
            Bounds result=new Bounds(model.transform.position,Vector3.zero);bool first=true;
            foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh=new Mesh();renderer.BakeMesh(mesh,false);mesh.RecalculateBounds();var local=mesh.bounds;
                for(int i=0;i<8;i++){Vector3 corner=local.center+Vector3.Scale(local.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));Vector3 point=renderer.transform.rotation*corner+renderer.transform.position;if(first){result=new Bounds(point,Vector3.zero);first=false;}else result.Encapsulate(point);}if(Application.isPlaying)Object.Destroy(mesh);else Object.DestroyImmediate(mesh);
            }
            if(first)foreach(var r in model.GetComponentsInChildren<Renderer>()){if(first){result=r.bounds;first=false;}else result.Encapsulate(r.bounds);}
            return result;
        }
        public static void FreezePose(Transform character,bool seated=false)
        {
            var motion=character.GetComponent<CharacterMotion>();if(motion)motion.enabled=false;
            var animation=character.GetComponentInChildren<Animation>();
            if(animation && seated)foreach(AnimationState state in animation)if(state.name.ToLowerInvariant().Contains("sitdown")){state.clip.SampleAnimation(animation.gameObject,state.length);animation.enabled=false;return;}
            if(animation)animation.enabled=false;
            if(!seated)return;
            foreach(var bone in character.GetComponentsInChildren<Transform>())
            {
                if(bone.name.StartsWith("UpperLeg"))bone.localRotation*=Quaternion.Euler(-70,0,0);
                if(bone.name.StartsWith("LowerLeg"))bone.localRotation*=Quaternion.Euler(90,0,0);
                if(bone.name.StartsWith("UpperArm"))bone.localRotation*=Quaternion.Euler(-35,0,0);
            }
        }
    }
    public sealed class CharacterAppearance:MonoBehaviour{public string AssetKey;public int Role;public bool Female;}
    public sealed class CharacterUniform:MonoBehaviour
    {
        public Material[] Materials;
        void OnDestroy(){if(Materials!=null)foreach(var material in Materials)if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
    }
    public sealed class CharacterMotion:MonoBehaviour
    {
        Animation animationPlayer;string idle,walk;Vector3 previous;
        void Start(){previous=transform.position;animationPlayer=GetComponentInChildren<Animation>();if(!animationPlayer)return;foreach(AnimationState state in animationPlayer){state.wrapMode=WrapMode.Loop;string name=state.name.ToLowerInvariant();if(name.Contains("idle"))idle=state.name;if(name.EndsWith("|walk")||name=="walk")walk=state.name;}if(idle!=null)animationPlayer.Play(idle);}
        void Update(){float speed=(transform.position-previous).magnitude/Mathf.Max(Time.deltaTime,.001f);previous=transform.position;if(animationPlayer){string clip=speed>.3f?walk:idle;if(clip!=null && !animationPlayer.IsPlaying(clip))animationPlayer.CrossFade(clip,.2f);}}
    }
}
