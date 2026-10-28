using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Askich
{
    public sealed partial class ArenaGame : MonoBehaviour
    {
        enum Mode { Menu, Faction, Story, Playing, Upgrade, Paused, Shop, Defeat, Victory, Settlement, Armory, Raid }
        sealed class Enemy
        {
            public SpriteRenderer view, warningView;
            public Vector2 pos, direction;
            public int kind;
            public float hp, maxHp, speed, timer, warning, flash;
            public bool winding, objectiveTarget;
        }
        sealed class Arrow { public SpriteRenderer view; public Vector2 pos, velocity; public float life = 4; }
        sealed class Coin { public SpriteRenderer view; public Vector2 pos; public int value; }
        sealed class Obstacle { public Vector2 pos; public float radius; }
        readonly List<Enemy> enemies = new List<Enemy>();
        readonly List<Arrow> arrows = new List<Arrow>();
        readonly List<Coin> coins = new List<Coin>();
        readonly List<Obstacle> obstacles = new List<Obstacle>();
        readonly List<SpriteRenderer> captives = new List<SpriteRenderer>();
        Transform world, actors, effects;
        Camera gameCamera;
        SpriteRenderer player, marker, objective, ally;
        SynthAudio sound;
        Mode mode;
        AskichProfile profile;
        bool persistenceEnabled = true, blocking, orderAlly, savedResult;
        Vector2 playerPos, facing = Vector2.right, dodgeDirection, objectivePos, allyPos;
        float health, maxHealth, stamina, maxStamina, damage, moveSpeed, reach;
        float attackTimer, attackPose, dodgeTimer, dodgeCooldown, hurtTimer, regenDelay, elapsed, spawnTimer, missionTime, allyTimer;
        float objectiveHealth, objectiveMax, staminaRegen = 24;
        int faction, wave, remaining, kills, score, runCredits, rescued;
        string notice = "";
        float noticeTime;
        public int LocationIndex => wave >= 3 ? 1 : 0;
        public string LocationName => LocationIndex == 0 ? "ПРИГРАНИЧНОЕ ПОСЕЛЕНИЕ" : "СТАРАЯ КРЕПОСТЬ";

        void Awake()
        {
            Application.targetFrameRate = 120;
            profile = AskichProfile.Load(); faction = profile.selectedFaction;
            gameCamera = new GameObject("Campaign camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.orthographic = true; gameCamera.orthographicSize = 8.1f;
            gameCamera.transform.position = new Vector3(0, 0, -10);
            gameCamera.backgroundColor = new Color(.075f,.065f,.045f);
            sound = gameObject.AddComponent<SynthAudio>();
            actors = new GameObject("Warriors").transform; actors.SetParent(transform);
            effects = new GameObject("Effects").transform; effects.SetParent(transform);
            RebuildArena();
            player = Actor("Player", faction, Vector2.zero);
            marker = NeonVisual.Make("Player allegiance", "ring", CampaignData.Colors[faction], Vector2.zero, new Vector2(.85f,.36f), -5, actors);
            mode = Mode.Menu;
            Debug.Log("ASKICH_MEDIEVAL_READY");
        }
        SpriteRenderer Actor(string label, int row, Vector2 position, float scale = 1)
        {
            var v = MedievalArt.Make(label, MedievalArt.Warrior(row,0), position, scale, actors);
            v.gameObject.AddComponent<WarriorMotion>().baseScale=scale;
            NeonVisual.Make("Shadow", "circle", new Color(0,0,0,.25f), Vector2.zero, new Vector2(.7f,.22f), -4, v.transform);
            return v;
        }
        void Prop(int index, Vector2 position, float scale, float solid = 0)
        {
            var scenery=MedievalArt.Make("Scenery", MedievalArt.Prop(index), position, scale, world, Sort(position));
            if(index==3||index==4||index==9||index==12)scenery.gameObject.AddComponent<LivingScenery>().kind=index;
            if (solid > 0) obstacles.Add(new Obstacle { pos = position + Vector2.up * .35f, radius = solid });
        }
        int Sort(Vector2 p) => 100 - Mathf.RoundToInt(p.y * 10);
        void RebuildArena()
        {
            if (world != null) Destroy(world.gameObject);
            world = new GameObject(LocationName).transform; world.SetParent(transform);
            obstacles.Clear();
            var ground=MedievalArt.Make("Ground", MedievalArt.Ground(LocationIndex == 1), Vector2.zero, 1, world, -100);
            ground.color=faction==2?new Color(.85f,.92f,1):faction==1?new Color(1,.96f,.82f):Color.white;
            for(int n=0;n<14;n++) {
                Vector2 p=new Vector2(-11+(n*7)%23,-5+(n*3)%10);
                var leaf=NeonVisual.Make("Windborne leaf","diamond",LocationIndex==0?new Color(.64f,.49f,.21f,.5f):new Color(.6f,.59f,.51f,.2f),p,new Vector2(.09f,.05f),-10,world);
                var wind=leaf.gameObject.AddComponent<DriftingLeaf>();wind.origin=p;wind.phase=n*.7f;
            }
            if (LocationIndex == 0)
            {
                Prop(0,new Vector2(-8,3),4.2f,1.15f); Prop(0,new Vector2(7,3.4f),3.8f,1);
                Prop(0,new Vector2(-8,-5.3f),3.7f,.9f); Prop(6,new Vector2(4,1),2,.55f);
                Prop(12,new Vector2(8,-5),3,.8f); Prop(7,new Vector2(-4,3.8f),1.4f,.4f);
                for(int i=0;i<6;i++) Prop(i%2==0?3:4,new Vector2(i%2==0?-12:12,-5+i*1.8f),3.4f);
                Prop(10,new Vector2(3,-5.4f),1.5f,.45f);
            }
            else
            {
                Prop(1,new Vector2(0,4.1f),5); Prop(2,new Vector2(-9,3.7f),4.4f,1);
                Prop(2,new Vector2(9,3.7f),4.4f,1);
                for(int x=-6;x<=6;x+=3) if(x!=0) Prop(14,new Vector2(x,4.8f),3);
                Prop(15,new Vector2(10,-4.8f),3.6f,.8f); Prop(12,new Vector2(-9,-5),3.2f,.8f);
                Prop(9,new Vector2(-4,2.8f),1.5f,.35f); Prop(9,new Vector2(4,2.8f),1.5f,.35f);
                Prop(8,new Vector2(5,-4.7f),1.8f,.6f);
            }
        }
        public void StartRun()
        {
            ClearCombat();
            wave = kills = score = runCredits = rescued = 0;
            faction = Mathf.Clamp(faction,0,2); profile.selectedFaction = faction; Save();
            maxHealth = faction==1?135:faction==0?110:105; health = maxHealth;
            maxStamina = faction==2?120:110; stamina = maxStamina; staminaRegen = 24;
            damage = faction==0?28:faction==1?26:34; moveSpeed = faction==1?3.65f:faction==0?4.1f:4.3f;
            reach = faction==0?2.2f:1.65f; ApplyEquipment();
            attackTimer = attackPose = dodgeTimer = dodgeCooldown = hurtTimer = regenDelay = elapsed = allyTimer = 0;
            blocking = orderAlly = savedResult = false; notice = ""; noticeTime = 0;
            playerPos = new Vector2(0,-1); facing = Vector2.right;
            player.sprite = MedievalArt.Warrior(faction,0); player.color = SkinColor(); player.transform.position = playerPos;
            marker.color = CampaignData.Colors[faction]; marker.transform.position = playerPos;
            RebuildArena(); mode = Mode.Story;
        }
        void ClearCombat()
        {
            foreach(var drop in gearDrops)Destroy(drop.view.gameObject);gearDrops.Clear();
            foreach(var e in enemies) { Destroy(e.view.gameObject); Destroy(e.warningView.gameObject); } enemies.Clear();
            foreach(var a in arrows) Destroy(a.view.gameObject); arrows.Clear();
            foreach(var c in coins) Destroy(c.view.gameObject); coins.Clear();
            foreach(var p in captives) if(p!=null) Destroy(p.gameObject); captives.Clear();
            if(objective!=null) Destroy(objective.gameObject); objective=null;
            if(ally!=null) Destroy(ally.gameObject); ally=null;
        }
        void NextWave()
        {
            if(wave>=5) return;
            ClearCombat(); wave++; remaining = ArenaRules.WaveSize(wave); spawnTimer = 1.2f; missionTime = 0; rescued = 0;
            playerPos = new Vector2(0,-1); health = Mathf.Min(maxHealth,health+25); stamina = maxStamina;
            RebuildArena();
            if(wave==2 || wave==4)
            {
                objectivePos = wave==2?new Vector2(-9,-2):new Vector2(0,4);
                objectiveHealth = objectiveMax = wave==2?220:260;
                objective = MedievalArt.Make(wave==2?"Supply wagon":"Gate barricade",MedievalArt.Prop(wave==2?5:13),objectivePos,wave==2?2.8f:3.3f,actors,Sort(objectivePos));
                if(wave==2) playerPos = objectivePos+Vector2.down;
            }
            if(wave==3)
                foreach(var p in new[]{new Vector2(-6,0),new Vector2(6,0),new Vector2(0,-4)})
                {
                    var captive=Actor("Prisoner — E",3,p,.8f); captive.color=new Color(.65f,.7f,.8f);
                    NeonVisual.Make("Rescue marker","ring",new Color(.8f,.7f,.4f),Vector2.zero,new Vector2(1.1f,.45f),-3,captive.transform);
                    captives.Add(captive);
                }
            if(orderAlly) { allyPos=playerPos+Vector2.left; ally=Actor("Brother Mathieu",6,allyPos); }
            mode=Mode.Playing; Debug.Log("ASKICH_MISSION_"+wave);
        }
        void Update()
        {
            ScreenLayout.Fit(gameCamera);
            if(TouchControls.Enabled) {
                touchControls.Read();
                if(touchControls.pause&&(mode==Mode.Playing||mode==Mode.Paused)){mode=mode==Mode.Playing?Mode.Paused:Mode.Playing;return;}
            }
            var k=Keyboard.current;
            if(k!=null)
            {
                if(k.mKey.wasPressedThisFrame) sound.Toggle();
                if(k.escapeKey.wasPressedThisFrame)
                { if(mode==Mode.Playing) mode=Mode.Paused; else if(mode==Mode.Paused) mode=Mode.Playing; else if(mode==Mode.Shop||mode==Mode.Faction||mode==Mode.Settlement||mode==Mode.Armory) mode=Mode.Menu; return; }
                if(mode==Mode.Menu && k.enterKey.wasPressedThisFrame) { mode=Mode.Faction; return; }
                if(mode==Mode.Faction)
                {
                    for(int i=0;i<3;i++) if(ChoiceKey(k,i)) { faction=i; StartRun(); return; }
                }
                if(mode==Mode.Story)
                {
                    if(wave==3) { if(k.digit1Key.wasPressedThisFrame) ContinueStory(true); else if(k.digit2Key.wasPressedThisFrame) ContinueStory(false); }
                    else if(k.enterKey.wasPressedThisFrame) ContinueStory(false);
                    return;
                }
                if(mode==Mode.Upgrade) { for(int i=0;i<3;i++) if(ChoiceKey(k,i)) { Upgrade(i); return; } }
            }
            if(mode==Mode.Raid) { TickRaid(Time.deltaTime); return; }
            if(mode!=Mode.Playing) return;
            Tick(Mathf.Min(Time.deltaTime,.05f));
        }
        bool ChoiceKey(Keyboard k,int i) => i==0?k.digit1Key.wasPressedThisFrame:i==1?k.digit2Key.wasPressedThisFrame:k.digit3Key.wasPressedThisFrame;
        void Tick(float dt)
        {
            elapsed+=dt; missionTime+=dt; noticeTime-=dt;
            attackTimer-=dt; attackPose-=dt; dodgeTimer-=dt; dodgeCooldown-=dt; hurtTimer-=dt; regenDelay-=dt;
            if(regenDelay<=0) stamina=Mathf.Min(maxStamina,stamina+staminaRegen*dt*(blocking?.3f:1));
            var k=Keyboard.current; var mouse=Mouse.current;
            Vector2 move=Vector2.zero;
            if(k!=null&&!TouchControls.Enabled)
            {
                move=new Vector2((k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0),(k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0)).normalized;
                if(k.spaceKey.wasPressedThisFrame) Dodge(move);
                if(k.eKey.wasPressedThisFrame) {Rescue();PickGear();}
            }
            if(mouse!=null&&!TouchControls.Enabled)
            {
                Vector2 aim=(Vector2)gameCamera.ScreenToWorldPoint(mouse.position.ReadValue())-playerPos;
                if(aim.sqrMagnitude>.01f) facing=aim.normalized;
            }
            if(TouchControls.Enabled) {
                move=touchControls.move;
                if(touchControls.dodge)Dodge(move);
                if(touchControls.interact){Rescue();PickGear();}
                Enemy closest=null;float range=100;
                foreach(var enemy in enemies){float d=(enemy.pos-playerPos).sqrMagnitude;if(d<range){range=d;closest=enemy;}}
                if(closest!=null)facing=(closest.pos-playerPos).normalized;else if(move.sqrMagnitude>.01f)facing=move;
            }
            blocking=(TouchControls.Enabled?touchControls.block:mouse!=null&&mouse.rightButton.isPressed)&&dodgeTimer<=0&&stamina>0;
            playerPos=MoveBody(playerPos,(dodgeTimer>0?dodgeDirection*12:move*moveSpeed*(blocking?.52f:1))*dt);
            if(TouchControls.Enabled?touchControls.heavy:k!=null&&k.qKey.wasPressedThisFrame) Attack(true);
            if(TouchControls.Enabled?touchControls.attack:mouse!=null&&mouse.leftButton.isPressed) Attack(false);
            Animate(player,faction,playerPos,move.sqrMagnitude>0,attackPose>0,facing.x);
            player.color=hurtTimer>0?new Color(1,.55f,.5f):SkinColor();
            marker.transform.position=playerPos; marker.color=blocking?new Color(.95f,.85f,.55f):CampaignData.Colors[faction];
            if(remaining>0)
            {
                spawnTimer-=dt;
                if(spawnTimer<=0) { SpawnEnemy(); remaining--; spawnTimer=wave==5?1.2f:1.7f; }
            }
            UpdateEnemies(dt);
            if(mode!=Mode.Playing) return;
            UpdateArrows(dt);
            if(mode!=Mode.Playing) return;
            UpdateAlly(dt); UpdateCoins(dt);
            if(wave==2 && Vector2.Distance(playerPos,objectivePos)<3.2f)
            { objectivePos.x=Mathf.Min(9,objectivePos.x+dt*.75f); objective.transform.position=objectivePos; objective.sortingOrder=Sort(objectivePos); }
            if(ObjectiveComplete() && remaining==0 && enemies.Count==0) CompleteMission();
        }
        Vector2 MoveBody(Vector2 position,Vector2 delta)
        {
            Vector2 p=ArenaRules.Clamp(position+delta,.4f);
            foreach(var o in obstacles)
            {
                Vector2 away=p-o.pos; float r=o.radius+.35f;
                if(away.sqrMagnitude<r*r) p=o.pos+(away.sqrMagnitude<.001f?Vector2.down:away.normalized)*r;
            }
            return ArenaRules.Clamp(p,.4f);
        }
        void Animate(SpriteRenderer v,int row,Vector2 p,bool moving,bool attacking,float direction)
        {
            v.transform.position=p; v.sortingOrder=Sort(p);
            v.sprite=MedievalArt.Warrior(row,attacking?3:moving?1+(int)(elapsed*7)%2:0);
            var motion=v.GetComponent<WarriorMotion>();if(motion!=null){motion.moving=moving;motion.attacking=attacking;}
            if(Mathf.Abs(direction)>.05f) v.flipX=direction<0;
        }
        bool Spend(float amount)
        { if(stamina<amount) return false; stamina-=amount; regenDelay=.35f; return true; }
        void Dodge(Vector2 direction)
        {
            if(dodgeCooldown>0 || !Spend(25)) return;
            dodgeDirection=direction.sqrMagnitude>.01f?direction:facing; dodgeTimer=.19f; dodgeCooldown=.75f; hurtTimer=.28f; sound.Play("dash");
        }
        void Attack(bool heavy)
        {
            if(mode!=Mode.Playing || attackTimer>0 || dodgeTimer>0 || !Spend(heavy?28:8)) return;
            attackTimer=heavy?.8f:.38f; attackPose=.23f; blocking=false;
            float range=reach+(heavy?.3f:0), cone=heavy?-.15f:faction==0?.55f:.1f;
            Swing(playerPos,facing,range,cone,new Color(.9f,.8f,.5f,.65f));
            for(int i=enemies.Count-1;i>=0;i--)
            {
                var e=enemies[i]; Vector2 to=e.pos-playerPos;
                if(to.magnitude<=range && (to.sqrMagnitude<.1f||Vector2.Dot(facing,to.normalized)>=cone))
                {
                    e.hp-=damage*(heavy?(faction==2?2.3f:1.8f):1); e.flash=.12f;
                    e.pos=MoveBody(e.pos,to.normalized*(heavy?.6f:.2f));
                    if(e.hp<=0) Kill(e);
                }
            }
            sound.Play("shot");
        }
        void Swing(Vector2 p,Vector2 dir,float range,float cone,Color color)
        {
            float center=Mathf.Atan2(dir.y,dir.x)*Mathf.Rad2Deg, width=Mathf.Acos(cone)*Mathf.Rad2Deg;
            for(int i=0;i<9;i++)
            {
                float angle=center-width+i*width/4;
                Vector2 d=new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad));
                var v=NeonVisual.Make("Blade trail","square",color,p+d*range*.7f,new Vector2(.26f,.045f),200,effects);
                v.transform.rotation=Quaternion.Euler(0,0,angle+90); Destroy(v.gameObject,.13f);
            }
        }
        void SpawnEnemy()
        {
            int kind=wave==5&&remaining==1?3:wave>=3&&remaining%5==0?2:remaining%4==0?1:0;
            Vector2 p=new Vector2(Random.value<.5f?-11.8f:11.8f,Random.Range(-3.5f,2.5f));
            var e=new Enemy { kind=kind,pos=p,hp=kind==3?440:kind==2?120:kind==1?42:52,speed=kind>=2?1.25f:kind==1?1.65f:1.9f,timer=1 };
            e.maxHp=e.hp; e.view=Actor(kind==3?"Keeper of the chapel":"Enemy",kind==1?4:kind>=2?5:3,p,kind==3?1.45f:1);
            e.warningView=NeonVisual.Make("Attack telegraph","ring",new Color(.9f,.35f,.15f,.65f),p,new Vector2(kind==3?3:1.8f,kind==3?1.3f:.8f),-2,actors);
            e.warningView.enabled=false; enemies.Add(e);
        }
        void UpdateEnemies(float dt)
        {
            foreach(var e in enemies)
            {
                e.timer-=dt; e.flash-=dt;
                Vector2 target=playerPos;
                bool siege=objective!=null && Vector2.Distance(e.pos,objectivePos)<Vector2.Distance(e.pos,playerPos)-1;
                if(siege) target=objectivePos;
                Vector2 to=target-e.pos; float distance=to.magnitude;
                if(e.winding)
                {
                    e.warning-=dt;
                    if(e.warning<=0)
                    {
                        e.winding=false; e.warningView.enabled=false; e.timer=e.kind==3?1.5f:1.05f;
                        if(e.kind==1) ShootArrow(e);
                        else
                        {
                            float range=e.kind==3?2.3f:1.4f;
                            Swing(e.pos,e.direction,range,.15f,new Color(.8f,.25f,.15f,.6f));
                            if(e.objectiveTarget && objective!=null && Vector2.Distance(e.pos,objectivePos)<range+1) DamageObjective(e.kind>=2?18:10);
                            else if(Vector2.Distance(e.pos,playerPos)<range && Vector2.Dot(e.direction,(playerPos-e.pos).normalized)>.05f) Hurt(e.kind==3?30:e.kind==2?22:12,e.pos);
                            if(mode!=Mode.Playing) return;
                        }
                    }
                }
                else
                {
                    float attackRange=e.kind==1?6:e.kind==3?1.9f:1.15f;
                    if(distance<attackRange && e.timer<=0)
                    { e.winding=true; e.warning=e.kind==3?.95f:e.kind==1?.65f:.55f; e.direction=to.normalized; e.objectiveTarget=siege; e.warningView.enabled=true; }
                    else
                    {
                        Vector2 move=distance>attackRange*.8f?to.normalized:e.kind==1&&distance<3?-to.normalized:Vector2.zero;
                        foreach(var other in enemies) if(other!=e)
                        { Vector2 away=e.pos-other.pos; if(away.sqrMagnitude<.65f&&away.sqrMagnitude>.001f) move+=away.normalized*.7f; }
                        e.pos=MoveBody(e.pos,Vector2.ClampMagnitude(move,1)*e.speed*dt);
                    }
                }
                Animate(e.view,e.kind==1?4:e.kind>=2?5:3,e.pos,!e.winding,e.winding,to.x);
                e.view.color=e.flash>0?new Color(1,.5f,.4f):Color.white;
                e.warningView.transform.position=e.pos;
            }
        }
        void Hurt(float amount,Vector2 source)
        {
            if(mode!=Mode.Playing || hurtTimer>0) return;
            if(blocking && Vector2.Dot(facing,(source-playerPos).normalized)>.15f && Spend(amount*(faction==1?.45f:.8f)))
            { amount*=.12f; notice="Удар принят щитом"; noticeTime=.7f; }
            health=Mathf.Max(0,health-amount); hurtTimer=.45f; sound.Play("hit");
            if(health<=0) Finish(false);
        }
        void DamageObjective(float amount)
        { objectiveHealth=Mathf.Max(0,objectiveHealth-amount); if(objectiveHealth<=0) { notice=wave==2?"Обоз потерян":"Ворота пали"; Finish(false); } }
        void ShootArrow(Enemy e)
        {
            var a=new Arrow{pos=e.pos+Vector2.up*.5f,velocity=e.direction*7};
            a.view=NeonVisual.Make("Arrow","square",new Color(.72f,.56f,.29f),a.pos,new Vector2(.42f,.055f),180,actors);
            a.view.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(a.velocity.y,a.velocity.x)*Mathf.Rad2Deg); arrows.Add(a);
        }
        void UpdateArrows(float dt)
        {
            for(int i=arrows.Count-1;i>=0;i--)
            {
                var a=arrows[i]; Vector2 next=a.pos+a.velocity*dt; a.life-=dt;
                if(ArenaRules.SegmentHits(a.pos,next,playerPos+Vector2.up*.4f,.45f)) { Hurt(10,a.pos-a.velocity.normalized); a.life=0; }
                else if(objective!=null && ArenaRules.SegmentHits(a.pos,next,objectivePos+Vector2.up*.5f,.8f)) { DamageObjective(7); a.life=0; }
                else foreach(var o in obstacles) if(ArenaRules.SegmentHits(a.pos,next,o.pos,o.radius)) { a.life=0; break; }
                a.pos=next; a.view.transform.position=next;
                if(a.life<=0) { Destroy(a.view.gameObject); arrows.RemoveAt(i); }
                if(mode!=Mode.Playing) return;
            }
        }
        void Kill(Enemy e)
        {
            if(!enemies.Remove(e)) return;
            kills++; score+=e.kind==3?500:50; DropGear(e.pos);
            var c=new Coin {pos=e.pos,value=e.kind==3?50:5+e.kind*3};
            c.view=NeonVisual.Make("Silver","circle",new Color(.8f,.82f,.76f),c.pos,Vector2.one*.2f,160,actors); coins.Add(c);
            Destroy(e.view.gameObject); Destroy(e.warningView.gameObject);
        }
        void UpdateCoins(float dt)
        {
            for(int i=coins.Count-1;i>=0;i--)
            {
                var c=coins[i]; float d=Vector2.Distance(playerPos,c.pos);
                if(d<2) c.pos=Vector2.MoveTowards(c.pos,playerPos,dt*6);
                c.view.transform.position=c.pos;
                if(d<.5f) { AddSilver(c.value); Destroy(c.view.gameObject); coins.RemoveAt(i); }
            }
        }
        void UpdateAlly(float dt)
        {
            if(ally==null) return;
            allyTimer-=dt; Enemy target=null; float closest=5;
            foreach(var e in enemies) { float d=Vector2.Distance(allyPos,e.pos); if(d<closest) {closest=d;target=e;} }
            Vector2 destination=target!=null?target.pos:playerPos+Vector2.left;
            Vector2 dir=destination-allyPos; allyPos=MoveBody(allyPos,dir.normalized*Mathf.Min(dir.magnitude,3*dt));
            bool attack=target!=null&&closest<1.7f&&allyTimer<=0;
            Animate(ally,6,allyPos,dir.magnitude>.3f,attack,dir.x);
            if(attack) { allyTimer=.9f; target.hp-=24; if(target.hp<=0) Kill(target); }
        }
        void Rescue()
        {
            if(wave!=3) return;
            for(int i=captives.Count-1;i>=0;i--) if(Vector2.Distance(playerPos,captives[i].transform.position)<1.5f)
            { Destroy(captives[i].gameObject); captives.RemoveAt(i); rescued++; AddSilver(10); notice="Пленный освобождён"; noticeTime=2; }
        }
        bool ObjectiveComplete() => wave==2?objectivePos.x>=8.9f&&objectiveHealth>0:wave==3?rescued==3:wave==4?missionTime>=35&&objectiveHealth>0:true;
        void CompleteMission()
        {
            foreach(var c in coins) { AddSilver(c.value); Destroy(c.view.gameObject); } coins.Clear();
            AddSilver(25*wave); sound.Play("upgrade");
            if(wave==5) Finish(true); else mode=Mode.Story;
        }
        void Finish(bool victory)
        {
            if(savedResult) return; savedResult=true;
            mode=victory?Mode.Victory:Mode.Defeat;
            profile.bestScore=Mathf.Max(profile.bestScore,score);
            if(victory) profile.completedFactions|=1<<faction;
            Save(); sound.Play(victory?"upgrade":"lose"); Debug.Log(victory?"ASKICH_VICTORY":"ASKICH_DEFEAT");
        }
        void Save() { if(persistenceEnabled) profile.Save(); }
        void AddSilver(int value) { if(value<=0) return; profile.credits+=value; runCredits+=value; Save(); }
        Color SkinColor() => profile.selectedSkin==1?new Color(.82f,.91f,1):profile.selectedSkin==2?new Color(1,.86f,.62f):Color.white;
    }
}
