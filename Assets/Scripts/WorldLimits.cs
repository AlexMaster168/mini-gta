using UnityEngine;
namespace SunsetState
{
    public static class WorldLimits
    {
        public const float MinX=-5500,MaxX=5500,MinZ=-2500,MaxZ=7000,Width=11000,Depth=9500;
        public static bool Contains(Vector3 p)=>p.x>=MinX && p.x<=MaxX && p.z>=MinZ && p.z<=MaxZ;
    }
}
