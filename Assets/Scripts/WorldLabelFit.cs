using UnityEngine;
namespace SunsetState
{
    // Measure in mesh-local space. World AABBs collapse when a label turns sideways.
    public sealed class WorldLabelFit:MonoBehaviour
    {
        public float Width=3,Height=.65f;Renderer surface;
        public static WorldLabelFit Attach(TextMesh text,float width,float height)
        {var fit=text.GetComponent<WorldLabelFit>();if(!fit)fit=text.gameObject.AddComponent<WorldLabelFit>();fit.Width=width;fit.Height=height;fit.Fit();return fit;}
        public void Fit()
        {
            if(!surface)surface=GetComponent<Renderer>();if(!surface)return;
            Vector3 size=surface.localBounds.size,scale=transform.parent?transform.parent.lossyScale:Vector3.one;
            if(size.x<.0001f||size.y<.0001f){transform.localScale=Vector3.one*.1f;return;}
            float factor=Mathf.Min(Width/(size.x*Mathf.Max(.0001f,Mathf.Abs(scale.x))),Height/(size.y*Mathf.Max(.0001f,Mathf.Abs(scale.y))));
            transform.localScale=Vector3.one*Mathf.Clamp(factor,.0001f,10);
        }
        void LateUpdate(){Fit();}
    }
}
