using UnityEngine;
namespace SunsetState
{
    public sealed class NpcGroundMotor:MonoBehaviour
    {
        public CharacterController Controller {get;private set;}
        Vector3 desired,lastSafe;float vertical,embedded;bool safeKnown;
        public void Initialize()
        {
            Controller=GetComponent<CharacterController>();
            if(!Controller){var previous=GetComponent<CapsuleCollider>();if(previous){previous.enabled=false;Destroy(previous);}Controller=gameObject.AddComponent<CharacterController>();}
            Controller.height=1.8f;Controller.radius=.3f;Controller.center=Vector3.up*.9f;Controller.stepOffset=.32f;Controller.skinWidth=.035f;Controller.slopeLimit=46;
            SnapToGround();
        }
        void Start(){if(!Controller)Initialize();}
        public bool SnapToGround()
        {
            if(!Controller)return false;Physics.SyncTransforms();Vector3 requested=transform.position;if(requested.y<0)requested.y=2;
            if(!SpawnSafety.TryPlayerPosition(requested,out var safe,transform))return false;
            bool enabled=Controller.enabled;Controller.enabled=false;transform.position=safe;Controller.enabled=enabled;vertical=0;lastSafe=safe;safeKnown=true;return true;
        }
        public void Move(Vector3 velocity){desired=Vector3.ProjectOnPlane(velocity,Vector3.up);}
        void LateUpdate()
        {
            var game=GameRoot.Instance;if(!game || !game.Playing || game.Panel!="" || !Controller || !Controller.enabled){desired=Vector3.zero;return;}
            if(Controller.isGrounded)vertical=-2;vertical=Mathf.Max(-30,vertical+Physics.gravity.y*Time.deltaTime);
            Controller.Move((desired+Vector3.up*vertical)*Time.deltaTime);desired=Vector3.zero;
            embedded=SpawnSafety.ClearPlayer(transform.position,transform)?0:embedded+Time.deltaTime;
            if(embedded>.4f || transform.position.y<-.5f)
            {if(!SnapToGround() && safeKnown){Controller.enabled=false;transform.position=lastSafe;Controller.enabled=true;vertical=0;}embedded=0;}
            else if(Controller.isGrounded){lastSafe=transform.position;safeKnown=true;}
        }
    }
}
