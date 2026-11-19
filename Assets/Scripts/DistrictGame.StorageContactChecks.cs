#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Askich {
 public sealed partial class DistrictGame {
  IEnumerator CheckStorageContact(){
   string folder=Environment.GetEnvironmentVariable("ZONBEE_STORAGE_CONTACT_CHECK");Directory.CreateDirectory(folder);testSave=Path.Combine(Application.temporaryCachePath,"contact-"+Guid.NewGuid()+".json");
   Application.runInBackground=true;playing=true;paused=false;foreach(var device in InputSystem.devices.ToArray())if(device is Keyboard||device is Mouse)InputSystem.DisableDevice(device);
   foreach(var z in infected){z.hp=0;z.root.gameObject.SetActive(false);}health=stamina=100;attack=heroStagger=0;heroVelocity=Vector2.zero;bleeding=false;infection=0;zoom=4;
   var results=new List<string>();void Check(bool ok,string message){results.Add((ok?"PASS ":"FAIL ")+message);File.WriteAllLines(Path.Combine(folder,"storage-contact-checks.txt"),results);if(!ok){Application.Quit(1);throw new Exception(message);}}
   var h=homes[5];h.open=true;SnapDoor(h);
   for(int slot=1;slot<=2;slot++){
    CancelSearch();h.taken[slot]=false;RefreshContainers(h);hero.p=StorageStand(h,slot)+Vector2.right*.4f;heroVelocity=Vector2.zero;yield return new WaitForSeconds(.3f);current=h;
    var start=hero.p;ApproachStorage(h,slot);Check(approachHome==h&&searchSlot<0&&!containerAudio.Searching,"approach precedes rummaging and sound: "+slot);
    yield return new WaitForSeconds(.12f);Check(Vector2.Distance(hero.p,start)>.04f&&Vector2.Distance(hero.p,start)<.24f,"approach walks without teleporting: "+slot);
    float deadline=Time.time+3;while(approachHome!=null&&Time.time<deadline)yield return null;yield return new WaitForSeconds(.2f);yield return new WaitForEndOfFrame();Check(searchSlot==slot&&Vector2.Distance(hero.p,StorageStand(h,slot))<.03f,"search starts at the accessible front edge: "+slot);
    Check(hero.visual.ArtContactWeight>.95f,"contact pose blends in: "+slot);
    float handY=(hero.visual.ArtBone("Hand.L").position.y+hero.visual.ArtBone("Hand.R").position.y)/2;
    Check(handY>.55f&&handY<(slot==2?1.05f:1.15f),"hands reach the actual container height: "+slot);
    Check(hero.visual.ArtBone("Foot.L").position.y>.15f&&hero.visual.ArtBone("Foot.L").position.y<.4f&&hero.visual.ArtBone("Foot.R").position.y>.15f&&hero.visual.ArtBone("Foot.R").position.y<.4f,"feet remain at ground level: "+slot);
    yield return CaptureGameCamera(Path.Combine(folder,slot==1?"drawer-contact.png":"medical-contact.png"),2.5f);
    CancelSearch();yield return new WaitForSeconds(.3f);Check(hero.visual.ArtContactWeight==0,"cancellation releases contact pose: "+slot);
   }
   hero.p=StorageStand(h,1)+Vector2.right*.4f;heroVelocity=Vector2.zero;current=h;h.taken[1]=false;ApproachStorage(h,1);Use();Check(approachHome==null&&searchSlot<0,"E cancels automatic approach before looting");
   var goal=StorageStand(h,1);var obstacle=new Barrier{rect=new Rect(goal-new Vector2(.1f,.1f),new Vector2(.2f,.2f))};barriers.Add(obstacle);ApproachStorage(h,1);Check(approachHome==null&&searchSlot<0,"blocked standing point rejects approach without crossing furniture");barriers.Remove(obstacle);
   Save();Check(Load(),"existing save loads after transient contact and approach state");Application.Quit();
  }
 }
}
#endif
