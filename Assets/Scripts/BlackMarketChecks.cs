using System;
using System.Collections;
using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        IEnumerator CheckBlackMarket(Action<bool> result)
        {
            try
            {
                SelectRole(14);Player.Teleport(BlackMarket.Points[0]+Vector3.back*2);Panel="";Time.timeScale=1;Money=100;
                Wanted=4;nextResponse=0;UpdatePursuit();UpdateWantedResponse();Require(pursuer&&ResponseOfficers>0&&responseCars.Count>0,"Wanted check must create actual pursuit units");
                Require(!BuyWantedClearance()&&Wanted==4&&Money==100,"Insufficient payment must preserve wanted level and cash");
                Money=10000;int wallet=Money;var mission=MakeMission(0);ActiveMission=mission;
                Require(BuyWantedClearance()&&Wanted==0&&Money==wallet-2000,"Gurram must charge exactly once per wanted star and clear all stars");
                Require(!pursuer&&ResponseOfficers==0&&responseCars.Count==0&&arrest==0&&footArrest==0&&ActiveMission==mission,"Clearance must remove pursuit and arrest progress without cancelling mission");
                wallet=Money;Require(!BuyWantedClearance()&&Money==wallet,"Clearance at zero stars must not charge again");
                Save();Wanted=5;Load();Require(Wanted==0,"Cleared wanted status must survive loading");CancelMission();
                for(int level=1;level<=5;level++){Wanted=level;Money=10000;Require(BuyWantedClearance()&&Money==10000-level*500&&Wanted==0,"Clearance price must cover every wanted level");}
                Player.Teleport(Vector3.zero);Wanted=3;Money=10000;Require(!BuyWantedClearance()&&Wanted==3&&Money==10000,"Clearance must require visiting vendor in person");
                Player.Teleport(BlackMarket.Points[0]+Vector3.back*2);Wanted=0;Wanted=1;nextResponse=0;UpdatePursuit();UpdateWantedResponse();Require(pursuer&&ResponseOfficers>0,"New crime must allow police pursuit again after clearance");
                Require(BuyWantedClearance(),"Cleanup clearance must succeed");SelectRole(0);Debug.Log("SUNSET_BLACK_MARKET_PRICE_PROXIMITY_PURSUIT_RESET_SAVE_NEW_CRIME_OK");
            }
            catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);result(false);}
            yield return null;
        }
    }
}
