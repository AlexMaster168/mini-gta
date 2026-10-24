using UnityEngine;
namespace SunsetState
{
    public sealed class MinigunVisual:MonoBehaviour
    {
        Transform barrels;
        public static void Create(Transform player)
        {
            var root=new GameObject("Minigun · rotating barrels");root.transform.SetParent(player,false);root.transform.localPosition=new Vector3(.38f,.95f,.48f);var visual=root.AddComponent<MinigunVisual>();
            var body=WorldBuilder.Box("Minigun receiver",Vector3.zero,new Vector3(.28f,.3f,.4f),new Color(.09f,.1f,.11f),root.transform,false);body.transform.localPosition=Vector3.zero;
            var ammo=WorldBuilder.Box("Minigun ammo box",Vector3.zero,new Vector3(.27f,.24f,.3f),new Color(.24f,.28f,.19f),root.transform,false);ammo.transform.localPosition=new Vector3(-.2f,-.12f,-.05f);
            visual.barrels=new GameObject("Six-barrel rotor").transform;visual.barrels.SetParent(root.transform,false);visual.barrels.localPosition=Vector3.forward*.48f;
            for(int i=0;i<6;i++){var barrel=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Destroy(barrel.GetComponent<Collider>());barrel.transform.SetParent(visual.barrels,false);float a=i*Mathf.PI/3;barrel.transform.localPosition=new Vector3(Mathf.Cos(a)*.095f,Mathf.Sin(a)*.095f,0);barrel.transform.localRotation=Quaternion.Euler(90,0,0);barrel.transform.localScale=new Vector3(.055f,.36f,.055f);barrel.GetComponent<Renderer>().sharedMaterial=WorldBuilder.Mat(new Color(.16f,.17f,.18f));}
        }
        void LateUpdate()
        {
            var game=GameRoot.Instance;if(!game)return;bool visible=game.Playing&&!game.Player.Vehicle&&!game.ServiceSelected&&game.WeaponIndex==GameRoot.MinigunIndex&&(game.RoleIndex==1||game.RoleIndex==2);
            foreach(var renderer in GetComponentsInChildren<Renderer>())renderer.enabled=visible;
            if(visible&&game.Panel==""&&Input.GetMouseButton(0))barrels.Rotate(0,0,1500*Time.deltaTime,Space.Self);
        }
    }
}
