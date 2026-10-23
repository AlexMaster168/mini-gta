using UnityEngine;
namespace SunsetState
{
    public static class SpawnSafety
    {
        public static bool ClearPlayer(Vector3 foot,Transform own=null)
        {
            foreach(var hit in Physics.OverlapCapsule(foot+Vector3.up*.38f,foot+Vector3.up*1.45f,.3f,~(1<<2),QueryTriggerInteraction.Ignore))
                if(!own || !hit.transform.IsChildOf(own))return false;
            return true;
        }
        public static bool Ground(Vector3 requested,float rise,float drop,out RaycastHit ground,Transform own=null)
        {
            ground=default;float best=float.NegativeInfinity;
            foreach(var hit in Physics.RaycastAll(requested+Vector3.up*(rise+.1f),Vector3.down,rise+drop+.1f,~(1<<2),QueryTriggerInteraction.Ignore))
            {
                if(hit.normal.y<.65f || hit.point.y>requested.y+rise || hit.collider.attachedRigidbody || hit.collider is CharacterController || hit.collider.GetComponentInParent<HostileAgent>() || hit.collider.GetComponentInParent<Pedestrian>() || hit.collider.GetComponentInParent<BaseNPC>() || own && hit.transform.IsChildOf(own))continue;
                if(hit.point.y>best){best=hit.point.y;ground=hit;}
            }
            return best>float.NegativeInfinity;
        }
        static bool Candidate(Vector3 requested,out Vector3 result,Transform own)
        {
            result=requested;
            if(!Ground(requested,.5f,6,out var floor,own))return false;
            result=floor.point+Vector3.up*.06f;return ClearPlayer(result,own);
        }
        public static bool TryPlayerPosition(Vector3 requested,out Vector3 result,Transform own=null)
        {
            if(Candidate(requested,out result,own))return true;
            for(float radius=1;radius<=40;radius+=2)for(int i=0;i<16;i++){float angle=i*Mathf.PI/8;if(Candidate(requested+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius),out result,own))return true;}
            result=requested;return false;
        }
        public static bool VehicleSpace(Vector3 position,Vector3 dimensions,Quaternion rotation,VehicleMotor own=null)
        {
            var overlaps=Physics.OverlapBox(position+Vector3.up*Mathf.Max(.8f,dimensions.y*.55f),new Vector3(dimensions.x*.5f+.3f,Mathf.Max(.5f,dimensions.y*.48f),dimensions.z*.5f+.5f),rotation,~(1<<2),QueryTriggerInteraction.Ignore);
            foreach(var hit in overlaps){if(own && hit.transform.IsChildOf(own.transform))continue;if(hit.bounds.max.y<position.y+.15f)continue;return false;}return true;
        }
        static bool VehicleCandidate(Vector3 requested,Vector3 size,Quaternion rotation,VehicleMotor own,out Vector3 position)
        {
            position=requested;
            if(!Ground(requested,.5f,6,out var floor,own?own.transform:null))return false;
            position=floor.point+Vector3.up*.18f;
            // Support all four corners: do not spawn across a ledge or through a raised platform.
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
            {
                Vector3 corner=position+rotation*new Vector3(x*size.x*.4f,0,z*size.z*.38f);
                if(!Ground(corner,.45f,1,out var support,own?own.transform:null) || Mathf.Abs(support.point.y-floor.point.y)>.45f)return false;
            }
            return VehicleSpace(position,size,rotation,own);
        }
        public static bool TryVehiclePosition(Vector3 requested,Vector3 size,float yaw,out Vector3 position,VehicleMotor own=null)
        {
            Physics.SyncTransforms();var rotation=Quaternion.Euler(0,yaw,0);
            if(VehicleCandidate(requested,size,rotation,own,out position))return true;
            for(float radius=8;radius<=160;radius+=8)for(int i=0;i<16;i++){float angle=i*Mathf.PI/8;Vector3 candidate=requested+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);if(VehicleCandidate(candidate,size,rotation,own,out position))return true;}
            position=requested;return false;
        }
        public static Vector3 VehiclePosition(Vector3 requested,Vector3 size,float yaw,VehicleMotor own=null)
        {
            if(TryVehiclePosition(requested,size,yaw,out var safe,own))return safe;
            // A known open road is preferable to returning a blocked spawn point.
            if(TryVehiclePosition(new Vector3(0,2,0),size,yaw,out safe,own))return safe;
            throw new System.InvalidOperationException("No safe ground position for vehicle");
        }
    }
}
