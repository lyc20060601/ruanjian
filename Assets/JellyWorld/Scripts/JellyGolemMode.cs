using System.Collections;
using UnityEngine;

namespace JellyWorldGame
{
    public class JellyGolemMode : MonoBehaviour
    {
        public bool Active { get; private set; }
        public bool Transitioning { get; private set; }
        public bool FirstPerson { get; private set; }
        public int Energy { get; private set; }
        public int Crushed { get; private set; }
        public int Stomps { get; private set; }
        public float Seconds { get; private set; }
        public Transform Actor { get { return actor; } }
        public bool IsSlamming { get { return slamAge>=0; } }
        public bool IsJumping {get;private set;}
        public bool IsCharging {get;private set;}
        public float Charge01 {get{return Mathf.Clamp01(chargeTime/1.25f);}}
        public float LastJumpDistance {get;private set;}
        public float LookYaw {get{return yaw;}}
        public float LookPitch {get{return pitch;}}
        public string TransitionPhase {get;private set;}
        public bool PlatformVisible {get{return arena!=null && arena.gameObject.activeSelf;}}
        JellyWorld world;
        Transform stage,decor,effects,arena,actor;
        JellyGuardianVisual visual;
        Camera cam;
        Vector3 savedPosition,spawnPoint,jumpStart,jumpEnd;
        Quaternion savedRotation,savedStage;
        float savedFov,savedNear,yaw,pitch=28,stepClock,slamAge=-1,shake,gaitSpeed,introAge;
        float chargeTime,jumpAge,jumpDuration,jumpHeight;
        Vector2Int pendingIce=new Vector2Int(-1,-1);
        float iceFollowAt,feedbackUntil;
        string hitFeedback="";
        bool slamHit, ending, wasFog,uiMouse,returnAfterJump;
        LineRenderer landingRing;
        Material landingRingMaterial;
        Color oldFogColor;
        float oldFogStart,oldFogEnd;
        FogMode oldFogMode;
        bool CanControl {get{return Active && !Transitioning && !ending && !world.IsPaused;}}
        public void Begin(JellyWorld w,Transform board,Transform scenery,Camera camera,Transform fx,Vector2Int landing)
        {
            world=w;stage=board;decor=scenery;cam=camera;effects=fx;
            savedPosition=cam.transform.position;savedRotation=cam.transform.rotation;savedFov=cam.fieldOfView;savedStage=stage.localRotation;
            savedNear=cam.nearClipPlane;
            wasFog=RenderSettings.fog;oldFogColor=RenderSettings.fogColor;oldFogStart=RenderSettings.fogStartDistance;
            oldFogEnd=RenderSettings.fogEndDistance;oldFogMode=RenderSettings.fogMode;
            Active=true;Transitioning=true;ending=false;FirstPerson=false;
            IsCharging=false;IsJumping=false;uiMouse=false;returnAfterJump=false;chargeTime=0;
            pendingIce=new Vector2Int(-1,-1);hitFeedback="";feedbackUntil=0;
            Energy=18;Crushed=0;Stomps=3;Seconds=30;yaw=0;pitch=28;stepClock=0;slamAge=-1;shake=0;introAge=0;
            spawnPoint=new Vector3(landing.x-world.BoardHalf,.65f,landing.y-world.BoardHalf);
            world.ClearGuardianEffects();BuildArena();arena.gameObject.SetActive(false);actor.gameObject.SetActive(false);decor.gameObject.SetActive(false);
            cam.orthographic=false;cam.fieldOfView=31;cam.nearClipPlane=.1f;
            StartCoroutine(Enter());
        }
        void BuildArena()
        {
            arena=new GameObject("Guardian • cloud sanctuary").transform;arena.SetParent(world.transform,false);
            world.Shape("Floating sanctuary",arena,new Vector3(0,-1.35f,0),new Vector3(world.BoardSize+4.5f,1.55f,world.BoardSize+4.5f),world.stone);
            world.Shape("Mint foundation",arena,new Vector3(0,-2,0),new Vector3(world.BoardSize+3.9f,.7f,world.BoardSize+3.9f),world.mint);
            world.Shape("Gold floating seam",arena,new Vector3(0,-1.84f,0),new Vector3(world.BoardSize+4.55f,.10f,world.BoardSize+4.55f),world.gold);
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2) {
                var p=new Vector3(x*(world.BoardHalf+1.38f),-.02f,z*(world.BoardHalf+1.38f));
                world.Shape("Corner plinth",arena,p,new Vector3(.78f,.65f,.78f),world.cream);
                var gem=world.Shape("Sentinel crystal",arena,p+Vector3.up*.67f,new Vector3(.40f,.98f,.40f),world.rune);
                gem.GetComponent<MeshFilter>().sharedMesh=world.fractureMesh;
                world.Shape("Golden crystal collar",arena,p+Vector3.up*.36f,new Vector3(.55f,.12f,.55f),world.gold);
            }
            for(int i=0;i<12;i++) {
                float a=i*6.28318f/12;Vector3 p=new Vector3(Mathf.Sin(a)*24,-5,Mathf.Cos(a)*24);
                world.Shape("Distant sorbet island",arena,p,new Vector3(8,4,8),world.candy[i%5],true);
                world.Shape("Vanilla island top",arena,p+Vector3.up*1.9f,new Vector3(8.1f,1.3f,8.1f),world.cream,true);
                for(int j=0;j<3;j++)world.Shape("Cloud puff",arena,p+new Vector3(j-1,9+(i%3),0),new Vector3(3.2f,1.5f,2.2f),world.white,true);
                if(i%2==0) {
                    world.Shape("Sugar spire",arena,p+Vector3.up*5,new Vector3(.9f,6,.9f),world.candy[(i+1)%5]);
                    var crystal=world.Shape("Spire crown",arena,p+Vector3.up*8,new Vector3(1.1f,2,1.1f),world.rune);
                    crystal.GetComponent<MeshFilter>().sharedMesh=world.fractureMesh;
                }
            }
            actor=new GameObject("MOSS • sugar crystal guardian").transform;actor.SetParent(arena,false);
            actor.position=spawnPoint;actor.rotation=Quaternion.Euler(0,180,0);
            visual=actor.gameObject.AddComponent<JellyGuardianVisual>();visual.Build(world);actor.localScale=Vector3.one*.56f;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=cam.backgroundColor;
            RenderSettings.fogStartDistance=19;RenderSettings.fogEndDistance=48;
        }
        Vector3 OverviewLocal {get{return new Vector3(0,-world.BoardSize-5,-world.BoardSize-3);}}
        void FrameStage(Vector3 local,float fov)
        {
            cam.transform.position=stage.TransformPoint(local);
            cam.transform.rotation=Quaternion.LookRotation(stage.position-cam.transform.position,stage.up);
            cam.fieldOfView=fov;
        }
        IEnumerator Enter()
        {
            TransitionPhase="fold";Vector3 startLocal=stage.InverseTransformPoint(savedPosition);
            world.GuardianImpact(true);
            for(float t=0;t<1.1f;t+=Time.deltaTime) {
                float u=Mathf.SmoothStep(0,1,t/1.1f);
                stage.localRotation=Quaternion.Slerp(savedStage,Quaternion.Euler(90,0,0),u);
                FrameStage(Vector3.Lerp(startLocal,OverviewLocal,u),Mathf.Lerp(31,45,u));
                world.SetGuardianHUD(Seconds,Energy,Crushed,Stomps,false,"展开棋盘");yield return null;
            }
            stage.localRotation=Quaternion.Euler(90,0,0);FrameStage(OverviewLocal,45);
            TransitionPhase="deploy";arena.gameObject.SetActive(true);actor.gameObject.SetActive(true);
            Vector3 from=cam.transform.position;Quaternion rot=cam.transform.rotation;
            Vector3 end=ThirdPositionAt(spawnPoint);Quaternion endRot=Quaternion.LookRotation(CameraTargetAt(spawnPoint)-end);
            for(float t=0;t<.8f;t+=Time.deltaTime) {
                float u=Mathf.SmoothStep(0,1,t/.8f);actor.position=spawnPoint+Vector3.up*(1-u)*3.5f;
                cam.transform.SetPositionAndRotation(Vector3.Lerp(from,end,u),Quaternion.Slerp(rot,endRot,u));
                cam.fieldOfView=Mathf.Lerp(45,53,u);yield return null;
            }
            actor.position=spawnPoint;
            JellyVfx.Ring(world,effects,actor.position,Vector3.up,1.5f,.65f,new Color(.5f,1,.82f,.9f));
            Transitioning=false;TransitionPhase="active";world.busy=false;world.SetJumpHUD(0,false,false);
        }
        Vector2 MovementInput()
        {
            return new Vector2((Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),
                (Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0));
        }
        void Update()
        {
            if(!CanControl)return;
            float dt=Time.deltaTime;if(!world.InfiniteMode)Seconds=Mathf.Max(0,Seconds-dt);introAge+=dt;
            if(Input.GetKeyDown(KeyCode.V))ToggleView();
            if(Input.GetKeyDown(KeyCode.Tab)){uiMouse=!uiMouse;SyncCursor();}
            if(Input.GetKeyDown(KeyCode.R)){End();return;}
            yaw+=((Input.GetKey(KeyCode.E)?1:0)-(Input.GetKey(KeyCode.Q)?1:0))*90*dt;
            if((FirstPerson && Cursor.lockState==CursorLockMode.Locked) || (!FirstPerson && Input.GetMouseButton(1)))
                Look(new Vector2(Input.GetAxisRaw("Mouse X"),Input.GetAxisRaw("Mouse Y")));
            if(FirstPerson && !uiMouse && Cursor.lockState!=CursorLockMode.Locked && Input.GetMouseButtonDown(0) &&
                (UnityEngine.EventSystems.EventSystem.current==null || !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()))SyncCursor();
            if(pendingIce.x>=0 && !IsJumping && !IsCharging && slamAge<0 && Time.time>=iceFollowAt) {
                var cell=new Vector2Int(Mathf.RoundToInt(actor.position.x+world.BoardHalf),Mathf.RoundToInt(actor.position.z+world.BoardHalf));
                bool same=cell==pendingIce;pendingIce=new Vector2Int(-1,-1);
                if(same){Impact(false);hitFeedback="消除 2/2";feedbackUntil=Time.time+.5f;}
            }
            Vector2 input=MovementInput();bool sprint=Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);
            if(Input.GetKeyDown(KeyCode.Space))BeginCharge();
            if(IsCharging) {AddCharge(dt);UpdateLandingRing(input);if(Input.GetKeyUp(KeyCode.Space))ReleaseJump(input);}
            if(Input.GetKeyDown(KeyCode.F))Slam();
            if(IsJumping)UpdateJump(dt);
            else if(slamAge>=0) {
                slamAge+=dt;float phase=slamAge/.8f;
                if(!slamHit && phase>=.64f){slamHit=true;Impact(true);}
                visual.Animate(0,false,Mathf.Clamp01(phase));if(phase>=1)slamAge=-1;
            } else if(IsCharging) {visual.Animate(0,false,-1);visual.ChargePose(Charge01);}
            else {
                Drive(input,sprint,dt);visual.Animate(gaitSpeed,sprint,-1);
                if(pendingIce.x>=0 && Time.time<iceFollowAt)visual.IceStompPose(1-(iceFollowAt-Time.time)/.46f);
            }
            if(!IsJumping) {var p=actor.position;p.y=Mathf.Lerp(p.y,GroundAt(p),1-Mathf.Exp(-18*dt));actor.position=p;}
            if(FirstPerson)actor.rotation=Quaternion.Euler(0,yaw,0);
            world.SetGuardianHUD(Seconds,Energy,Crushed,Stomps,FirstPerson,
                Time.time<feedbackUntil?hitFeedback:"");
            world.SetJumpHUD(Charge01,IsCharging,IsJumping);
            if(!world.InfiniteMode && (Seconds<=0 || Energy<=0) && slamAge<0 && !IsJumping)End();
        }
        public void Look(Vector2 mouseDelta)
        {
            if(!CanControl)return;
            yaw=Mathf.Repeat(yaw+mouseDelta.x*2.3f,360);pitch=Mathf.Clamp(pitch-mouseDelta.y*1.8f,-75,80);
        }
        public void SyncCursor()
        {
            if(world!=null && world.IsPaused)CancelCharge();
            bool locked=Active && !Transitioning && FirstPerson && !uiMouse && !world.IsPaused && Application.isFocused;
            Cursor.lockState=locked?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!locked;
        }
        void OnApplicationFocus(bool focus)
        {
            if(!focus){CancelCharge();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            else if(Active)SyncCursor();
        }
        void OnDisable(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        public void Drive(Vector2 input,bool sprint,float dt)
        {
            if(!CanControl || slamAge>=0 || IsJumping || IsCharging)return;
            if(pendingIce.x>=0 && Time.time<iceFollowAt){gaitSpeed=0;return;}
            input=Vector2.ClampMagnitude(input,1);gaitSpeed=input.magnitude;
            Vector3 direction=Quaternion.Euler(0,yaw,0)*new Vector3(input.x,0,input.y);
            Vector3 before=actor.position,after=before+direction*(sprint?3.7f:2.25f)*Mathf.Min(dt,.1f);
            after.x=Mathf.Clamp(after.x,-world.BoardLimit,world.BoardLimit);after.z=Mathf.Clamp(after.z,-world.BoardLimit,world.BoardLimit);actor.position=after;
            if(direction.sqrMagnitude>.01f) {
                if(!FirstPerson)actor.rotation=Quaternion.Slerp(actor.rotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-15*dt));
                if((after-before).sqrMagnitude>.000001f) {stepClock-=dt;if(stepClock<=0){Impact(false);stepClock=sprint?.20f:.31f;}}
            }
        }
        public void BeginCharge()
        {
            if(!CanControl || IsJumping || slamAge>=0 || IsCharging)return;
            IsCharging=true;chargeTime=0;gaitSpeed=0;
        }
        public void AddCharge(float dt){if(CanControl && IsCharging)chargeTime=Mathf.Min(1.25f,chargeTime+Mathf.Max(0,dt));}
        void CancelCharge(){IsCharging=false;chargeTime=0;if(landingRing!=null)landingRing.gameObject.SetActive(false);}
        public static float JumpDistance(float charge){return Mathf.Lerp(1,4.5f,Mathf.Clamp01(charge));}
        Vector3 JumpDestination(Vector2 aim)
        {
            Vector3 direction=Quaternion.Euler(0,yaw,0)*(aim.sqrMagnitude>.01f?new Vector3(aim.normalized.x,0,aim.normalized.y):Vector3.forward);
            Vector3 end=actor.position+direction*JumpDistance(Charge01);
            end.x=Mathf.Clamp(end.x,-world.BoardLimit,world.BoardLimit);end.z=Mathf.Clamp(end.z,-world.BoardLimit,world.BoardLimit);end.y=GroundAt(end);return end;
        }
        Vector3 JumpPoint(Vector3 start,Vector3 end,float height,float t)
        {
            float travel=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.88f,t));Vector3 p=Vector3.Lerp(start,end,travel);
            p.y=Mathf.Lerp(start.y,end.y,t)+Mathf.Sin(Mathf.PI*t)*height;
            if(travel>0 && travel<1)p.y=Mathf.Max(.69f,p.y);return p;
        }
        void UpdateLandingRing(Vector2 aim)
        {
            if(landingRing==null) {
                var g=new GameObject("Jump landing target");g.transform.SetParent(arena,false);
                landingRingMaterial=new Material(world.glow);landingRingMaterial.SetInt("_ZTest",(int)UnityEngine.Rendering.CompareFunction.Always);
                landingRingMaterial.renderQueue=3500;
                landingRing=g.AddComponent<LineRenderer>();landingRing.sharedMaterial=landingRingMaterial;landingRing.useWorldSpace=true;
                landingRing.loop=true;landingRing.positionCount=48;landingRing.widthMultiplier=.045f;
                landingRing.startColor=landingRing.endColor=new Color(1,.80f,.30f,.95f);
                landingRing.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;landingRing.receiveShadows=false;
            }
            landingRing.gameObject.SetActive(true);var end=JumpDestination(aim);
            // A compact ring sits just above the landing surface; nothing spans the view.
            for(int i=0;i<48;i++) {
                float angle=i*Mathf.PI*2/48;
                landingRing.SetPosition(i,end+new Vector3(Mathf.Cos(angle)*.34f,.025f,Mathf.Sin(angle)*.34f));
            }
        }
        public void ReleaseJump(Vector2 aim)
        {
            if(!CanControl || !IsCharging)return;
            jumpStart=actor.position;jumpEnd=JumpDestination(aim);jumpAge=0;
            jumpDuration=.65f+Charge01*.40f;jumpHeight=1.5f+Charge01*.9f;
            LastJumpDistance=Vector2.Distance(new Vector2(jumpStart.x,jumpStart.z),new Vector2(jumpEnd.x,jumpEnd.z));
            var direction=jumpEnd-jumpStart;direction.y=0;if(!FirstPerson && direction.sqrMagnitude>.01f)actor.rotation=Quaternion.LookRotation(direction);
            world.PlaySound(JellySound.Jump,.30f);
            IsCharging=false;IsJumping=true;if(landingRing!=null)landingRing.gameObject.SetActive(false);
            JellyVfx.Ring(world,effects,jumpStart,Vector3.up,.8f,.3f,new Color(.5f,1,.9f,.7f));
        }
        void UpdateJump(float dt)
        {
            jumpAge+=dt;float t=Mathf.Clamp01(jumpAge/jumpDuration);
            actor.position=JumpPoint(jumpStart,jumpEnd,jumpHeight,t);visual.Animate(0,false,-1);visual.LeapPose(t);
            if(t>=1) {actor.position=jumpEnd;IsJumping=false;chargeTime=0;Impact(false);if(returnAfterJump){returnAfterJump=false;End();}}
        }
        public void Slam()
        {
            if(!CanControl || (!world.InfiniteMode && Stomps<=0) || slamAge>=0 || IsJumping || IsCharging)return;
            if(!world.InfiniteMode)Stomps--;slamAge=0;slamHit=false;
            JellyVfx.Ring(world,effects,new Vector3(actor.position.x,.72f,actor.position.z),Vector3.up,1.65f,.52f,new Color(1,.81f,.43f,.8f));
        }
        float GroundAt(Vector3 p)
        {
            int x=Mathf.Clamp(Mathf.RoundToInt(p.x+world.BoardHalf),0,world.BoardSize-1),y=Mathf.Clamp(Mathf.RoundToInt(p.z+world.BoardHalf),0,world.BoardSize-1);
            return world.board.Cells[x,y]<0?.14f:.65f;
        }
        void Impact(bool heavy)
        {
            int cx=Mathf.Clamp(Mathf.RoundToInt(actor.position.x+world.BoardHalf),0,world.BoardSize-1),cy=Mathf.Clamp(Mathf.RoundToInt(actor.position.z+world.BoardHalf),0,world.BoardSize-1),before=Crushed;
            bool centerIce=world.IsIced(cx,cy);
            if(heavy) {for(int ring=0;ring<=1;ring++)for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)if(Mathf.Abs(dx)+Mathf.Abs(dy)==ring)Crush(cx+dx,cy+dy);}
            else Crush(cx,cy);
            if(centerIce && !world.IsIced(cx,cy)) {
                pendingIce=new Vector2Int(cx,cy);iceFollowAt=Time.time+.46f;
                hitFeedback="碎冰 1/2";feedbackUntil=Time.time+.46f;shake=.025f;
            }
            if(Crushed>before || heavy) {
                shake=heavy?.17f:.045f;world.GuardianImpact(heavy);
                JellyVfx.Ring(world,effects,new Vector3(actor.position.x,.75f,actor.position.z),Vector3.up,heavy?2.4f:.65f,heavy?.75f:.35f,
                    heavy?new Color(1,.86f,.42f,1):new Color(.60f,1,.89f,.7f));
            }
        }
        void Crush(int x,int y)
        {
            if(!world.InfiniteMode && Energy<=0)return;
            if(world.CrushGuardianCell(x,y)){if(!world.InfiniteMode)Energy--;Crushed++;}
        }
        void LateUpdate()
        {
            if(!CanControl || actor==null)return;
            Vector3 desired=FirstPerson?actor.position+Vector3.up*1.30f:ThirdPositionAt(actor.position);
            Quaternion rotation=FirstPerson?Quaternion.Euler(pitch,yaw,0):Quaternion.LookRotation(CameraTargetAt(actor.position)-desired);
            float u=1-Mathf.Exp(-12*Time.deltaTime);
            cam.transform.position=Vector3.Lerp(cam.transform.position,desired,u);cam.transform.rotation=Quaternion.Slerp(cam.transform.rotation,rotation,u);
            cam.fieldOfView=Mathf.Lerp(cam.fieldOfView,FirstPerson?72:53,u);
            visual.FirstPerson(FirstPerson || Vector3.Distance(cam.transform.position,actor.position+Vector3.up*.72f)<1.45f);
            shake=Mathf.MoveTowards(shake,0,Time.deltaTime*.5f);
            cam.transform.position+=new Vector3(Mathf.Sin(Time.time*67),Mathf.Cos(Time.time*83),0)*shake;
        }
        Vector3 CameraTargetAt(Vector3 p){return p+new Vector3(0,.65f,.35f);}
        Vector3 ThirdPositionAt(Vector3 p){return p+Quaternion.Euler(0,yaw,0)*new Vector3(0,5.4f,-7.3f);}
        public void ToggleView()
        {
            if(!CanControl)return;FirstPerson=!FirstPerson;uiMouse=false;visual.FirstPerson(FirstPerson);SyncCursor();
        }
        public void End()
        {
            if(!CanControl)return;if(IsJumping){returnAfterJump=true;return;}CancelCharge();
            if(slamAge>=0 && !slamHit){slamHit=true;Impact(true);}
            ending=true;Transitioning=true;world.busy=true;FirstPerson=false;actor.gameObject.SetActive(false);SyncCursor();StartCoroutine(Exit());
        }
        IEnumerator Exit()
        {
            TransitionPhase="recall";world.ClearGuardianEffects();Vector3 from=cam.transform.position;Quaternion rot=cam.transform.rotation;
            Vector3 overhead=stage.TransformPoint(OverviewLocal);Quaternion overheadRot=Quaternion.LookRotation(stage.position-overhead,stage.up);
            for(float t=0;t<.55f;t+=Time.deltaTime) {
                float u=Mathf.SmoothStep(0,1,t/.55f);cam.transform.SetPositionAndRotation(Vector3.Lerp(from,overhead,u),Quaternion.Slerp(rot,overheadRot,u));
                actor.localScale=Vector3.one*.56f*(1-u);world.SetGuardianHUD(Seconds,Energy,Crushed,Stomps,false,"返回棋盘");yield return null;
            }
            actor.gameObject.SetActive(false);arena.gameObject.SetActive(false);TransitionPhase="unfold";
            Vector3 endLocal=Quaternion.Inverse(savedStage)*(savedPosition-stage.position);
            for(float t=0;t<1.1f;t+=Time.deltaTime) {
                float u=Mathf.SmoothStep(0,1,t/1.1f);stage.localRotation=Quaternion.Slerp(Quaternion.Euler(90,0,0),savedStage,u);
                FrameStage(Vector3.Lerp(OverviewLocal,endLocal,u),Mathf.Lerp(45,31,u));yield return null;
            }
            Restore();world.CompleteGuardian();
        }
        void Restore()
        {
            Active=false;Transitioning=false;ending=false;IsCharging=false;IsJumping=false;FirstPerson=false;TransitionPhase="idle";
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            if(stage!=null)stage.localRotation=savedStage;
            if(cam!=null){cam.orthographic=true;cam.fieldOfView=savedFov;cam.nearClipPlane=savedNear;cam.transform.SetPositionAndRotation(savedPosition,savedRotation);}
            if(decor!=null)decor.gameObject.SetActive(true);
            if(arena!=null){arena.gameObject.SetActive(false);Destroy(arena.gameObject);}
            if(landingRingMaterial!=null){Destroy(landingRingMaterial);landingRingMaterial=null;}
            RenderSettings.fog=wasFog;RenderSettings.fogColor=oldFogColor;RenderSettings.fogStartDistance=oldFogStart;
            RenderSettings.fogEndDistance=oldFogEnd;RenderSettings.fogMode=oldFogMode;actor=null;
        }
        public void Cancel(){if(!Active)return;StopAllCoroutines();Restore();}
    }
}
