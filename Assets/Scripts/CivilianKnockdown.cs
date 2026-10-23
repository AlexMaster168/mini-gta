using UnityEngine;
namespace SunsetState
{
    public sealed class CivilianKnockdown:MonoBehaviour
    {
        Transform visual;NpcGroundMotor motor;Vector3 position;Quaternion rotation;CapsuleCollider prone;public float Until;bool dead;Collider[] standing;bool[] enabledBefore;
        public static void Fall(GameObject actor,bool killed,float seconds=3.5f)
        {
            var fall=actor.GetComponent<CivilianKnockdown>();if(!fall)
            {
                fall=actor.AddComponent<CivilianKnockdown>();fall.motor=actor.GetComponent<NpcGroundMotor>();fall.visual=actor.transform.Find("Character");
                if(fall.visual){fall.position=fall.visual.localPosition;fall.rotation=fall.visual.localRotation;CharacterModels.FreezePose(fall.visual);fall.visual.localRotation=Quaternion.Euler(90,0,0);fall.visual.localPosition=new Vector3(0,.35f,-.9f);}
                fall.standing=actor.GetComponents<Collider>();fall.enabledBefore=new bool[fall.standing.Length];for(int i=0;i<fall.standing.Length;i++){fall.enabledBefore[i]=fall.standing[i].enabled;fall.standing[i].enabled=false;}
                if(fall.motor){fall.motor.Controller.enabled=false;fall.motor.enabled=false;}
                fall.prone=actor.AddComponent<CapsuleCollider>();fall.prone.direction=2;fall.prone.center=Vector3.up*.35f;fall.prone.radius=.3f;fall.prone.height=1.7f;
            }
            fall.dead|=killed;fall.Until=Mathf.Max(fall.Until,Time.time+seconds);
        }
        void Update()
        {
            if(dead || Time.time<Until)return;prone.enabled=false;Destroy(prone);
            if(visual){visual.localPosition=position;visual.localRotation=rotation;var movement=visual.GetComponent<CharacterMotion>();if(movement)movement.enabled=true;var animation=visual.GetComponentInChildren<Animation>();if(animation)animation.enabled=true;}
            for(int i=0;i<standing.Length;i++)if(standing[i])standing[i].enabled=enabledBefore[i];
            if(motor){motor.enabled=true;motor.Controller.enabled=true;motor.SnapToGround();}Destroy(this);
        }
    }
}
