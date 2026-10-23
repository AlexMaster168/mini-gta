using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        void InventoryChecks()
        {
            LoadOutfits(null,null,null);
            for(int role=0;role<Roles.Length;role++)
            {
                SelectRole(role);for(int skin=0;skin<3;skin++){SkinChoices[role]=skin;Player.SetColor(Roles[role].Color);Require(Player.Visual.GetComponentInChildren<SkinnedMeshRenderer>() && !CasualClothes,"Role skin must keep human and work uniform: "+role);}
                var titles=new System.Collections.Generic.HashSet<string>();for(int chapter=0;chapter<StoryBook.ChapterCount;chapter++){Require(titles.Add(StoryBook.Stories[role].Title(chapter)),"Story chapters need distinct titles: "+role);foreach(var point in MakeStoryMission(role,chapter,0).Points)Require(WorldLimits.Contains(point),"Story route outside map");}
                Require(titles.Count==StoryBook.ChapterCount,"Every role needs fifty story chapters");
            }
            SelectRole(4);EquipAccessory(4);Player.Health=40;personalMedkitReadyAt=0;UsePersonalMedkit();Require(Player.Health==80,"Medical bag must increase medkit healing to forty");EquipAccessory(7);Require(AccessoryAt(0)!=7,"Medic cannot equip criminal or tactical mask");
            SelectRole(14);EquipAccessory(7);Require(AccessoryAt(0)==7 && Mathf.Approximately(AccessoryProtection,.03f),"Bandit must have balaclava with protection bonus");EquipAccessory(6);Require(CJRun && Mathf.Approximately(RunMultiplier,1.2f),"CJ style set must increase sprint");
            SelectRole(2);EquipAccessory(5);EquipAccessory(7);EquipAccessory(6);ShieldChoices[2]=1;Player.SetColor(Roles[2].Color);Require(MaxArmor==175 && ShieldEquipped,"FBI loadout must support armor plate and shield");
            Vector3 front=Player.Position+Player.transform.forward*10,back=Player.Position-Player.transform.forward*10;Require(Mathf.Approximately(ShieldDamageScale(front,true),.2f) && ShieldDamageScale(back,true)==1 && ShieldDamageScale(front,false)==1,"Shield must protect only front while blocking");
            StoryProgress[2]=19;StoryChoices[2]=127;Save();LoadOutfits(null,null,null);StoryProgress[2]=0;Load();Require(ShieldEquipped && CJRun && AccessoryAt(0)==7 && SkinChoices[2]==2 && StoryProgress[2]==19 && StoryChoices[2]==127,"Inventory, skins and extended story choices must save");
            SelectRole(4);Require(!ShieldEquipped,"Shield cannot transfer to incompatible role");
            LoadOutfits(null,null,null);SelectRole(0);Debug.Log("SUNSET_ROLE_SKINS_ACCESSORY_STATS_SHIELD_20_CHAPTERS_OK");
        }
    }
}
