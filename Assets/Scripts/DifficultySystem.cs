using System;
using UnityEngine;
namespace SunsetState
{
    public sealed class DifficultyProfile
    {
        public readonly string Name;
        public readonly float Damage,HitChance,ShotInterval,Reaction,ResponseInterval;
        public DifficultyProfile(string name,float damage,float hit,float shots,float reaction,float response)
        {Name=name;Damage=damage;HitChance=hit;ShotInterval=shots;Reaction=reaction;ResponseInterval=response;}
    }
    public sealed partial class GameRoot
    {
        public static readonly DifficultyProfile[] Difficulties={
            new DifficultyProfile("Легко",.55f,.45f,1.4f,1.5f,12),
            new DifficultyProfile("Нормально",1,.7f,1,1,8),
            new DifficultyProfile("Сложно",1.4f,.82f,.8f,.75f,6),
            new DifficultyProfile("Кошмар",1.9f,.94f,.6f,.5f,4)};
        public int DifficultyIndex{get;private set;}=1;
        public DifficultyProfile Difficulty=>Difficulties[DifficultyIndex];
        void RestoreDifficulty(SaveData data){DifficultyIndex=data.DifficultyVersion>0?Mathf.Clamp(data.Difficulty,0,3):1;}
        void SetDifficulty(int index)
        {
            DifficultyIndex=Mathf.Clamp(index,0,3);
            if(Playing){nextResponse=Mathf.Min(nextResponse,Time.time+Difficulty.ResponseInterval);Save();Notify("Сложность: "+Difficulty.Name);}
        }
        public void ApplyEnemyHit(float damage,Vector3 source){DamagePlayer(damage*Difficulty.Damage,source);}
        void DrawDifficultyChoices(float left,float top,float width)
        {
            Label(left,top,width,35,Playing?"СЛОЖНОСТЬ ИГРЫ":"СЛОЖНОСТЬ НОВОЙ ИГРЫ",heading);
            for(int i=0;i<Difficulties.Length;i++)
            {
                var profile=Difficulties[i];float y=top+48+i*86;
                if(Button(new Rect(left,y,width,40),(DifficultyIndex==i?"✓  ":"")+profile.Name.ToUpper()))SetDifficulty(i);
                Label(left+8,y+44,width-16,32,"Урон врагов ×"+profile.Damage.ToString("0.##")+" · точность "+Mathf.RoundToInt(profile.HitChance*100)+"% · полиция: "+profile.ResponseInterval+" с",small);
            }
        }
        void CheckDifficulty()
        {
            int original=DifficultyIndex;SelectRole(0);Player.Teleport(new Vector3(3100,2,-1800));Array.Clear(AccessoryChoices,0,AccessoryChoices.Length);
            float previousDamage=0;
            for(int i=0;i<4;i++)
            {
                SetDifficulty(i);Armor=0;Player.Health=100;ApplyEnemyHit(10,Player.Position+Vector3.forward*5);
                float damage=100-Player.Health;Require(Mathf.Approximately(damage,10*Difficulty.Damage)&&damage>previousDamage,"Each difficulty must apply increasing enemy damage");previousDamage=damage;
                Save();DifficultyIndex=(i+1)%4;Load();Require(DifficultyIndex==i,"Difficulty must survive saving and loading");
                if(i>0)Require(Difficulties[i].HitChance>Difficulties[i-1].HitChance&&Difficulties[i].ShotInterval<Difficulties[i-1].ShotInterval&&Difficulties[i].Reaction<Difficulties[i-1].Reaction&&Difficulties[i].ResponseInterval<Difficulties[i-1].ResponseInterval,"Enemy accuracy, reaction and police response must escalate");
            }
            RestoreDifficulty(JsonUtility.FromJson<SaveData>("{}"));Require(DifficultyIndex==1,"Legacy saves must keep normal difficulty");
            RestoreDifficulty(new SaveData{DifficultyVersion=1,Difficulty=99});Require(DifficultyIndex==3,"Invalid difficulty must be clamped");
            SetDifficulty(original);Player.Health=100;SelectRole(0);Debug.Log("SUNSET_FOUR_DIFFICULTIES_DAMAGE_BALANCE_SAVE_LEGACY_OK");
        }
        void CheckRadioLibrary()
        {
            var radio=StateRadio.Instance;Require(radio&&radio.TrackCount==9,"Radio must contain nine tracks");
            var unique=new System.Collections.Generic.HashSet<AudioClip>();
            for(int i=0;i<radio.TrackCount;i++){radio.PhoneRadio(i);var clip=radio.SelectedClip;Require(clip&&clip.length>10&&clip.channels>0&&clip.frequency>=22050&&unique.Add(clip),"Every radio choice must select a different valid full track: "+i);}
            radio.NextTrack();var first=radio.SelectedClip;radio.PhoneRadio(0);Require(radio.SelectedClip==first,"Playlist must wrap to its first track");
            radio.PhoneRadio(999);var last=radio.SelectedClip;radio.PhoneRadio(8);Require(last==radio.SelectedClip,"Track selection must clamp invalid indices");radio.PhoneRadio(0);
            float originalEngine=VehicleAudio.EngineVolume;VehicleAudio.EngineVolume=.3f;
            foreach(float volume in new[]{0f,.02f,.24f,.6f})foreach(bool driven in new[]{false,true})Require(VehicleAudio.MixedEngineVolume(driven,true,volume)<=volume*.25f+.00001f,"Music must cap engine gain even with maximum engine setting");
            Require(Mathf.Approximately(VehicleAudio.MixedEngineVolume(true,false,.24f),.3f),"Radio off must restore chosen engine gain");VehicleAudio.EngineVolume=originalEngine;
            Debug.Log("SUNSET_NINE_RADIO_TRACKS_VALID_SELECTION_PLAYLIST_WRAP_ENGINE_DUCKING_OK");
        }
    }
}
