using UnityEngine;
namespace SunsetState
{
    public sealed class NeutralActor:MonoBehaviour
    {
        public float Health=100;
        public void TakeDamage(float amount){if(Health<=0)return;Health=Mathf.Max(0,Health-amount);CivilianKnockdown.Fall(gameObject,Health<=0);}
    }
    public static class NpcEffects
    {
        public static GameObject Actor(Collider collider)
        {
            var enemy=collider.GetComponentInParent<HostileAgent>();if(enemy && !enemy.VehicleUnit)return enemy.gameObject;
            var pedestrian=collider.GetComponentInParent<Pedestrian>();if(pedestrian)return pedestrian.gameObject;
            var staff=collider.GetComponentInParent<BaseNPC>();if(staff)return staff.gameObject;
            var contact=collider.GetComponentInParent<NeutralActor>();return contact?contact.gameObject:null;
        }
        public static bool Alive(GameObject actor)
        {
            if(!actor || !actor.activeInHierarchy)return false;
            var enemy=actor.GetComponent<HostileAgent>();if(enemy)return !enemy.Neutralized && !enemy.VehicleUnit;
            var pedestrian=actor.GetComponent<Pedestrian>();if(pedestrian)return !pedestrian.Neutralized;
            var staff=actor.GetComponent<BaseNPC>();if(staff)return !staff.Neutralized;
            var contact=actor.GetComponent<NeutralActor>();return contact && contact.Health>0;
        }
        public static bool Stun(GameObject actor,float seconds)
        {
            if(!Alive(actor))return false;var enemy=actor.GetComponent<HostileAgent>();
            if(enemy)enemy.Stun(seconds);else CivilianKnockdown.Fall(actor,false,seconds);return true;
        }
    }
}
