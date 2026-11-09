using System.Collections.Generic;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  sealed class HouseWindow {public Home home;public Vector2 p;public int state;public float angle;public Barrier pane,sill;public Transform leaf;public GameObject glass;}
  readonly List<HouseWindow> houseWindows=new List<HouseWindow>();AudioClip glassBreak;
  HouseWindow breakingWindow;float glassTime,glassHealth;
  BackExit fortTarget;int fortMode;float fortTime,fortHealth,fortNoise;
  bool FortWorking=>fortTarget!=null;
  bool FortHandsBusy=>FortWorking&&fortMode!=2;
  void BuildEastWall(Home h,int id,Color tint){
   if(!HasHousePlan(id)){Wall("East wall",h.p+new Vector2(4,0),new Vector2(.3f,8),2.8f,tint);return;}
   Wall("East window wall south",h.p+new Vector2(4,-2),new Vector2(.3f,4),2.8f,tint);Wall("East window wall north",h.p+new Vector2(4,3),new Vector2(.3f,2),2.8f,tint);
   var sill=Wall("Window sill wall",h.p+new Vector2(4,1),new Vector2(.3f,2),.8f,tint);sill.blocksSight=false;
   Cube("Window upper wall",V(h.p+new Vector2(4,1),2.65f),new Vector3(.3f,.3f,2),tint);
   var w=new HouseWindow{home=h,sill=sill,p=h.p+new Vector2(4,1)};w.pane=new Barrier{rect=new Rect(w.p-new Vector2(.15f,1),new Vector2(.3f,2))};barriers.Add(w.pane);
   w.leaf=new GameObject("Hinged window frame").transform;w.leaf.position=V(h.p+new Vector2(4,0));var wood=new Color(.60f,.59f,.47f);
   foreach(float z in new[]{0f,2f})Cube("Window frame upright",new Vector3(0,1.6f,z),new Vector3(.12f,1.7f,.10f),wood,w.leaf);
   foreach(float y in new[]{.8f,2.4f})Cube("Window frame rail",new Vector3(0,y,1),new Vector3(.12f,.1f,2),wood,w.leaf);
   w.glass=Cube("Window glass",new Vector3(0,1.6f,1),new Vector3(.045f,1.5f,1.9f),new Color(.25f,.38f,.39f),w.leaf);houseWindows.Add(w);
  }
  void BuildDoorBoards(BackExit e){e.boardArt=new GameObject[2];for(int i=0;i<2;i++){var board=Cube("Removable back door barricade",V(e.p+Vector2.down*.15f,.8f+i*.7f),new Vector3(2.85f,.22f,.10f),new Color(.35f,.27f,.18f));board.transform.rotation=Quaternion.Euler(0,0,i==0?8:-8);board.SetActive(false);e.boardArt[i]=board;}}
  void RefreshBoards(BackExit e){foreach(var art in e.boardArt)art.SetActive(e.boards>0);if(e.boards>0){e.open=false;e.angle=0;e.gate.active=true;e.leaf.rotation=Quaternion.identity;}}
  int CaptureBoards(){return backExits[0].boards|(backExits[1].boards<<2);}
  int CaptureWindows(){return houseWindows[0].state|(houseWindows[1].state<<2);}
  void RestoreFortifications(int boards,int windows){ClearWindowClimb(false);breakingWindow=null;CancelFort();for(int i=0;i<backExits.Count;i++){backExits[i].boards=(boards>>(i*2))&3;RefreshBoards(backExits[i]);}for(int i=0;i<houseWindows.Count;i++){var w=houseWindows[i];w.state=(windows>>(i*2))&3;w.angle=w.state==1?95:0;RefreshWindow(w);}foreach(var z in infected){z.barricadeTarget=z.barricadeRoute=null;z.barricadeWind=0;}}
  void RefreshWindow(HouseWindow w){w.pane.active=w.state!=2&&w.angle<50;w.leaf.localRotation=Quaternion.Euler(0,w.angle,0);w.leaf.gameObject.SetActive(w.state!=2);}
  HouseWindow NearbyWindow(){foreach(var w in houseWindows)if(Vector2.Distance(hero.p,w.p)<1.8f)return w;return null;}
  void PlayGlass(Vector2 p){
   if(!glassBreak){const int rate=22050;var data=new float[rate/2];var random=new System.Random(422);for(int i=0;i<data.Length;i++){float t=(float)i/rate;data[i]=((float)random.NextDouble()*2-1)*Mathf.Exp(-t*14)*.55f+Mathf.Sin(t*8700)*Mathf.Exp(-t*25)*.12f;}glassBreak=AudioClip.Create("Authored breaking glass",data.Length,1,rate,false);glassBreak.SetData(data,0);}AudioSource.PlayClipAtPoint(glassBreak,V(p,1.6f),.55f);
  }
  bool UseWindow(){var w=NearbyWindow();if(w==null)return false;if(w.state==2){Say("C · перелезть через окно");return true;}bool outside=hero.p.x>w.p.x;
   if(w.state==0&&outside&&homes.IndexOf(w.home)==8){if(weapon==0){Say("Заперто изнутри. Для стекла нужен инструмент.");return true;}if(attack>0||stamina<14)return true;breakingWindow=w;glassTime=0;glassHealth=health;stamina-=14;attack=.85f;heroVelocity=Vector2.zero;hero.root.rotation=Quaternion.LookRotation(V((w.p-hero.p).normalized));hero.visual.Attack(weapon,.85f);return true;}
   w.state=w.state==0?1:0;Alert(w.p,3);foley.Door(V(w.p,1.6f),w.state==1);Save();return true;
  }
  string FortReason(BackExit e,int mode){if(Climbing||attack>0||heroStagger>0||medicalAction!=MedicalAction.None||cacheSearching||searchSlot>=0)return "Сначала закончи действие";if(BackOccupied(e))return "Проём занят";if(mode!=2&&RoomAt(hero.p)!=e.home)return "Крепления находятся внутри";if(mode==0)return inventory.Count(DistrictItemKind.Wood)<2||inventory.Count(DistrictItemKind.Wire)<1?"Нужно 2 доски и проволока":null;if(mode==1)return TryPack(new[]{Ingredient(DistrictItemKind.Wood,1)},false)?null:"Нужно место для снятой доски";return weapon==0?"Нужен инструмент":stamina<14?"Недостаточно сил":null;}
  bool StartFort(BackExit e,int mode){string reason=FortReason(e,mode);if(reason!=null){Say(reason);return false;}fortTarget=e;fortMode=mode;fortTime=fortNoise=0;fortHealth=health;CancelSearch();heroVelocity=Vector2.zero;if(mode==2){stamina-=14;attack=.85f;hero.visual.Attack(weapon,.85f);}else foley.StartTask(true);return true;}
  void CancelFort(){if(fortTarget!=null&&fortMode!=2&&foley)foley.StopTask();fortTarget=null;fortTime=0;}
  void DamageBoards(BackExit e,int damage){if(e.boards<=0)return;e.boards=Mathf.Max(0,e.boards-damage);if(e.boards==0)e.open=true;RefreshBoards(e);sound.Play("hit");Alert(e.p+Vector2.up,12);Save();}
  void TickFortifications(float dt){
   foreach(var w in houseWindows){w.angle=Mathf.MoveTowards(w.angle,w.state==1?95:0,dt*190);RefreshWindow(w);}
   if(breakingWindow!=null){var glass=breakingWindow;if(heroVelocity.sqrMagnitude>.01f||health<glassHealth||heroStagger>0||suppliesOpen||journalOpen||medicalAction!=MedicalAction.None){breakingWindow=null;}else{glassTime+=dt;if(glassTime>=.35f){glass.state=2;RefreshWindow(glass);PlayGlass(glass.p);Alert(glass.p+Vector2.right*1.1f,26);Save();breakingWindow=null;}}}
   if(!FortWorking)return;var e=fortTarget;if(Vector2.Distance(hero.p,e.p)>=1.8f||heroVelocity.sqrMagnitude>.01f||health<fortHealth||heroStagger>0||medicalAction!=MedicalAction.None||suppliesOpen||journalOpen||fortMode!=2&&attack>0){CancelFort();return;}
   fortTime+=dt;fortNoise-=dt;if(fortNoise<=0){fortNoise=1;Alert(e.p+Vector2.up*1.1f,fortMode==2?18:8);}
   if(fortMode==2){if(fortTime>=.35f){DamageBoards(e,1);CancelFort();}return;}
   if(fortTime<(fortMode==0?6:4))return;
   if(BackOccupied(e)){CancelFort();Say("Проём занят");return;}
   if(fortMode==0){if(e.boards==0&&inventory.Count(DistrictItemKind.Wood)>=2&&inventory.Count(DistrictItemKind.Wire)>=1){inventory.Remove(DistrictItemKind.Wood,2);inventory.Remove(DistrictItemKind.Wire,1);e.boards=3;RefreshBoards(e);progression.Gain(DistrictSkill.Craft,5);}}
   else if(e.boards>0&&TryPack(new[]{Ingredient(DistrictItemKind.Wood,1)},true)){e.boards=0;RefreshBoards(e);}else Say("Нет места. Доски остались на двери.");CancelFort();Save();
  }
  bool UseFortification(){if(FortWorking){CancelFort();return true;}if(breakingWindow!=null){breakingWindow=null;return true;}return UseWindow();}
  bool TickBarricadeImpact(Person z,float dt){var e=z.barricadeTarget;if(e==null)return false;z.barricadeWind-=dt;if(e.boards<=0||z.hp<=0||z.stagger>0){z.barricadeTarget=null;return false;}if(z.barricadeWind<=0){if(Vector2.Distance(z.p,e.p)<2.2f)DamageBoards(e,z.id%3==1?2:1);z.barricadeTarget=null;}Animate(z,Vector2.zero,true,.5f);return true;}
  bool TryEnemyBarricade(Person z,float dt){
   if(CanSeeHero(z)&&RoomAt(hero.p)==RoomAt(z.p))return false;
   if(IsCrawler(z)||z.awareness!=Awareness.Chase&&z.awareness!=Awareness.Investigate&&z.awareness!=Awareness.Search)return false;
   foreach(var e in backExits){if(e.boards<=0)continue;bool inside=RoomAt(z.p)==e.home,wantsInside=RoomAt(z.goal)==e.home;if(wantsInside&&inside||!wantsInside&&Vector2.Distance(z.goal,e.p)>2.5f)continue;
    if(wantsInside&&z.barricadeRoute==null){if(z.path.Count>0&&z.repath>0)return false;if(z.repath<=0){var clearPath=FindPath(z.p,z.goal);if(clearPath.Count>0){z.path=clearPath;z.repath=.7f;return false;}}}
    var approach=e.p+Vector2.up*(inside?-1.1f:1.1f);if(Vector2.Distance(z.p,approach)<.85f){if(z.rest<=0){z.barricadeTarget=e;z.barricadeWind=.55f;z.rest=1.7f;z.root.rotation=Quaternion.LookRotation(V((e.p-z.p).normalized));z.visual.Attack(0,1.2f);}else Animate(z,Vector2.zero,true,0);return true;}
    if(z.repath<=0||z.barricadeRoute!=e){z.path=FindPath(z.p,approach);z.repath=.7f;z.barricadeRoute=e;}Vector2 old=z.p;if(z.path.Count>0){if(Vector2.Distance(z.p,z.path[0])<.25f)z.path.RemoveAt(0);else z.p=MoveAmongActors(z,(z.path[0]-z.p).normalized*dt*z.speed,true);}Animate(z,z.p-old,true,0);return true;
   }z.barricadeRoute=null;return false;
  }
  void DrawFortifications(){if(Climbing)return;var w=NearbyWindow();if(w!=null){string hint=w.state==2?"C · перелезть":w.state==1?"C · перелезть    E · закрыть окно":hero.p.x>w.p.x&&homes.IndexOf(w.home)==8?"E · разбить запертое окно · инструмент · громко":"E · открыть окно · тихо";Panel(new Rect(350,495,580,36));GUI.Label(new Rect(362,502,556,26),hint,small);}if(FortWorking){Panel(new Rect(350,545,580,38));GUI.Label(new Rect(362,551,556,28),(fortMode==2?"Взлом":fortMode==0?"Укрепление":"Разбор")+" · "+Mathf.RoundToInt(fortTime/(fortMode==2?.35f:fortMode==0?6:4)*100)+"% · E отмена",small);}}
  void ReleaseFortifications(){if(glassBreak)Destroy(glassBreak);}
 }
}
