using System;
using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        bool loadRecovered;string loadNotice="Сохранение недоступно.";
        public void Save()
        {
            try{var s=new SaveData{ShootingXP=ShootingXP,Difficulty=DifficultyIndex,DifficultyVersion=1,Courts=CourtOutcomes,Visa=VisaRemaining,Bank=BankBalance,Licenses=PersonalLicenses,OwnedGuns=OwnedGunMask,Purchases=Purchases,LoadedAmmo=SavedMagazines,ReserveAmmo=SavedReserves,Females=FemaleChoices,Skins=SkinChoices,Accessories=AccessoryChoices,Shields=ShieldChoices,Laws=StateLaws,Role=RoleIndex,Money=Money,Contraband=Contraband,Board=(int)SelectedBoard,Completed=Completed,X=SavePosition.x,Z=SavePosition.z,Health=Player.Health,Hour=Hour,Armor=Armor,Escaped=Escaped,CasualClothes=CasualClothes,Wanted=Wanted,Stories=StoryProgress,Choices=StoryChoices,Career=CareerXP};SaveStore.Write(SavePath,s);RuntimeSettings.Save();}
            catch(Exception e){Debug.LogWarning("Save failed: "+e.Message);Notify("Не удалось сохранить игру: "+e.Message);}
        }
        bool Load()
        {
            try{StopTraining(false);var s=SaveStore.Read(SavePath,out loadRecovered);RestoreDifficulty(s);ShootingXP=Mathf.Clamp(s.ShootingXP,0,1000);LoadOutfits(s.Skins,s.Accessories,s.Shields,s.Females);Contraband=Mathf.Clamp(s.Contraband,0,20);SelectedBoard=(PersonalBoard)Mathf.Clamp(s.Board,0,2);RoleIndex=Mathf.Clamp(s.Role,0,Roles.Length-1);CourtOutcomes=s.Courts??new int[10];Array.Resize(ref CourtOutcomes,10);visaUntil=Time.realtimeSinceStartup+Mathf.Clamp(s.Visa,0,1200);BankBalance=Mathf.Max(0,s.Bank);StateLaws=s.Laws&7;PersonalLicenses=s.Licenses&7;OwnedGunMask=s.OwnedGuns&71;Money=Mathf.Max(0,s.Money);Completed=Mathf.Max(0,s.Completed);Hour=s.Hour%24;CasualClothes=s.CasualClothes;EquipForRole();LoadShop(s.OwnedGuns,s.Purchases,s.LoadedAmmo,s.ReserveAmmo);Armor=Mathf.Clamp(s.Armor,0,MaxArmor);LoadCareer(s.Career);Escaped=s.Escaped;Wanted=Mathf.Clamp(s.Wanted,0,5);StoryProgress=s.Stories??new int[Roles.Length];StoryChoices=s.Choices??new int[Roles.Length];Array.Resize(ref StoryProgress,Roles.Length);Array.Resize(ref StoryChoices,Roles.Length);for(int i=0;i<Roles.Length;i++){StoryProgress[i]=Mathf.Clamp(StoryProgress[i],0,StoryBook.ChapterCount);StoryChoices[i]=Mathf.Clamp(StoryChoices[i],0,StoryBook.ChoiceMask);}Player.Health=Mathf.Clamp(s.Health,1,100);Player.Teleport(new Vector3(Mathf.Clamp(s.X,WorldLimits.MinX+10,WorldLimits.MaxX-10),2,Mathf.Clamp(s.Z,WorldLimits.MinZ+10,WorldLimits.MaxZ-10)));return true;}
            catch(Exception e){Debug.LogWarning("Load failed: "+e.Message);loadNotice=e is UnsupportedSaveVersionException?e.Message:"Сохранение недоступно. Начата новая игра; исходные файлы оставлены на диске.";return false;}
        }
        void OnApplicationQuit(){if(Playing)Save();RuntimeSettings.Save();}
        void OnApplicationPause(bool paused){if(paused&&!IsTestRun){if(Playing)Save();RuntimeSettings.Save();}}
        void OnApplicationFocus(bool focused){if(!focused && !IsTestRun && Playing && Panel=="")Toggle("pause");}
    }
}
