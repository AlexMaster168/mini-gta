using System;
using System.Collections;
using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        IEnumerator CheckAutoAim(Action<bool> result)
        {
            SelectRole(1);Panel="";Wanted=0;Player.Teleport(new Vector3(3600,2,-1400));
            Camera camera=Camera.main;var follow=camera.GetComponent<FollowCamera>();follow.enabled=false;
            var enemy=HostileAgent.Spawn(this,Player.Position+new Vector3(3,0,22),"Auto aim check",false);
            var distant=HostileAgent.Spawn(this,Player.Position+Vector3.forward*250,"Long range aim check",false);distant.gameObject.SetActive(false);
            GameObject obstacle=null;
            yield return new WaitForFixedUpdate();
            try
            {
                camera.transform.position=Player.Position+new Vector3(0,1.3f,-7);camera.transform.LookAt(Player.Position+new Vector3(0,1.05f,25));
                autoAimTarget=null;WeaponIndex=4;ServiceSelected=false;
                Require(FindAutoTarget(camera,AutoAimRange)==enemy,"Auto aim must acquire an off-centre enemy");
                Vector2 marker=AimScreenPoint(camera,enemy,1280,720);Vector3 projected=camera.WorldToViewportPoint(AimPoint(enemy));
                Require(marker.x>660 && Vector2.Distance(marker,new Vector2(projected.x*1280,(1-projected.y)*720))<.01f,"Reticle must project onto enemy instead of screen centre");
                enemy.transform.position+=Vector3.right*2;Physics.SyncTransforms();Require(FindAutoTarget(camera,AutoAimRange)==enemy && AimScreenPoint(camera,enemy,1280,720).x>marker.x+10,"Reticle must follow moving target");
                nextShot=0;int ammunition=magazines[4];Fire();Require(enemy.Health<100 && magazines[4]==ammunition-1,"Shot must hit the same off-centre target as reticle");
                Vector3 eye=camera.transform.position,point=AimPoint(enemy);
                obstacle=WorldBuilder.Box("Aim occlusion wall",(eye+point)*.5f,new Vector3(4,4,1),Color.gray);Physics.SyncTransforms();
                Require(FindAutoTarget(camera,AutoAimRange)==null,"Auto aim must release enemy behind wall");obstacle.SetActive(false);Destroy(obstacle);obstacle=null;
                Vector3 muzzle=Player.transform.position+Vector3.up*1.3f;
                obstacle=WorldBuilder.Box("Muzzle obstruction",muzzle+(point-muzzle).normalized*1.2f,new Vector3(.5f,1,.5f),Color.gray);Physics.SyncTransforms();
                Require(ClearAimLine(eye,point,enemy) && !ClearAimLine(muzzle,point,enemy) && FindAutoTarget(camera,AutoAimRange)==null,"Camera visibility alone must not allow aiming through cover beside player");obstacle.SetActive(false);Destroy(obstacle);obstacle=null;
                Require(FindAutoTarget(camera,10)==null,"Auto aim must respect weapon range");
                enemy.Stun(8);Require(Mathf.Abs(AimPoint(enemy).y-enemy.transform.position.y-.35f)<.001f,"Prone target must be aimed at body on ground");
                enemy.TakeDamage(1000);Require(FindAutoTarget(camera,AutoAimRange)==null,"Dead target must lose lock immediately");
                distant.gameObject.SetActive(true);Physics.SyncTransforms();WeaponIndex=10;Require(FindAutoTarget(camera,AutoAimRange)==distant,"Sniper HUD and shot must share extended lock range");WeaponIndex=4;Require(FindAutoTarget(camera,AutoAimRange)==null,"Rifle must not inherit sniper lock range");
                Debug.Log("SUNSET_AUTO_AIM_PROJECTION_TRACKING_HITS_OCCLUSION_RANGE_OK");
            }
            catch(Exception exception){Debug.LogError("SUNSET_SMOKE_FAILED "+exception);result(false);}
            finally
            {
                if(obstacle)Destroy(obstacle);Enemies.Remove(enemy);Enemies.Remove(distant);Destroy(enemy.gameObject);Destroy(distant.gameObject);autoAimTarget=null;follow.enabled=true;SelectRole(0);
            }
        }
    }
}
