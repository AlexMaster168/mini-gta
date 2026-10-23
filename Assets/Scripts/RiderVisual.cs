using System.Collections.Generic;
using UnityEngine;
namespace SunsetState
{
    public sealed class BicycleCrank:MonoBehaviour
    {
        VehicleMotor car;Transform pivot;public float Angle {get;private set;}
        void Awake()
        {
            car=GetComponent<VehicleMotor>();pivot=new GameObject("Animated pedals").transform;pivot.SetParent(transform,false);pivot.localPosition=new Vector3(0,.4f,-.05f);
            var old=transform.Find("Pedal crank");if(old)old.SetParent(pivot,true);
            for(int side=-1;side<=1;side+=2)
            {
                var arm=WorldBuilder.Box("Crank arm",Vector3.zero,new Vector3(.05f,.17f,.05f),new Color(.3f,.32f,.34f),null,false);arm.transform.SetParent(pivot,false);arm.transform.localPosition=new Vector3(side*.23f,side*.085f,0);
                var pedal=WorldBuilder.Box("Pedal",Vector3.zero,new Vector3(.16f,.04f,.12f),Color.black,null,false);pedal.transform.SetParent(pivot,false);pedal.transform.localPosition=new Vector3(side*.23f,side*.17f,0);
            }
        }
        public Vector3 FootTarget(float side)=>transform.TransformPoint(new Vector3(side*.23f,.4f,-.05f)+Quaternion.Euler(Angle,0,0)*Vector3.up*(side*.17f));
        void Update()
        {
            var game=GameRoot.Instance;if(!game || !game.Playing || game.Panel!="")return;
            if(car.Throttle>.05f && car.Speed>.5f)Angle=Mathf.Repeat(Angle+Mathf.Min(720,car.Speed/3.6f*85)*Time.deltaTime,360);
            pivot.localRotation=Quaternion.Euler(Angle,0,0);
        }
    }
    public sealed class RiderVisual:MonoBehaviour
    {
        Transform human,hips;VehicleMotor car;Renderer[] original;int role=-1;bool female;int outfit;Vector3 seat;
        readonly Dictionary<string,Transform> bones=new Dictionary<string,Transform>();readonly Dictionary<Transform,Quaternion> rest=new Dictionary<Transform,Quaternion>();
        public Transform Rider=>human;
        public Vector3 SeatPosition=>car.transform.TransformPoint(seat+Vector3.up*.1f);
        void Awake(){car=GetComponentInParent<VehicleMotor>();original=GetComponentsInChildren<Renderer>();}
        void Rebuild(int index)
        {
            if(human)Destroy(human.gameObject);role=index;bones.Clear();rest.Clear();if(index<0)return;
            var game=GameRoot.Instance;female=game.FemaleChoices[game.RoleIndex];outfit=game.SkinChoices[game.RoleIndex];human=CharacterModels.Create(car.transform,index,game.Roles[index].Color,female,outfit);CharacterModels.FreezePose(human,true);
            foreach(var bone in human.GetComponentsInChildren<Transform>()){bones[bone.name]=bone;rest[bone]=bone.localRotation;}
            bones.TryGetValue("Hips",out hips);
            seat=car.Kind==VehicleKind.Bicycle?new Vector3(0,1.08f,-.3f):new Vector3(0,.78f,-.3f);
            foreach(var renderer in car.GetComponentsInChildren<MeshRenderer>())
                if(renderer.name.ToLowerInvariant().Contains("saddle") && !renderer.transform.IsChildOf(human))
                {seat=car.transform.InverseTransformPoint(new Vector3(renderer.bounds.center.x,renderer.bounds.max.y,renderer.bounds.center.z));break;}
            Fit();
        }
        void Fit()
        {
            if(!human)return;foreach(var pair in rest)if(pair.Key)pair.Key.localRotation=pair.Value;
            human.localRotation=Quaternion.identity;
            if(hips)human.position+=SeatPosition-hips.position;else human.localPosition=seat-Vector3.up*.65f;
            if(bones.TryGetValue("Spine",out var spine))spine.localRotation*=Quaternion.Euler(12,0,0);
            var crank=car.GetComponent<BicycleCrank>();
            foreach(string side in new[]{"L","R"})
            {
                if(bones.TryGetValue("UpperLeg."+side,out var upper))
                {
                    float sign=Mathf.Sign(car.transform.InverseTransformPoint(upper.position).x);if(sign==0)sign=side=="L"?1:-1;
                    Vector3 foot=crank?crank.FootTarget(sign):car.transform.TransformPoint(new Vector3(sign*.25f,.43f,.05f));
                    Solve("UpperLeg."+side,"LowerLeg."+side,"Foot."+side,foot,car.transform.position+car.transform.forward+car.transform.right*sign*.35f+Vector3.up*.65f);
                }
                if(bones.TryGetValue("UpperArm."+side,out var arm))
                {
                    float sign=Mathf.Sign(car.transform.InverseTransformPoint(arm.position).x);if(sign==0)sign=side=="L"?1:-1;
                    Vector3 grip=car.transform.TransformPoint(new Vector3(sign*.3f,car.Kind==VehicleKind.Bicycle?1.18f:1.03f,.58f));
                    Solve("UpperArm."+side,"LowerArm."+side,"Fist."+side,grip,car.transform.position+car.transform.right*sign*.8f+Vector3.up*.7f);
                }
            }
        }
        void Solve(string rootName,string middleName,string endName,Vector3 target,Vector3 pole)
        {
            if(!bones.TryGetValue(rootName,out var root) || !bones.TryGetValue(middleName,out var middle) || !bones.TryGetValue(endName,out var end))return;
            Vector3 origin=root.position;float a=Vector3.Distance(origin,middle.position),b=Vector3.Distance(middle.position,end.position);
            if(a<.01f || b<.01f)return;
            Vector3 direction=target-origin;float distance=Mathf.Clamp(direction.magnitude,Mathf.Abs(a-b)+.001f,a+b-.001f);direction.Normalize();
            Vector3 bend=Vector3.ProjectOnPlane(pole-origin,direction).normalized;if(bend.sqrMagnitude<.01f)bend=car.transform.forward;
            float along=(a*a-b*b+distance*distance)/(2*distance);Vector3 joint=origin+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            root.rotation=Quaternion.FromToRotation(middle.position-origin,joint-origin)*root.rotation;
            middle.rotation=Quaternion.FromToRotation(end.position-middle.position,origin+direction*distance-middle.position)*middle.rotation;
            end.rotation=car.transform.rotation;
        }
        public void Refresh()
        {
            if(!car)car=GetComponentInParent<VehicleMotor>();if(!car)return;if(original==null)original=GetComponentsInChildren<Renderer>();foreach(var renderer in original)if(renderer)renderer.enabled=false;
            var game=GameRoot.Instance;int index=car.Driver?(game.CasualClothes?0:game.RoleIndex):-1;
            if(index!=role || index>=0 && (female!=game.FemaleChoices[game.RoleIndex] || outfit!=game.SkinChoices[game.RoleIndex]))Rebuild(index);if(human && index>=0)Fit();
        }
        void LateUpdate(){Refresh();}
        void OnDestroy(){if(human)Destroy(human.gameObject);}
    }
}
