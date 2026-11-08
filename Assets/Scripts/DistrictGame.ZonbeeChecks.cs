#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Askich {
 public sealed partial class DistrictGame {
  IEnumerator CheckZonbeePolish(){
   string folder=Environment.GetEnvironmentVariable("ZONBEE_POLISH_CHECK");Directory.CreateDirectory(folder);testSave=Path.Combine(Application.temporaryCachePath,"zonbee-check-"+Guid.NewGuid()+".json");playing=true;paused=false;Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
   foreach(var d in InputSystem.devices.ToArray())if(d is Keyboard||d is Mouse)InputSystem.DisableDevice(d);
   var keys=InputSystem.AddDevice<Keyboard>();keys.MakeCurrent();var results=new System.Collections.Generic.List<string>();
   void Check(bool ok,string detail){results.Add((ok?"PASS ":"FAIL ")+detail);File.WriteAllLines(Path.Combine(folder,"zonbee-checks.txt"),results);if(!ok){Application.Quit(1);throw new Exception(detail);}}
   yield return null;foreach(var enemy in infected){enemy.hp=0;enemy.root.gameObject.SetActive(false);}heroVelocity=Vector2.zero;
   Check(Application.productName=="Zonbee","Windows product and title are Zonbee");
   Check(solidProps.Count>100,"furniture and street objects have registered movement obstacles");
   File.WriteAllLines(Path.Combine(folder,"solid-props.txt"),solidProps.Select(p=>p.name+" "+p.rect));
   var table=solidProps.First(p=>p.name=="Table");var point=new Vector2(table.rect.xMin-.16f,table.rect.center.y);var stopped=Move(point,Vector2.right*4);
   Check(stopped.x<=table.rect.xMin+.01f&&stopped.x>point.x,"large movement step cannot tunnel through table");
   Check(SolidBlocked(table.rect.center),"table footprint blocks the survivor body");
   var fixedPosition=ResolveSolidPosition(table.rect.center);Check(!Blocked(fixedPosition)&&RoomAt(fixedPosition)==RoomAt(table.rect.center),"legacy position inside furniture is repaired within its original room");
   Check(Sight(table.rect.center+Vector2.down,table.rect.center+Vector2.up),"low furniture does not falsely block head-height sight");
   foreach(var home in homes){home.open=true;SnapDoor(home);for(int slot=0;slot<3;slot++){
    Check(ContainerReachable(home,slot),"reachable container after solid furniture: "+homes.IndexOf(home)+" / "+slot);
   }}
   hero.p=new Vector2(-24,-24);hero.root.position=V(hero.p);progression.Data.minute=990;noticeTime=0;showMap=showHelp=false;
   foreach(var size in new[]{new Vector2Int(1600,1000),new Vector2Int(1920,800)}){
    Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);yield return new WaitForSeconds(.6f);ScreenLayout.Fit(cam);Check(cam.rect==new Rect(0,0,1,1),"full camera viewport at "+size);
   }
   var background=cam.backgroundColor;var mask=cam.cullingMask;cam.cullingMask=0;cam.backgroundColor=new Color(.14f,.35f,.68f);yield return null;yield return new WaitForEndOfFrame();var proof=ScreenCapture.CaptureScreenshotAsTexture();
   var left=proof.GetPixel(3,3);var right=proof.GetPixel(proof.width-4,3);Check(left.b>.2f&&right.b>.2f&&Mathf.Abs(left.b-right.b)<.06f,"both screen edges receive a fresh frame instead of stale world strips");File.WriteAllBytes(Path.Combine(folder,"viewport-solid-proof.png"),proof.EncodeToPNG());Destroy(proof);cam.cullingMask=mask;cam.backgroundColor=background;
   Check(!showMap&&!showHelp&&HudCaption.y>600&&HudVitals.width<400,"default HUD hides help and map and keeps radio out of the centre");
   Check(CompactHudHit(HudInventory.center)&&!PointerOnHud(new Vector2(640,360)),"compact HUD blocks its controls while centre stays interactive");
   InputSystem.QueueStateEvent(keys,new KeyboardState(Key.M));yield return null;yield return null;Check(showMap,"M opens optional minimap");InputSystem.QueueStateEvent(keys,new KeyboardState());yield return null;InputSystem.QueueStateEvent(keys,new KeyboardState(Key.M));yield return null;yield return null;Check(!showMap,"M hides optional minimap");InputSystem.QueueStateEvent(keys,new KeyboardState());yield return null;
   InputSystem.QueueStateEvent(keys,new KeyboardState(Key.F1));yield return null;yield return null;Check(showHelp,"F1 opens control help on demand");InputSystem.QueueStateEvent(keys,new KeyboardState());showHelp=false;
   var clip=hero.visual.ArtClip("Walk");hero.visual.SetPaused(true);hero.visual.ArtSample("Walk",.08f);var foot=hero.visual.ArtBone("Foot.L");Vector3 a=hero.root.InverseTransformPoint(foot.position);hero.visual.ArtSample("Walk",.18f);var b=hero.root.InverseTransformPoint(foot.position);float speed=(a.z-b.z)/(clip.length*.1f);Check(Mathf.Abs(speed-1.8f)<.30f,"revised stance follows nominal walking speed without overreaching");
   hero.visual.ArtSample("Walk",0);float low=hero.visual.ArtBone("Hips").position.y;hero.visual.ArtSample("Walk",.25f);float high=hero.visual.ArtBone("Hips").position.y;Check(high>low+.04f,"pelvis rises over supporting foot instead of rising at double contact");
   hero.visual.SetPaused(false);hero.p=new Vector2(-24,-24);heroVelocity=Vector2.zero;InputSystem.QueueStateEvent(keys,new KeyboardState(Key.W));var start=hero.p;yield return new WaitForSeconds(.65f);InputSystem.QueueStateEvent(keys,new KeyboardState());Check(Vector2.Distance(hero.p,start)>.8f&&Vector2.Distance(hero.p,start)<1.35f&&hero.visual.State=="Walk","ordinary keyboard walk uses revised speed and active skeletal animation");yield return new WaitForSeconds(.15f);
   hero.p=homes[5].p+new Vector2(.1f,-1.4f);hero.root.position=V(hero.p);heroVelocity=Vector2.zero;zoom=6;radioOn=trainingRadio=true;radioSpeaking=false;staticSound.Play();noticeTime=0;yield return new WaitForSeconds(.5f);hudRoom=current;roomTitleUntil=0;vitalTipTime=0;yield return CaptureZonbeeUI(Path.Combine(folder,"zonbee-wide-hud.png"));radioOn=trainingRadio=false;staticSound.Stop();
   // Real legacy import, with both source and destination confined to test paths.
   hero.p=ResolveSolidPosition(hero.p);Save();string snapshot=File.ReadAllText(SavePath);legacySaveOverride=Path.Combine(Application.temporaryCachePath,"zonbee-legacy-"+Guid.NewGuid()+".json");string legacyFixture=Environment.GetEnvironmentVariable("ZONBEE_LEGACY_FIXTURE");string originalLegacy=string.IsNullOrEmpty(legacyFixture)?snapshot:File.ReadAllText(legacyFixture);File.WriteAllText(legacySaveOverride,originalLegacy);if(File.Exists(SavePath))File.Delete(SavePath);if(File.Exists(SavePath+".bak"))File.Delete(SavePath+".bak");
   Check(Load()&&File.Exists(SavePath)&&File.ReadAllText(legacySaveOverride)==originalLegacy,"renamed game imports old profile once and leaves original bytes intact");
   File.WriteAllText(SavePath,"broken");File.WriteAllText(SavePath+".bak","broken");float healthBefore=health;Check(!Load()&&health==healthBefore,"existing corrupted Zonbee world is not replaced with stale legacy data");File.WriteAllText(SavePath,snapshot);legacySaveOverride=null;Load();
   foreach(var enemy in infected){enemy.hp=0;enemy.root.gameObject.SetActive(false);}noticeTime=0;
   hero.p=ResolveSolidPosition(new Vector2(-24,-24));heroVelocity=Vector2.zero;zoom=5;yield return new WaitForSeconds(.3f);string film=Path.Combine(folder,"motion");Directory.CreateDirectory(film);
   InputSystem.QueueStateEvent(keys,new KeyboardState(Key.D));for(int i=0;i<40;i++){yield return new WaitForSeconds(.05f);yield return CaptureZonbeeUI(Path.Combine(film,"walk-"+i.ToString("D3")+".png"));}
   InputSystem.QueueStateEvent(keys,new KeyboardState(Key.D,Key.LeftShift));for(int i=0;i<40;i++){yield return new WaitForSeconds(.05f);yield return CaptureZonbeeUI(Path.Combine(film,"run-"+i.ToString("D3")+".png"));}
   InputSystem.QueueStateEvent(keys,new KeyboardState());InputSystem.RemoveDevice(keys);Application.Quit();
  }
  IEnumerator CaptureZonbeeUI(string file){yield return new WaitForEndOfFrame();var picture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(file,picture.EncodeToPNG());Destroy(picture);}
 }
}
#endif
