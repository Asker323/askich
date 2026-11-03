using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
namespace Askich {
// Independent prototype scene. Never reads or writes the campaign profile.
public sealed class SurvivalGame:MonoBehaviour {
 sealed class House { public Vector2 pos; public string name; public bool open; public string[] loot; public bool[] searched=new bool[3]; public SpriteRenderer roof; }
 sealed class Infected { public Vector2 pos,goal; public float hp=60,windup,cooldown,alert,phase; public bool striking; public SpriteRenderer view; }
 readonly List<House> homes=new List<House>(); readonly List<Infected> zombies=new List<Infected>();
 readonly MenuPointer pointer=new MenuPointer(); readonly TouchControls touch=new TouchControls();
 Transform world,interior,actors; Camera cam; SpriteRenderer player,tool,toolHead; SynthAudio audioFx;
 Vector2 pos=new Vector2(-10,-5),face=Vector2.right,velocity; int inside=-1,weapon,wood,food,bandages,secured,kills;
 float hp=100,energy=100,timeLeft=180,attackClock,attackAge,hurt,searchTime,noticeTime,noiseLife;
 int searching=-1; bool requestedAttack,hitPending,started,paused,ended,won; string notice=""; Vector2 noisePos;
 GUIStyle label,title,small; string objective=>secured==1?"Убежище укреплено":weapon==0?"Найди оружие: ищи мастерскую у восточной дороги":wood<2?"Собери 2 доски для укрепления убежища":food<1?"Найди припасы в домах":"Вернись к южному двору и укрепи его (E)";
 void Awake(){EnhancedTouchSupport.Enable();Application.targetFrameRate=60; cam=new GameObject("Survival camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.orthographic=true;cam.orthographicSize=8.1f;cam.backgroundColor=new Color(.035f,.045f,.04f);cam.transform.position=new Vector3(0,0,-10); world=new GameObject("Village").transform;interior=new GameObject("Interior").transform;actors=new GameObject("Survivors").transform; audioFx=gameObject.AddComponent<SynthAudio>();
  MedievalArt.Make("Ground",MedievalArt.Ground(false),Vector2.zero,1,world,-100);
  AddHome(new Vector2(-8,3),"Дом травницы",new[]{"Бинт","Еда","Доска"}); AddHome(new Vector2(0,3.6f),"Пустой двор",new[]{"Пусто","Доска","Пусто"});AddHome(new Vector2(8,3),"Мастерская",new[]{"Топор","Доска","Пусто"});AddHome(new Vector2(-6,-3),"Дом у колодца",new[]{"Еда","Бинт","Пусто"});AddHome(new Vector2(6,-3),"Сторожка",new[]{"Копьё","Пусто","Доска"});
  for(int i=0;i<12;i++)MedievalArt.Make("Tree",MedievalArt.Prop(i%2==0?3:4),new Vector2(i%2==0?-12:12,-5+i/2*1.9f),3.1f,world,130-i*18);
  MedievalArt.Make("Well",MedievalArt.Prop(6),new Vector2(-2,-1),1.5f,world,105);
  DrawRect("Shelter ground",new Vector2(0,-5.7f),new Vector2(4,2),new Color(.30f,.25f,.15f),world,-50);
  for(int i=0;i<3;i++){Vector2 c=new Vector2(-3+i*3,1.5f);DrawRect("Cupboard",c,new Vector2(1.2f,.6f),new Color(.36f,.23f,.12f),interior,80);}
  DrawRect("Floor",Vector2.zero,new Vector2(15,9),new Color(.22f,.18f,.13f),interior,-90);DrawRect("Back wall",new Vector2(0,4.5f),new Vector2(16,.5f),new Color(.12f,.13f,.11f),interior,70);
  player=MedievalArt.Make("Unarmed survivor",AnimatedArt.Villager(0,0),pos,1.1f,actors,100);tool=DrawRect("Equipped tool",Vector2.zero,new Vector2(.06f,.7f),new Color(.7f,.6f,.4f),actors,110);tool.enabled=false;toolHead=DrawRect("Tool head",Vector2.zero,new Vector2(.3f,.15f),new Color(.6f,.65f,.63f),actors,111);toolHead.enabled=false;
  for(int i=0;i<9;i++){Vector2 p=new Vector2(-9+i*2.2f,i%2==0?-.2f:6);var z=new Infected{pos=p,goal=p,phase=i*1.8f};z.view=MedievalArt.Make("Infected",AnimatedArt.Villager(1,0),p,1.15f,actors,100);z.view.color=new Color(.48f,.62f,.39f);zombies.Add(z);}
  interior.gameObject.SetActive(false);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-survival-check")>=0)StartCoroutine(CheckPrototype());
#endif
 }
 SpriteRenderer DrawRect(string n,Vector2 p,Vector2 size,Color color,Transform parent,int sort)=>NeonVisual.Make(n,"square",color,p,size,sort,parent);
 void AddHome(Vector2 p,string n,string[] loot){var h=new House{pos=p,name=n,loot=loot};h.roof=MedievalArt.Make(n,MedievalArt.House(0,0),p,3.3f,world,100-Mathf.RoundToInt(p.y*10));DrawRect("Door",p+new Vector2(0,-.15f),new Vector2(.5f,.55f),new Color(.08f,.06f,.04f),world,h.roof.sortingOrder+1);homes.Add(h);}
 void Say(string s){notice=s;noticeTime=4;}
 void Noise(Vector2 p,float radius){noisePos=p;noiseLife=1;foreach(var z in zombies)if(z.hp>0&&Vector2.Distance(z.pos,p)<radius){z.goal=p;z.alert=6;}}
 public static bool Blocks(Vector2 p,Vector2 house)=>p.x>house.x-1.45f&&p.x<house.x+1.45f&&p.y>house.y+.2f&&p.y<house.y+2.3f;
 Vector2 Move(Vector2 p,Vector2 delta){Vector2 q=p+delta;if(inside>=0)return new Vector2(Mathf.Clamp(q.x,-6.5f,6.5f),Mathf.Clamp(q.y,-3.5f,3.5f));q=new Vector2(Mathf.Clamp(q.x,-11.3f,11.3f),Mathf.Clamp(q.y,-6.6f,6.4f));foreach(var h in homes)if(Blocks(q,h.pos))return p;return q;}
 void Update(){pointer.Read();touch.Read();ScreenLayout.Fit(cam);var k=Keyboard.current;bool esc=k!=null&&k.escapeKey.wasPressedThisFrame;if(esc||touch.pause){paused=!paused;searching=-1;touch.Reset();}if(!started||paused||ended)return;
  float dt=Mathf.Min(Time.deltaTime,.05f);timeLeft-=dt;noticeTime-=dt;hurt-=dt;noiseLife-=dt;attackClock-=dt;
  if(timeLeft<=0){ended=true;Say("Ночь застала тебя без защиты.");return;}
  Vector2 input=touch.move;if(!TouchControls.Enabled&&k!=null){input=new Vector2((k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0),(k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0));}
  bool crouch=k!=null&&k.leftCtrlKey.isPressed,sprint=k!=null&&k.leftShiftKey.isPressed&&energy>5&&!crouch;float speed=crouch?1.3f:sprint?4.2f:2.6f;energy=Mathf.Clamp(energy+(sprint&&input.sqrMagnitude>0?-22:13)*dt,0,100);
  velocity=Vector2.MoveTowards(velocity,Vector2.ClampMagnitude(input,1)*speed,dt*18);Vector2 before=pos;pos=Move(pos,velocity*dt);if(input.sqrMagnitude>.01f)face=input.normalized;
  if(!TouchControls.Enabled&&Mouse.current!=null){Vector2 m=cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());if((m-pos).sqrMagnitude>.1f)face=(m-pos).normalized;}
  if(sprint&&inside<0&&input.sqrMagnitude>0)Noise(pos,6);
  if((pos-before).sqrMagnitude>.0001f)searching=-1;
  bool interact=touch.interact||(k!=null&&k.eKey.wasPressedThisFrame);if(interact)Interact();
  if(searching>=0){searchTime+=dt;if(searchTime>=1.6f){int slot=searching;searching=-1;Search(slot);}}
  if((requestedAttack||touch.attack||(!TouchControls.Enabled&&Mouse.current!=null&&Mouse.current.leftButton.isPressed))&&attackClock<=0){searching=-1;attackClock=weapon==1?.9f:weapon==2?.65f:.6f;attackAge=0;hitPending=true;Noise(inside<0?pos:homes[inside].pos,weapon==0?3:7);audioFx.Play("shot");}
  requestedAttack=false;
  if(hitPending){attackAge+=dt;if(attackAge>.22f){hitPending=false;if(inside<0)foreach(var z in zombies)if(z.hp>0&&Vector2.Distance(pos,z.pos)<(weapon==2?2.2f:1.35f)&&Vector2.Dot((z.pos-pos).normalized,face)>.25f){z.hp-=weapon==0?8:weapon==1?42:28;z.pos=Move(z.pos,face*.4f);z.striking=false;z.cooldown=.6f;audioFx.Play("hit");if(z.hp<=0){kills++;z.view.transform.rotation=Quaternion.Euler(0,0,80);z.view.color=new Color(.25f,.24f,.18f);}}}}
  if(k!=null&&k.bKey.wasPressedThisFrame&&bandages>0&&hp<100){bandages--;hp=Mathf.Min(100,hp+30);Say("Рана перевязана: +30 здоровья");}
  foreach(var z in zombies){if(z.hp<=0||inside>=0)continue;z.cooldown-=dt;z.alert-=dt;if(inside<0&&Vector2.Distance(pos,z.pos)<(crouch?2.4f:5)){z.goal=pos;z.alert=4;}
   if(z.striking){z.windup-=dt;z.view.color=new Color(.9f,.45f,.25f);if(z.windup<=0){z.striking=false;z.cooldown=1.3f;if(inside<0&&Vector2.Distance(z.pos,pos)<1.15f&&hurt<=0){hp-=18;hurt=.8f;audioFx.Play("hit");if(hp<=0)ended=true;}}}
   else if(inside<0&&Vector2.Distance(z.pos,pos)<.95f&&z.cooldown<=0){z.striking=true;z.windup=.7f;}
   else {Vector2 target=z.alert>0?z.goal:new Vector2(Mathf.Sin(Time.time*.18f+z.phase)*8,Mathf.Cos(Time.time*.14f+z.phase)*5);z.pos=Move(z.pos,(target-z.pos).normalized*(z.alert>0?1.45f:.5f)*dt);z.view.color=new Color(.48f,.62f,.39f);}
   z.view.enabled=inside<0;z.view.transform.position=z.pos;z.view.sortingOrder=100-Mathf.RoundToInt(z.pos.y*10);z.view.sprite=AnimatedArt.Villager(1,(int)(Time.time*(z.alert>0?6:3)+z.phase)%4);z.view.flipX=z.goal.x<z.pos.x;
  }
  player.transform.position=pos;player.sortingOrder=100-Mathf.RoundToInt(pos.y*10);player.sprite=AnimatedArt.Villager(0,input.sqrMagnitude>.01f?(int)(Time.time*7)%4:0);player.flipX=face.x<0;player.color=hurt>0?new Color(1,.5f,.4f):Color.white;
  tool.enabled=weapon>0;tool.transform.position=(Vector3)(pos+face*.4f+Vector2.up*.5f);tool.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(face.y,face.x)*Mathf.Rad2Deg-90+(attackClock>0?Mathf.Sin(attackClock*12)*55:0));tool.transform.localScale=new Vector3(.07f,weapon==2?1.4f:.7f,1);tool.sortingOrder=player.sortingOrder+1;toolHead.enabled=weapon>0;toolHead.transform.position=tool.transform.position+tool.transform.up*(weapon==2?.65f:.28f);toolHead.transform.rotation=tool.transform.rotation;toolHead.transform.localScale=weapon==2?new Vector3(.13f,.3f,1):new Vector3(.34f,.18f,1);toolHead.sortingOrder=tool.sortingOrder+1;
 }
 int NearbyHome(){for(int i=0;i<homes.Count;i++)if(Vector2.Distance(pos,homes[i].pos)<1.4f)return i;return -1;}
 void Interact(){if(inside>=0){if(pos.y< -2.5f){int exit=inside;inside=-1;pos=homes[exit].pos+Vector2.down*.7f;world.gameObject.SetActive(true);interior.gameObject.SetActive(false);foreach(var z in zombies)z.view.enabled=true;Say("Ты вышел во двор");return;}for(int i=0;i<3;i++)if(Vector2.Distance(pos,new Vector2(-3+i*3,1.5f))<1.6f){if(homes[inside].searched[i])Say("Уже обыскано");else{searching=i;searchTime=0;Say("Обыск… Не двигайся");Noise(homes[inside].pos,4);}return;}Say("Подойди к шкафу или к выходу на юге");return;}
  if(Vector2.Distance(pos,new Vector2(0,-5.7f))<1.8f){if(wood>=2&&food>=1&&weapon>0){wood-=2;food--;secured=1;won=ended=true;audioFx.Play("upgrade");Say("Дверь укреплена. Ты переживёшь эту ночь.");}else Say("Для убежища нужны оружие, 2 доски и еда");return;}
  int n=NearbyHome();if(n<0){Say("Подойди к двери дома (E)");return;}if(!homes[n].open){homes[n].open=true;Noise(pos,5);}inside=n;pos=new Vector2(0,-3);world.gameObject.SetActive(false);interior.gameObject.SetActive(true);foreach(var z in zombies)z.view.enabled=false;Say(homes[n].name+": осмотри три шкафа. Выход — на юге");
 }
 void Search(int slot){var h=homes[inside];if(h.searched[slot])return;h.searched[slot]=true;string item=h.loot[slot];if(item=="Топор")weapon=1;if(item=="Копьё")weapon=2;if(item=="Доска")wood++;if(item=="Еда")food++;if(item=="Бинт")bandages++;Say(item=="Пусто"?"Ничего полезного": "Найдено: "+item);audioFx.Play("upgrade");}
 void OnApplicationPause(bool value){if(value){paused=true;searching=-1;pointer.Reset();touch.Reset();}}
 void OnApplicationFocus(bool value){if(!value&&!Application.runInBackground)OnApplicationPause(true);}
 void OnDestroy(){EnhancedTouchSupport.Disable();}
 void Box(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
 bool Button(Rect r,string text){Box(r,new Color(.25f,.28f,.20f));GUI.Label(r,text,label);return Event.current.type==EventType.Repaint&&pointer.TakeClick(r);}
 void OnGUI(){if(label==null){label=new GUIStyle(GUI.skin.label){fontSize=20,wordWrap=true};label.normal.textColor=new Color(.92f,.9f,.77f);small=new GUIStyle(label){fontSize=16};title=new GUIStyle(label){fontSize=32,fontStyle=FontStyle.Bold};}GUI.matrix=Matrix4x4.TRS(ScreenLayout.Offset,Quaternion.identity,Vector3.one*ScreenLayout.Scale);
  Box(new Rect(16,14,1248,94),new Color(.04f,.06f,.045f,.94f));GUI.Label(new Rect(32,20,1070,30),"GOIDA · МОР В БЕРЕЖКАХ     |     "+(inside<0?"ДЕРЕВНЯ":homes[inside].name),label);GUI.Label(new Rect(32,55,1150,45),"Здоровье "+Mathf.CeilToInt(hp)+"   Силы "+Mathf.CeilToInt(energy)+"   До ночи "+Mathf.CeilToInt(timeLeft)+"с   Оружие: "+(weapon==0?"нет":weapon==1?"топор":"копьё")+"\nДоски "+wood+"/2   Еда "+food+"/1   Бинты "+bandages+" (B)",small);
  if(Button(new Rect(1150,20,100,35),paused?"Пуск":"Пауза")){paused=!paused;searching=-1;}
  if(!started||ended||paused){Box(new Rect(250,180,780,390),new Color(.045f,.065f,.05f,.97f));GUI.Label(new Rect(280,205,720,70),ended?(won?"ПЕРВАЯ НОЧЬ ПОД ЗАЩИТОЙ":"ТЕБЯ ПОГЛОТИЛ МОР"):paused?"ПАУЗА":"МОР В БЕРЕЖКАХ",title);GUI.Label(new Rect(280,285,715,170),ended?(won?"Ты собрал припасы и укрепил двор. Первый эпизод завершён.\nЭто начало прототипа выживания.":"Попробуй двигаться тише и не сражайся с толпой без оружия."):paused?"Время остановлено.":"Ты вернулся в деревню без оружия. Жители заражены. В восточной мастерской мог остаться инструмент.\nОбыщи дома, найди еду и две доски. Укрепи южный двор до темноты.\nШум привлекает заражённых. В домах пока безопасно.",label);if(Button(new Rect(280,480,340,55),ended?"Начать заново":started?"Продолжить":"Начать выживание")){if(ended){UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);return;}started=true;paused=false;}if(Button(new Rect(660,480,320,55),"Выйти"))Application.Quit();return;}
  Box(new Rect(16,115,950,38),new Color(.04f,.06f,.045f,.9f));GUI.Label(new Rect(28,118,930,35),objective,small);
  if(inside<0){foreach(var h in homes){Vector3 p=cam.WorldToScreenPoint(h.pos+Vector2.down*.35f);Vector2 ui=ScreenLayout.ToUI(p);GUI.Label(new Rect(ui.x-90,ui.y,200,40),h.name,small);}Vector2 s=ScreenLayout.ToUI(cam.WorldToScreenPoint(new Vector2(0,-5.7f)));GUI.Label(new Rect(s.x-90,s.y,240,35),"УБЕЖИЩЕ · E",label);}else{for(int i=0;i<3;i++){Vector2 s=ScreenLayout.ToUI(cam.WorldToScreenPoint(new Vector2(-3+i*3,1.5f)));GUI.Label(new Rect(s.x-65,s.y-40,180,50),homes[inside].searched[i]?"Обыскано":"Обыскать · E",small);}GUI.Label(new Rect(500,570,360,35),"Южная дверь · E",label);}
  if(searching>=0){Box(new Rect(480,520,320,15),Color.black);Box(new Rect(480,520,320*Mathf.Clamp01(searchTime/1.6f),15),new Color(.65f,.75f,.3f));}
  Box(new Rect(16,642,1248,65),new Color(.04f,.06f,.045f,.94f));GUI.Label(new Rect(30,646,1200,55),noticeTime>0?notice:"WASD — идти   Shift — бежать   Ctrl — красться   E — дверь / обыск / убежище\nМышь — направление   ЛКМ — удар (кулаки слабы)   B — бинт   Esc — пауза",small);
  if(TouchControls.Enabled){if(Button(new Rect(1040,475,180,70),"ОБЫСК / E"))Interact();if(Button(new Rect(1040,555,180,65),"БИНТ" )&&bandages>0&&hp<100){bandages--;hp=Mathf.Min(100,hp+30);}Box(new Rect(40,480,150,150),new Color(.25f,.3f,.2f,.6f));GUI.Label(new Rect(60,490,140,40),"ДВИЖЕНИЕ",small);if(Button(new Rect(840,550,170,65),"УДАР")){requestedAttack=true;}}
 }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
 IEnumerator CheckPrototype(){Application.runInBackground=true;started=true;paused=false;yield return new WaitForSeconds(.5f);var checks=new List<string>();void Check(bool c,string n){if(!c)throw new Exception(n);checks.Add(n);}Check(weapon==0,"starts unarmed");Check(homes.Exists(h=>Array.Exists(h.loot,x=>x=="Пусто")),"empty containers exist");Check(Blocks(new Vector2(-8,4),homes[0].pos),"house walls block");Noise(zombies[0].pos,2);Check(zombies[0].alert>0,"noise alerts infected");pos=homes[2].pos;Interact();Check(inside==2,"door enters interior");Search(0);Check(weapon==1,"workshop contains axe");yield return null;yield return new WaitForEndOfFrame();string folder=Environment.GetEnvironmentVariable("GOIDA_SURVIVAL_CHECK")??Application.temporaryCachePath;Directory.CreateDirectory(folder);var room=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,"interior.png"),room.EncodeToPNG());Destroy(room);Search(1);int w=wood;Search(1);Check(wood==w,"loot collected once");pos=new Vector2(0,-3);Interact();Check(inside==-1,"door exits interior");pos=homes[0].pos;Interact();Search(1);Search(2);pos=new Vector2(0,-3);Interact();pos=new Vector2(0,-5.7f);Interact();Check(won&&secured==1&&wood==0&&food==0,"supplies secure shelter");ended=false;won=false;pos=new Vector2(-1,-2);wood=2;food=1;noticeTime=0;timeLeft=150;yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,"survival.png"),t.EncodeToPNG());Destroy(t);File.WriteAllText(Path.Combine(folder,"checks.txt"),"PASS\n"+string.Join("\n",checks));Application.Quit();}
#endif
}
}
