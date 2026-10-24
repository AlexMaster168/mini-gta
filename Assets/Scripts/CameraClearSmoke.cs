using UnityEngine;
namespace SunsetState
{
    public sealed class CameraClearSmoke:MonoBehaviour
    {
        ParticleSystem smoke;readonly ParticleSystem.Particle[] particles=new ParticleSystem.Particle[100];
        void Awake(){smoke=GetComponent<ParticleSystem>();GetComponent<ParticleSystemRenderer>().maxParticleSize=.025f;}
        public static float Visibility(Vector3 particle,Vector3 camera,Vector3 focus)
        {
            float fade=Mathf.InverseLerp(2,6,Vector3.Distance(particle,camera));Vector3 line=focus-camera;float t=Vector3.Dot(particle-camera,line)/Mathf.Max(.01f,line.sqrMagnitude);
            if(t>0&&t<1&&Vector3.Distance(particle,camera+line*t)<1.4f)fade*=.08f;return fade;
        }
        void LateUpdate()
        {
            var camera=Camera.main;var game=GameRoot.Instance;if(!camera||!game)return;int count=smoke.GetParticles(particles);
            for(int i=0;i<count;i++){var color=particles[i].startColor;color.a=(byte)(40*Visibility(particles[i].position,camera.transform.position,game.Player.Position+Vector3.up));particles[i].startColor=color;}smoke.SetParticles(particles,count);
        }
    }
}
