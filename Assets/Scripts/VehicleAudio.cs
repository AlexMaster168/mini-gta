using UnityEngine;

namespace SunsetState
{
    public sealed class VehicleAudio:MonoBehaviour
    {
        public static float EngineVolume=.12f;
        public static float MixedEngineVolume(bool driven,bool radioPlaying,float musicVolume)
        {float level=driven?EngineVolume:Mathf.Min(.035f,EngineVolume*.3f);return radioPlaying?Mathf.Min(level,Mathf.Max(0,musicVolume)*(driven?.25f:.15f)):level;}
        VehicleMotor car;AudioSource engine,emergency,horn;bool alarm;static AudioClip hornClip;
        public void Initialize(VehicleMotor vehicle)
        {
            car=vehicle;engine=Source();emergency=Source();horn=Source();
            engine.clip=Resources.Load<AudioClip>(car.Flying?"Audio/Rotor":"Audio/Engine");engine.loop=true;if(car.Kind==VehicleKind.Bicycle)engine.clip=null;if(car.Kind==VehicleKind.Electric)engine.clip=ElectricHum();
            emergency.clip=Resources.Load<AudioClip>("Audio/Siren");emergency.loop=true;
            if(!hornClip){float[] data=new float[11025];for(int i=0;i<data.Length;i++){float t=i/22050f;data[i]=(Mathf.Sin(t*440*6.283f)+Mathf.Sin(t*349*6.283f))*.2f*Mathf.Min(1,(data.Length-i)/900f);}hornClip=AudioClip.Create("Vehicle horn",data.Length,1,22050,false);hornClip.SetData(data,0);}
        }
        static AudioClip ElectricHum(){int rate=22050;float[] samples=new float[rate];for(int i=0;i<rate;i++){float t=i/(float)rate;samples[i]=(.08f*Mathf.Sin(t*220*Mathf.PI*2)+.025f*Mathf.Sin(t*440*Mathf.PI*2));}var clip=AudioClip.Create("Electric drive · synthesized",rate,1,rate,false);clip.SetData(samples,0);return clip;}
        AudioSource Source(){var audio=gameObject.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=1;audio.minDistance=7;audio.maxDistance=130;audio.rolloffMode=AudioRolloffMode.Linear;audio.dopplerLevel=.25f;return audio;}
        void Update()
        {
            var game=GameRoot.Instance;if(!game || !car)return;
            bool active=game.Playing && game.Panel=="";
            bool near=Vector3.Distance(game.Player.Position,transform.position)<100;
            bool running=active && near && (car.Driver || car.Traffic || GetComponent<HostileAgent>());
            if(running && engine.clip){if(!engine.isPlaying)engine.Play();engine.pitch=car.Flying?1:Mathf.Clamp(.7f+car.RPM/7500,.6f,1.8f);engine.volume=MixedEngineVolume(car.Driver,StateRadio.IsAudible,StateRadio.Volume);}else engine.Pause();
            bool sound=active && near && (car.EmergencyEnabled || alarm);
            if(sound && emergency.clip){if(!emergency.isPlaying)emergency.Play();emergency.volume=.35f;emergency.pitch=alarm?1.35f:1;}else emergency.Pause();
            if(active && car.Driver && Input.GetKeyDown(KeyCode.B))horn.PlayOneShot(hornClip,.5f);
            if(active && car.Driver && Input.GetKeyDown(KeyCode.N)){alarm=!alarm;game.Notify(alarm?"Сигнализация включена · N — выключить":"Сигнализация выключена");}
        }
    }
    public sealed class StateRadio:MonoBehaviour
    {
        public static StateRadio Instance;
        public static readonly string[] TrackNames={"Midnight Drive · congusbongus","Police Loop · obscure music","Doomed · Alexander Ehlers","Flags · Alexander Ehlers","Great Mission · Alexander Ehlers","Spacetime · Alexander Ehlers","Twists · Alexander Ehlers","Waking the Devil · Alexander Ehlers","Warped · Alexander Ehlers"};
        static readonly string[] TrackPaths={"Audio/MidnightDrive","Audio/PoliceLoop","Audio/Radio/Doomed","Audio/Radio/Flags","Audio/Radio/Greatmission","Audio/Radio/Spacetime","Audio/Radio/Twists","Audio/Radio/Wakingthedevil","Audio/Radio/Warped"};
        public int TrackCount=>tracks.Length;
        public AudioClip SelectedClip=>radio.clip;
        public void PhoneRadio(int index){phoneListening=true;if(index<0)enabledRadio=!enabledRadio;else{enabledRadio=true;SelectTrack(index);}}
        void SelectTrack(int index){station=Mathf.Clamp(index,0,tracks.Length-1);radio.Stop();radio.clip=tracks[station];started=false;paused=false;}
        public void NextTrack(){SelectTrack((station+1)%tracks.Length);}
        AudioSource radio;AudioClip[] tracks;int station;bool enabledRadio=true,phoneListening,started,paused;
        public static bool IsAudible=>Instance&&Instance.radio&&Instance.radio.isPlaying;
        public static string Current="";public static float Volume=.24f;
        void Start(){Instance=this;radio=gameObject.AddComponent<AudioSource>();radio.playOnAwake=false;radio.loop=false;tracks=new AudioClip[TrackPaths.Length];for(int i=0;i<tracks.Length;i++)tracks[i]=Resources.Load<AudioClip>(TrackPaths[i]);radio.clip=tracks[0];}
        void Update()
        {
            var game=GameRoot.Instance;if(!game || !radio)return;
            if(game.Playing && game.Panel=="")
            {
                if(Input.GetKeyDown(KeyCode.T)){phoneListening=true;enabledRadio=!enabledRadio;game.Notify(enabledRadio?"Sunset Radio включено · Y — следующий трек":"Sunset Radio выключено");}
                if(Input.GetKeyDown(KeyCode.Y)){NextTrack();game.Notify(TrackNames[station]);}
                if(Input.GetKeyDown(KeyCode.Minus))Volume=Mathf.Max(0,Volume-.04f);
                if(Input.GetKeyDown(KeyCode.Equals))Volume=Mathf.Min(.6f,Volume+.04f);
            }
            bool play=enabledRadio && (!game.Playing || game.Panel=="" && (game.Player.Vehicle||phoneListening));
            if(play && radio.clip){if(paused){radio.UnPause();paused=false;}if(started&&!radio.isPlaying)NextTrack();if(!started){radio.Play();started=true;}radio.volume=Volume;Current=TrackNames[station];}else{if(started&&!paused){radio.Pause();paused=true;}Current="";}
        }
    }
}
