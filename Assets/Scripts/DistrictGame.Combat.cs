using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Askich {
 public sealed partial class DistrictGame {
  struct Strike {
   public float duration,impact,range,dot,damage,cost,stagger,push;
   public int targets;
   public Strike(float duration,float phase,float range,float dot,float damage,float cost,float stagger,float push,int targets){this.duration=duration;impact=duration*phase;this.range=range;this.dot=dot;this.damage=damage;this.cost=cost;this.stagger=stagger;this.push=push;this.targets=targets;}
  }
  // Contact phases come from sampling the authored hand curves (DistrictCombatProbe).
  static Strike WeaponStrike(int weapon)=>weapon==1?new Strike(.85f,.36f,1.85f,.35f,45,18,.5f,.3f,2):weapon==2?new Strike(.8f,.4f,2.7f,.86f,30,14,.35f,.22f,1):new Strike(.55f,.47f,1.35f,.6f,10,8,.22f,.12f,1);
  const float EnemyDuration=1.2f,EnemyImpact=EnemyDuration*.47f;
  const float CrawlerRange=1.85f,CrawlerTravel=.8f,CrawlerReach=1.08f,CrawlerGrip=.99f;
  const float CrawlerBrace=.54f,CrawlerContact=.76f,CrawlerEnd=1.68f,CrawlerHalfAngle=.53f;
  static bool IsCrawler(Person enemy)=>enemy.id%3==2;
  static float ImpactTime(Person enemy)=>IsCrawler(enemy)?CrawlerContact:EnemyImpact;
  static float LungeFraction(float remaining)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(CrawlerBrace,CrawlerContact,CrawlerContact-remaining));
  Vector2 heroVelocity,heroStrikeDirection;
  float heroStagger;
  int strikeSequence;
  Strike activeStrike;
  readonly Dictionary<Person,LineRenderer> attackCues=new Dictionary<Person,LineRenderer>();
  Material attackCueMaterial;MaterialPropertyBlock attackCueTint;

  Vector2 TouchStrikeDirection(){
   float reach=WeaponStrike(weapon).range;
   Vector2 aim=face;
   foreach(var enemy in infected){
    Vector2 toEnemy=enemy.p-hero.p;
    float distance=toEnemy.magnitude;
    if(enemy.hp<=0||distance>=reach||distance<.001f||!Sight(hero.p,enemy.p))continue;
    reach=distance;
    aim=toEnemy/distance;
   }
   return aim;
  }
  bool TryAttack(Vector2 direction){
   var strike=WeaponStrike(weapon);
   if(!playing||paused||dead||suppliesOpen||attack>0||heroStagger>0||stamina<strike.cost)return false;
   CancelMedical();activeStrike=strike;attack=strike.duration;stamina-=strike.cost;heroVelocity=Vector2.zero;searchSlot=-1;
   heroStrikeDirection=DrunkDirection(direction.sqrMagnitude>.001f?direction.normalized:Vector2.up);
   hero.root.rotation=Quaternion.LookRotation(V(heroStrikeDirection));
   if(hero.visual)hero.visual.Attack(weapon,strike.duration);
   Alert(hero.p,weapon==0?6:13);StartCoroutine(Hit(heroStrikeDirection,strike,++strikeSequence));return true;
  }
  IEnumerator Hit(Vector2 direction,Strike strike,int sequence){
   int generation=worldGeneration;float t=0;
   while(t<strike.impact){yield return null;if(!paused)t+=Mathf.Min(Time.deltaTime,.05f);if(dead||won||generation!=worldGeneration||sequence!=strikeSequence)yield break;}
   sound.Play("shot");ResolveStrike(direction,strike);
  }
  int ResolveStrike(Vector2 direction,Strike strike){
   var candidates=new List<Person>();
   foreach(var z in infected)if(z.hp>0&&Vector2.Distance(z.p,hero.p)<strike.range&&Vector2.Dot((z.p-hero.p).normalized,direction)>strike.dot&&Sight(hero.p,z.p))candidates.Add(z);
   candidates.Sort((a,b)=>(a.p-hero.p).sqrMagnitude.CompareTo((b.p-hero.p).sqrMagnitude));
   int hits=Mathf.Min(strike.targets,candidates.Count);
   for(int i=0;i<hits;i++){
    var z=candidates[i];z.hp-=strike.damage;z.striking=false;z.wind=0;z.recovery=0;z.stagger=strike.stagger;z.rest=Mathf.Max(z.rest,strike.stagger+.35f);
    Vector2 push=(z.p-hero.p).normalized*strike.push/4;
    for(int n=0;n<4;n++)z.p=Move(z.p,push);
    z.root.position=V(z.p);
    if(z.hp<=0){if(z.visual)z.visual.Die();else z.root.rotation=Quaternion.Euler(0,0,85);}else if(z.visual)z.visual.Hurt();
   }
   if(hits>0)sound.Play("hit");return hits;
  }
  void BeginEnemyAttack(Person z){
   z.striking=true;z.wind=ImpactTime(z);z.strikeOrigin=z.p;
   if(IsCrawler(z))z.lungeTravel=Mathf.Clamp(Vector2.Distance(hero.p,z.p)-CrawlerGrip,0,CrawlerTravel);
   z.strikeDirection=(hero.p-z.p).normalized;
   if(z.strikeDirection.sqrMagnitude<.01f)z.strikeDirection=Vector2.up;
   z.root.rotation=Quaternion.LookRotation(V(z.strikeDirection));
   if(z.visual)z.visual.Attack(0,IsCrawler(z)?CrawlerEnd:EnemyDuration);
  }
  bool TickEnemyAttack(Person z,float dt){
   bool crawler=IsCrawler(z);
   if(crawler&&(z.striking||z.recovery>0))z.root.rotation=Quaternion.LookRotation(V(z.strikeDirection));
   if(z.stagger>0||z.recovery>0){
    z.stagger=Mathf.Max(0,z.stagger-dt);
    z.recovery=Mathf.Max(0,z.recovery-dt);
    if(crawler&&z.visual&&z.stagger==0)z.visual.SampleLunge(CrawlerEnd-z.recovery);
    Animate(z,Vector2.zero,true,.1f);
    return true;
   }
   if(!z.striking)return false;
   float before=z.wind;
   z.wind-=dt;
   if(IsCrawler(z)){
    float advance=z.lungeTravel*(LungeFraction(z.wind)-LungeFraction(before));
    var destination=z.p+advance*z.strikeDirection;
    if(!Blocked(destination))z.p=destination;
    if(z.visual)z.visual.SampleLunge(CrawlerContact-Mathf.Max(z.wind,0));
   }
   if(z.wind<=0){
    z.striking=false;z.recovery=IsCrawler(z)?CrawlerEnd-CrawlerContact:EnemyDuration-EnemyImpact;z.rest=1.2f;
    Vector2 offset=hero.p-z.p;
    Vector2 fromOrigin=hero.p-z.strikeOrigin;
    bool contact=IsCrawler(z)
     ? offset.magnitude<CrawlerReach&&fromOrigin.magnitude<CrawlerRange&&Vector2.Dot(fromOrigin.normalized,z.strikeDirection)>Mathf.Cos(CrawlerHalfAngle)
     : offset.magnitude<1.55f&&Vector2.Dot(offset.normalized,z.strikeDirection)>.55f;
    if(contact&&damageDelay<=0&&Sight(z.p,hero.p)){
     health-=EnemyDamage(z);WoundHero();damageDelay=.8f;heroStagger=.28f;heroVelocity=Vector2.zero;attack=0;strikeSequence++;searchSlot=-1;sound.Play("hit");
     if(hero.visual)hero.visual.Hurt();
     if(health<=0){health=0;dead=true;if(hero.visual)hero.visual.Die();else hero.root.rotation=Quaternion.Euler(0,0,80);}
    }
   }
   Animate(z,Vector2.zero,true,.3f);return true;
  }
  void UpdateAttackCues(){
   if(attackCueTint==null)attackCueTint=new MaterialPropertyBlock();
   foreach(var z in infected){
    bool visible=z.hp>0&&z.striking&&Vector2.Distance(z.p,hero.p)<12&&Sight(hero.p,z.p);
    if(!attackCues.TryGetValue(z,out var line)){
     if(!visible)continue;
     if(!attackCueMaterial){var shader=Resources.Load<Shader>("Survival/AttackCue");attackCueMaterial=new Material(shader?shader:Shader.Find("Standard"));attackCueMaterial.color=new Color(1,.55f,.15f,.9f);}
     line=new GameObject("Infected attack warning").AddComponent<LineRenderer>();line.sharedMaterial=attackCueMaterial;line.useWorldSpace=true;line.loop=true;line.positionCount=19;line.widthMultiplier=.045f;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;attackCues[z]=line;
    }
    line.enabled=visible;if(!visible)continue;
    var origin=z.strikeOrigin;float reach=IsCrawler(z)?CrawlerRange:1.55f;float halfAngle=IsCrawler(z)?CrawlerHalfAngle:.98f;
    line.SetPosition(0,V(origin,.09f));float yaw=Mathf.Atan2(z.strikeDirection.x,z.strikeDirection.y);
    for(int i=0;i<18;i++){float a=yaw+Mathf.Lerp(-halfAngle,halfAngle,i/17f);line.SetPosition(i+1,V(origin+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*reach,.09f));}
    float progress=1-z.wind/ImpactTime(z);line.widthMultiplier=Mathf.Lerp(.04f,.085f,progress);attackCueTint.SetColor("_Color",Color.Lerp(new Color(1,.7f,.2f,.9f),new Color(1,.28f,.12f,1),progress));line.SetPropertyBlock(attackCueTint);
   }
  }
  void DrawCombatHint(){if(attack<=0)return;Panel(new Rect(470,588,300,34));GUI.Label(new Rect(483,591,285,30),attack>activeStrike.duration-activeStrike.impact?"Замах · направление зафиксировано":"Возврат оружия",small);}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  IEnumerator CheckCombat(Action<bool,string> check,string folder){
   foreach(var e in infected){e.hp=0;e.striking=false;e.stagger=e.recovery=0;}
   var enemy=infected.Find(e=>e.id%3==0);bleeding=false;infection=0;hero.p=new Vector2(-24,-24);heroVelocity=Vector2.zero;health=100;damageDelay=0;attack=0;heroStagger=0;weapon=1;stamina=100;
   enemy.hp=enemy.maxHp;enemy.visual.Revive();enemy.p=hero.p+Vector2.up*1.3f;enemy.alert=0;enemy.rest=100;enemy.stagger=100;
   check(hero.visual.NativeEquipmentReady&&hero.visual.Equip(1),"authored axe and spear meshes available with original skin binding");
   check(TryAttack(Vector2.up),"axe attack starts with enough stamina");float before=enemy.hp;
   yield return new WaitForSeconds(.14f);check(enemy.hp==before,"windup causes no early damage");
   paused=true;float remaining=attack;yield return new WaitForSeconds(.45f);check(enemy.hp==before&&Mathf.Abs(attack-remaining)<.001f,"pause freezes attack timer and prevents delayed damage");paused=false;face=Vector2.down;
   yield return new WaitForSeconds(.23f);check(enemy.hp==before-45,"axe damage lands at sampled swing phase in committed direction");
   check(enemy.stagger>0&&!enemy.striking,"hit interrupts enemy and applies stagger");
   check(!TryAttack(Vector2.up),"attack recovery prevents attack spam");
   yield return new WaitForSeconds(.6f);enemy.hp=enemy.maxHp;enemy.p=hero.p+new Vector2(1.4f,1.3f);enemy.stagger=100;
   check(ResolveStrike(Vector2.up,WeaponStrike(2))==0,"spear rejects off axis target");
   enemy.p=hero.p+Vector2.up*2.3f;check(ResolveStrike(Vector2.up,WeaponStrike(2))==1,"spear reaches beyond axe distance");
   enemy.hp=enemy.maxHp;enemy.p=new Vector2(-24,-23);hero.p=new Vector2(-24,-24);enemy.stagger=enemy.recovery=0;enemy.rest=100;BeginEnemyAttack(enemy);hero.p=enemy.p+Vector2.up;float hp=health;TickEnemyAttack(enemy,EnemyImpact+.01f);check(health==hp,"stepping behind committed enemy attack avoids damage");
   enemy.recovery=0;hero.p=enemy.p+Vector2.down;BeginEnemyAttack(enemy);TickEnemyAttack(enemy,EnemyImpact+.01f);check(health<=hp-EnemyDamage(enemy)&&heroStagger>0,"enemy contact damages and interrupts at impact phase");
   bleeding=false;infection=0;heroStagger=0;attack=0;stamina=0;check(!TryAttack(Vector2.up),"exhaustion prevents attack");stamina=100;
   // Real input into a closed door: locomotion must stop when position cannot advance.
   enemy.hp=0;enemy.visual.Die();var h=homes[0];h.open=false;SnapDoor(h);hero.p=h.p+Vector2.down*5;enemy.hp=enemy.maxHp;enemy.p=h.p+Vector2.down*3.5f;check(ResolveStrike(Vector2.up,WeaponStrike(1))==0,"closed door blocks melee damage");enemy.hp=0;hero.p=h.p+new Vector2(0,-4.49f);heroVelocity=Vector2.zero;
   var keys=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
   UnityEngine.InputSystem.InputSystem.QueueStateEvent(keys,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.A));
   yield return new WaitForSeconds(.45f);check(hero.visual.State=="Idle","blocked movement no longer plays walking animation");
   UnityEngine.InputSystem.InputSystem.QueueStateEvent(keys,new UnityEngine.InputSystem.LowLevel.KeyboardState());yield return null;
   noticeTime=0;hero.p=new Vector2(-24,-24);heroVelocity=Vector2.zero;heroStagger=0;enemy.hp=enemy.maxHp;enemy.visual.Revive();enemy.p=hero.p+Vector2.up*1.25f;enemy.stagger=enemy.recovery=0;enemy.root.position=V(enemy.p);enemy.rest=100;zoom=7;
   yield return new WaitForSeconds(.8f);BeginEnemyAttack(enemy);yield return new WaitForSeconds(.2f);check(attackCueMaterial&&attackCueMaterial.shader.name=="Berezki/AttackCue"&&attackCueMaterial.shader.isSupported,"attack warning shader survives Windows build");yield return new WaitForEndOfFrame();
   var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,"combat-warning.png"),image.EncodeToPNG());Destroy(image);
   enemy.striking=false;enemy.stagger=100;enemy.p=hero.p+Vector2.up*1.6f;attack=0;heroStagger=0;weapon=1;TryAttack(Vector2.up);yield return new WaitForSeconds(.27f);yield return new WaitForEndOfFrame();
   image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,"combat-swing.png"),image.EncodeToPNG());Destroy(image);
  }
#endif
 }
}
