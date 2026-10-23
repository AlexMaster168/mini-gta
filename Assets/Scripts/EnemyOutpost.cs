using System.Collections.Generic;
using UnityEngine;
namespace SunsetState
{
    public sealed class EnemyOutpost:MonoBehaviour
    {
        readonly List<HostileAgent> guards=new List<HostileAgent>();public int GuardCount=8;float nextWave,warnAt;
        public static void Build(GameRoot game,Transform parent)
        {
            Vector3[] centers={StateBuilder.NorthDistrict+new Vector3(600,0,350),StateBuilder.EastPort+new Vector3(-500,0,500),new Vector3(-3200,1,-1700)};
            for(int c=0;c<centers.Length;c++)
            {
                var root=new GameObject("Вооружённый лагерь "+(c+1));root.transform.SetParent(parent,false);root.transform.position=centers[c];root.AddComponent<EnemyOutpost>().GuardCount=8+c*2;
                WorldBuilder.Box("City building",centers[c]+new Vector3(0,3,30),new Vector3(20,6,15),new Color(.35f,.3f,.23f),parent);
                for(int i=0;i<6;i++)WorldBuilder.Box("Outpost cover",centers[c]+new Vector3((i%3-1)*14,1,(i/3)*18),new Vector3(4,2,2),new Color(.3f,.3f,.25f),parent);
                InteriorSystem.Label(centers[c]+new Vector3(0,5,-15),"ОПАСНАЯ ЗОНА · ВООРУЖЁННЫЙ ЛАГЕРЬ",parent,.07f);
            }
        }
        void Update()
        {
            var game=GameRoot.Instance;if(!game || !game.Playing || game.Panel!="")return;
            bool alive=false;foreach(var guard in guards)if(guard && !guard.Neutralized){alive=true;break;}
            if(!alive && Time.time>=nextWave){foreach(var old in guards)if(old){game.Enemies.Remove(old);Destroy(old.gameObject);}guards.Clear();for(int i=0;i<GuardCount;i++){Vector3 spot=transform.position+new Vector3((i%4-1.5f)*8,0,8+(i/4)*11);if(SpawnSafety.TryPlayerPosition(spot,out var safe))spot=safe;var guard=HostileAgent.Spawn(game,spot,"Вооружённый бандит",false);guard.AttackDamage=5+i%4;guards.Add(guard);}nextWave=Time.time+150;}
            if(Vector3.Distance(game.Player.Position,transform.position)<130 && Time.time>warnAt){warnAt=Time.time+60;game.Notify("Впереди вооружённый лагерь. I — оружие и броня, Q — аптечка.");}
        }
    }
}
