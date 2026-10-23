using UnityEngine;
namespace SunsetState
{
    public sealed class WorldTextFacing:MonoBehaviour
    {
        public static Quaternion ReadableRotation(Vector3 text,Vector3 viewer)
        {Vector3 away=text-viewer;away.y=0;return away.sqrMagnitude>.001f?Quaternion.LookRotation(away,Vector3.up):Quaternion.identity;}
        void LateUpdate(){if(Camera.main)transform.rotation=ReadableRotation(transform.position,Camera.main.transform.position);}
    }
}
