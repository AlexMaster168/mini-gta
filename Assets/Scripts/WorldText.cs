using UnityEngine;
namespace SunsetState
{
    public static class WorldText
    {
        static Material material;static Font font;
        public static Material ForFont(Font selected)
        {
            font=selected;if(!material){var basis=Resources.Load<Material>("WorldTextBase");material=basis?new Material(basis):new Material(Shader.Find("Sunset/WorldText"));Font.textureRebuilt+=Refresh;}
            material.mainTexture=font.material.mainTexture;return material;
        }
        static void Refresh(Font rebuilt){if(rebuilt==font&&material)material.mainTexture=font.material.mainTexture;}
    }
}
