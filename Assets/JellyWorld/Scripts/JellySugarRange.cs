using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace JellyWorldGame
{
    public class JellySugarRange : MonoBehaviour
    {
        public bool Active {get;private set;}
        public bool Transitioning {get;private set;}
        public int Ammo {get;private set;}
        public int Hits {get;private set;}
        public int Remaining {get;private set;}
        public Scene RangeScene {get{return rangeScene;}}
        public JellyRangeTarget[,] Targets=new JellyRangeTarget[8,8];
        JellyWorld world;
        Camera cam;
        Transform boardStage,decor,root,fx,launcher,muzzle;
        Scene mainScene,rangeScene;
        Vector3 savedPosition;
        Quaternion savedRotation;
        float savedFov,savedSize,savedNear,nextShot,recoil;
        bool savedOrtho,savedFog,ending;
        Color savedBackground;
        int pendingShots;
        static int session;
        public void Begin(JellyWorld w,Camera camera,Transform stage,Transform scenery)
        {
            world=w;cam=camera;boardStage=stage;decor=scenery;
            savedPosition=cam.transform.position;savedRotation=cam.transform.rotation;
            savedFov=cam.fieldOfView;savedSize=cam.orthographicSize;savedNear=cam.nearClipPlane;
            savedOrtho=cam.orthographic;savedBackground=cam.backgroundColor;savedFog=RenderSettings.fog;
            mainScene=SceneManager.GetActiveScene();Ammo=16;Hits=0;Remaining=world.BoardSize*world.BoardSize;Targets=new JellyRangeTarget[world.BoardSize,world.BoardSize];pendingShots=0;nextShot=0;recoil=0;
            Active=true;Transitioning=true;ending=false;StartCoroutine(Enter());
        }
        IEnumerator Fade(float from,float to,float seconds)
        {
            for(float t=0;t<seconds;t+=Time.unscaledDeltaTime){world.SetRangeFade(Mathf.Lerp(from,to,t/seconds));yield return null;}
            world.SetRangeFade(to);
        }
        IEnumerator Enter()
        {
            yield return world.PlayToolManifest(4,Vector2Int.zero);
            yield return Fade(0,1,.28f);
            rangeScene=SceneManager.CreateScene("SugarRange_"+(++session));
            SceneManager.SetActiveScene(rangeScene);
            root=new GameObject("糖弹靶场").transform;SceneManager.MoveGameObjectToScene(root.gameObject,rangeScene);
            boardStage.gameObject.SetActive(false);decor.gameObject.SetActive(false);
            BuildRange();
            cam.orthographic=false;cam.fieldOfView=49;cam.nearClipPlane=.08f;
            cam.transform.SetPositionAndRotation(new Vector3(0,4.2f,-1.8f),Quaternion.LookRotation(new Vector3(0,.20f,12)));
            cam.backgroundColor=new Color(.70f,.85f,.97f);RenderSettings.fog=false;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.60f,.62f,.69f);
            world.SetRangeVisible(true);world.SetRangeHUD(Ammo,Hits,Remaining);
            yield return Fade(1,0,.35f);
            Transitioning=false;world.busy=false;nextShot=Time.time+.18f;SyncCursor();
        }
        Transform Group(string name,Transform parent)
        {
            var g=new GameObject(name);g.transform.SetParent(parent,false);return g.transform;
        }
        void BuildRange()
        {
            fx=Group("Sugar impacts",root);
            world.Shape("Range foundation",root,new Vector3(0,-.38f,5),new Vector3(25,.7f,26),world.cream);
            for(int x=-6;x<=6;x++)for(int z=-2;z<=9;z++)
                world.Shape("Floor tile",root,new Vector3(x*1.8f,0,z*1.8f),new Vector3(1.76f,.06f,1.76f),(x+z)%2==0?world.mint:world.white);
            world.Shape("Target wall outer",root,new Vector3(0,4.45f,11.1f),new Vector3(10.4f,8.25f,.7f),world.candy[4]);
            world.Shape("Target wall rim",root,new Vector3(0,4.45f,10.70f),new Vector3(10.07f,7.94f,.18f),world.gold);
            world.Shape("Target wall inset",root,new Vector3(0,4.45f,10.52f),new Vector3(9.78f,7.66f,.22f),world.plum);
            for(int x=0;x<world.BoardSize;x++)for(int y=0;y<world.BoardSize;y++) {
                var p=new Vector3((x-world.BoardHalf)*(9.04f/world.BoardSize),1.35f+y*(6.96f/world.BoardSize),10.02f);
                world.Shape("Target socket",root,p+Vector3.forward*.35f,new Vector3(8.32f/world.BoardSize,6.56f/world.BoardSize,.12f),world.cream);
                if(world.board.Cells[x,y]>=0) {
                    var g=world.CreateRangeTarget(root,x,y,p);
                    Targets[x,y]=g.GetComponent<JellyRangeTarget>();
                } else Remaining--;
            }
            for(int side=-1;side<=1;side+=2) {
                world.Shape("Candy pillar",root,new Vector3(side*5.8f,4.5f,10.2f),new Vector3(.75f,9,.75f),world.mint);
                for(int i=0;i<8;i++)
                    world.Shape("Pillar ribbon",root,new Vector3(side*5.8f,.55f+i*1.1f,10.2f),new Vector3(.80f,.22f,.80f),world.blush).transform.localRotation=Quaternion.Euler(0,0,side*14);
                world.Shape("Pillar candy cap",root,new Vector3(side*5.8f,9.3f,10.2f),Vector3.one*1.1f,world.candy[0],true);
                for(int i=0;i<3;i++) {
                    Vector3 p=new Vector3(side*(8+i*1.4f),0,9+i*3);
                    world.Shape("Lollipop stem",root,p+Vector3.up*2,new Vector3(.17f,4,.17f),world.cream);
                    world.Shape("Lollipop",root,p+Vector3.up*4,new Vector3(1.6f,2,1.1f),world.candy[(i+1)%5],true);
                }
                world.Shape("Side booth",root,new Vector3(side*9,1.05f,5),new Vector3(3.5f,2.1f,4),world.candy[side<0?0:1]);
                world.Shape("Booth counter",root,new Vector3(side*9,2.2f,5),new Vector3(3.8f,.25f,4.3f),world.cream);
            }
            for(int i=0;i<13;i++) {
                world.Shape("Striped canopy",root,new Vector3((i-6)*.9f,8.83f,10.15f),new Vector3(.9f,.38f,2.6f),i%2==0?world.blush:world.cream);
                world.Shape("Canopy scallop",root,new Vector3((i-6)*.9f,8.61f,8.86f),new Vector3(.88f,.58f,.25f),i%2==0?world.blush:world.cream,true);
            }
            for(int i=0;i<16;i++) {
                float x=(i-7.5f)*1.1f;
                world.Shape("Backdrop clouds",root,new Vector3(x,10+(i%3)*.45f,19),new Vector3(2.8f,1.3f,1.2f),world.white,true);
            }
            launcher=JellyToolModels.Create(world,4,root);launcher.SetParent(cam.transform,false);
            launcher.localPosition=new Vector3(.88f,-.57f,1.6f);launcher.localRotation=Quaternion.Euler(-5,-9,0);launcher.localScale=Vector3.one*.31f;
            muzzle=launcher.Find("Candy muzzle");
            foreach(var r in launcher.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
        }
        void Update()
        {
            if(!Active || Transitioning || ending || world.IsPaused)return;
            bool ui=EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject();
            world.AimRangeUI(Input.mousePosition,ui);Cursor.visible=ui;
            if(Input.GetMouseButton(0) && !ui && Time.time>=nextShot)FireAtScreen(Input.mousePosition);
            recoil=Mathf.MoveTowards(recoil,0,Time.deltaTime*2.7f);
            launcher.localPosition=new Vector3(.88f,-.57f,1.6f-recoil*.35f);
            Ray ray=cam.ScreenPointToRay(Input.mousePosition);RaycastHit hit;
            Vector3 aim=Physics.Raycast(ray,out hit,80)?hit.point:ray.GetPoint(30);
            Vector3 localAim=cam.transform.InverseTransformDirection(aim-launcher.position);
            float yaw=Mathf.Clamp(Mathf.Atan2(localAim.x,localAim.z)*Mathf.Rad2Deg,-18,18);
            float pitch=Mathf.Clamp(-Mathf.Atan2(localAim.y,new Vector2(localAim.x,localAim.z).magnitude)*Mathf.Rad2Deg,-12,12);
            var desired=cam.transform.rotation*Quaternion.Euler(pitch,yaw,0);
            launcher.rotation=Quaternion.Slerp(launcher.rotation,desired,1-Mathf.Exp(-12*Time.deltaTime));
            if((!world.InfiniteMode && Ammo<=0 || Remaining<=0) && pendingShots==0)End();
        }
        public bool FireAtScreen(Vector2 screen)
        {
            if(!Active || Transitioning || ending || world.IsPaused || Time.time<nextShot || (!world.InfiniteMode && Ammo<=0))return false;
            Ray ray=cam.ScreenPointToRay(screen);RaycastHit hit;
            JellyRangeTarget target=null;Vector3 aim=ray.GetPoint(32);
            if(Physics.Raycast(ray,out hit,80)){target=hit.collider.GetComponent<JellyRangeTarget>();aim=hit.point;}
            if(!world.InfiniteMode)Ammo--;
            nextShot=Time.time+.19f;recoil=.14f;pendingShots++;
            var ball=world.Shape("Flying sugar bubble",fx,muzzle.position,Vector3.one*.18f,world.candy[Hits%5],true);
            ball.transform.position=muzzle.position;
            StartCoroutine(Fly(ball,aim,target));world.RangePop(Hits%5);
            world.SetRangeHUD(Ammo,Hits,Remaining);
            return true;
        }
        IEnumerator Fly(GameObject ball,Vector3 destination,JellyRangeTarget target)
        {
            Vector3 origin=ball.transform.position;float duration=Mathf.Clamp(Vector3.Distance(origin,destination)/30,.10f,.85f);
            for(float t=0;t<duration;t+=Time.deltaTime) {
                if(ball==null)yield break;
                float u=t/duration;ball.transform.position=Vector3.Lerp(origin,destination,u)+Vector3.up*Mathf.Sin(u*Mathf.PI)*.22f;
                yield return null;
            }
            if(ball!=null)Destroy(ball);
            if(target!=null && !target.taken && world.RemoveRangeCell(target.x,target.y)) {
                target.taken=true;var p=target.transform.position;
                JellyVfx.Shatter(world,fx,p,target.color,Vector3.back);
                if(target.iced)JellyVfx.IceBreak(world,fx,p,Vector3.back);
                target.gameObject.SetActive(false);Destroy(target.gameObject);Targets[target.x,target.y]=null;
                Hits++;Remaining--;world.RangeHit(target.color);
            } else JellyVfx.Ring(world,fx,destination,Vector3.back,.25f,.20f,new Color(1,1,1,.45f));
            pendingShots--;world.SetRangeHUD(Ammo,Hits,Remaining);
        }
        public void SyncCursor(){if(!Active)return;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        public void End()
        {
            if(!Active || Transitioning || ending || world.IsPaused)return;
            ending=true;world.busy=true;StartCoroutine(Exit());
        }
        IEnumerator Exit()
        {
            // Resolve already-fired candy before leaving, so score and ammo agree.
            while(pendingShots>0)yield return null;
            Transitioning=true;SyncCursor();yield return Fade(0,1,.28f);
            RestoreView();
            AsyncOperation unload=null;
            if(rangeScene.IsValid() && rangeScene.isLoaded)unload=SceneManager.UnloadSceneAsync(rangeScene);
            if(unload!=null)while(!unload.isDone)yield return null;
            yield return Fade(1,0,.3f);
            Active=false;Transitioning=false;ending=false;world.CompleteRange();
        }
        void RestoreView()
        {
            if(mainScene.IsValid() && mainScene.isLoaded)SceneManager.SetActiveScene(mainScene);
            if(root!=null)root.gameObject.SetActive(false);
            if(launcher!=null){launcher.gameObject.SetActive(false);Destroy(launcher.gameObject);}
            cam.orthographic=savedOrtho;cam.fieldOfView=savedFov;cam.orthographicSize=savedSize;cam.nearClipPlane=savedNear;
            cam.transform.SetPositionAndRotation(savedPosition,savedRotation);cam.backgroundColor=savedBackground;RenderSettings.fog=savedFog;
            boardStage.gameObject.SetActive(true);decor.gameObject.SetActive(true);
            world.SetRangeVisible(false);SyncCursor();
        }
        public void Cancel()
        {
            if(!Active)return;
            StopAllCoroutines();RestoreView();
            if(rangeScene.IsValid() && rangeScene.isLoaded)SceneManager.UnloadSceneAsync(rangeScene);
            Active=false;Transitioning=false;ending=false;pendingShots=0;world.SetRangeFade(0);
        }
        void OnDisable(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
    }
}
