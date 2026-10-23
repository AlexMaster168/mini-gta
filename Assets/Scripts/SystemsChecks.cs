using System;
using System.Collections;
using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        IEnumerator CheckNewSystems(Action<bool> result)
        {
            try
            {
                SelectRole(15);StoryProgress[15]=4;BeginStory(0);Player.Teleport(ActiveMission.Points[0]);Require(LawyerStageBlocked(true,true)&&Panel=="court","Trial must interrupt mission for an actual defence decision");int wallet=Money;ResolveCourt(0);Require(courtResolved&&CourtOutcomes[0]==1&&Money==wallet+900&&verdict.Contains("оправдание"),"Supported defence must acquit client");ResolveCourt(0);Require(Money==wallet+900,"Verdict must not pay twice");CancelMission();
                SelectRole(16);ActivateMission(MakeMafiaMission(0,0));Require(hostage&&mafiaHostage&&HostageStageBlocked,"Mafia mission must spawn an actual captive objective");ClearHostage();CancelMission();
                SelectRole(1);if(Call!=null)EndCall(false);StartOperation(2);Require(Call==null,"Hostage assault is reserved for FBI");StartOperation(0);Require(Call!=null&&Call.Threats.Count==3&&Call.Code=="10-32","Police robbery dispatch must spawn robbers");EndCall(false);
                SelectRole(2);StartOperation(2);Require(hostage&&!mafiaHostage&&Call.Code=="10-91","FBI rescue must contain actual hostage and guards");EndCall(false);StartOperation(3);Require(Call.Code=="10-82"&&operationObjects.Count==1,"Bomb response must contain interactive objective");EndCall(false);StartOperation(1);Require(Call.Code=="10-60"&&!operationEvidence,"Drug raid requires seizure in addition to enemies");EndCall(false);
                SkinChoices[2]=2;Player.SetColor(Roles[2].Color);Require(Array.Exists(Player.Visual.GetComponentsInChildren<Transform>(),x=>x.name=="SWAT ballistic helmet"),"SWAT must have a visible helmet and loadout");
                SelectRole(17);StateLaws=3;Require(MissionPay(100)>=110,"Employment law must affect actual mission reward");StartGovernorKidnap();Require(governorCaptured&&kidnapGuards.Count==3&&captiveGate,"Governor event must include confinement and hostile guards");ClearGovernorRescue();
                SelectRole(0);Money=5000;Wanted=0;Require(BuyVisa()&&Money==4500&&VisaRemaining>1190,"Visa purchase must charge money and grant timed stay");FlyVice(true);Require(OnVice&&RoleIndex==0,"Flight must preserve role and change region");SelectRole(2);var work=MakeMission(0);foreach(var point in work.Points)Require(ViceCityIsland.Contains(point),"Island job must stay on island");BankBalance=1250;StateLaws=5;Save();BankBalance=0;StateLaws=0;visaUntil=0;Load();Require(CourtOutcomes[0]==1&&BankBalance==1250&&StateLaws==5&&VisaRemaining>1100&&OnVice,"Bank, laws, visa and region must persist");FlyVice(false);Require(!OnVice,"Return flight must return to mainland");
                PhoneMessage("Проверка","Адрес задания");Require(phoneMessages.Count>0,"Phone inbox must accept mission messages");gpsDestination=ViceCityIsland.Airport;Require(MapGoal.HasValue&&MapGoal.Value==ViceCityIsland.Airport,"Custom GPS must route to selected destination");gpsDestination=null;
                SelectRole(16);EnterBuilding(StateBank.Entrance);StartBankHeist();Require(bankHeist!=null&&ActiveMission==bankHeist&&Wanted==5&&BankStageBlocked,"Bank robbery requires actual guarded vault and wanted response");CancelMission();ExitBuilding();
                SelectRole(2);var car=WorldBuilder.SpawnCar(this,new Vector3(0,2,200),"Custody test",Color.black,false,0,VehicleKind.SUV);car.Faction=2;var suspect=HostileAgent.Spawn(this,new Vector3(4,1,200),"Custody test suspect",false);suspect.Arrest();transportSuspect=suspect.gameObject;Player.Enter(car);UpdateTransport();Require(custodyVisual&&custodyVisual.parent==car.transform&&custodyVisual.gameObject.activeInHierarchy,"Cuffed prisoner must remain visible in rear seat");Player.Teleport(new Vector3(0,2,180));UpdateTransport();UpdateTransport();Require(custodyVisual&&custodyVisual.parent==null,"Prisoner must leave vehicle safely with escort");ClearCustody();Cars.Remove(car);Destroy(car.gameObject);
                SelectRole(0);StateLaws=0;BankBalance=0;StoryProgress[15]=0;LoadOutfits(null,null,null);Debug.Log("SUNSET_COURT_MAFIA_OPERATIONS_SWAT_GOVERNOR_VISA_BANK_PHONE_CUSTODY_OK");
            }
            catch(Exception exception){Debug.LogError("SUNSET_SMOKE_FAILED "+exception);result(false);}
            yield return null;
        }
    }
}
