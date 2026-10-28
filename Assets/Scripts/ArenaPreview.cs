#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Askich
{
    [DefaultExecutionOrder(-1000)]
    public sealed class ArenaPreview : MonoBehaviour
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        ArenaGame game; Keyboard keyboard; Mouse mouse; Camera cameraView;
        string directory; float started,modeSince; string lastMode=""; int lastWave; bool ready,done;
        readonly HashSet<string> captures=new HashSet<string>();
        readonly List<string> errors=new List<string>();
        [Serializable] class Report {public bool passed;public int wave,kills;public float health,seconds;public string mode;public string[] screenshots,errors;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Isolate() {if(Array.IndexOf(Environment.GetCommandLineArgs(),"-askich-preview")>=0) AskichProfile.SavePathOverride=Path.Combine(Path.GetTempPath(),"askich-preview-"+Guid.NewGuid().ToString("N")+".json");}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap() {if(Array.IndexOf(Environment.GetCommandLineArgs(),"-askich-preview")>=0)new GameObject("Preview").AddComponent<ArenaPreview>();}
        T Get<T>(string name)=>(T)typeof(ArenaGame).GetField(name,Flags).GetValue(game);
        T Field<T>(object o,string name)=>(T)o.GetType().GetField(name).GetValue(o);
        IEnumerator Start()
        {
            game=FindFirstObjectByType<ArenaGame>();typeof(ArenaGame).GetField("persistenceEnabled",Flags).SetValue(game,false);
            Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;AudioListener.volume=0;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            var devices=new List<InputDevice>(InputSystem.devices);foreach(var d in devices)if(d is Keyboard||d is Mouse)InputSystem.DisableDevice(d);
            keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();cameraView=Get<Camera>("gameCamera");
            string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-askich-preview-output");directory=at>=0?args[at+1]:Application.persistentDataPath;Directory.CreateDirectory(directory);
            Application.logMessageReceived+=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception)errors.Add(message);};
            started=Time.realtimeSinceStartup; yield return null;ready=true;
        }
        void Update()
        {
            if(!ready||done)return;
            string mode=Get<object>("mode").ToString();int wave=Get<int>("wave");
            if(mode!=lastMode||wave!=lastWave){lastMode=mode;lastWave=wave;modeSince=Time.realtimeSinceStartup;}
            float age=Time.realtimeSinceStartup-modeSince;
            var keys=new List<Key>();Vector2 pos=Get<Vector2>("playerPos"),aim=pos+Vector2.right; bool attack=false,block=false;
            if(mode!="Playing")
            {
                string shot=mode=="Menu"?"01-menu":mode=="Faction"?"02-factions":mode=="Story"?"story-"+wave:mode=="Upgrade"?"camp-"+wave:mode;
                if(age>.3f&&!captures.Contains(shot)) {captures.Add(shot);StartCoroutine(Capture(shot));}
                if(age>1.1f)
                {
                    if(mode=="Menu")keys.Add(Key.Enter);
                    if(mode=="Faction")keys.Add(Key.Digit1);
                    if(mode=="Story")keys.Add(wave==3?Key.Digit1:Key.Enter);
                    if(mode=="Upgrade")keys.Add(Key.Digit2);
                }
            }
            else
            {
                var enemies=Get<IList>("enemies");object nearest=null;float dist=100;
                float priority=1000;
                foreach(var e in enemies){Vector2 enemyPos=Field<Vector2>(e,"pos");float d=Vector2.Distance(pos,enemyPos),rating=d;
                    if(wave==2||wave==4){float danger=Vector2.Distance(enemyPos,Get<Vector2>("objectivePos"));if(danger<4)rating=d*.1f+danger*.5f-10;}
                    if(rating<priority){priority=rating;dist=d;nearest=e;}}
                Vector2 target=pos;
                if(nearest!=null)
                {
                    aim=Field<Vector2>(nearest,"pos");Vector2 dir=(aim-pos).normalized;
                    target=dist>1.7f?aim:pos-dir*.7f;
                    attack=dist<2.2f;block=Field<bool>(nearest,"winding")&&dist<2.2f;
                    if(dist<2.3f&&Get<float>("stamina")>65&&!block)keys.Add(Key.Q);
                    if(block&&Get<float>("stamina")>40&&Field<int>(nearest,"kind")>=2)keys.Add(Key.Space);
                }
                else if(wave==2||wave==4)target=Get<Vector2>("objectivePos");
                else if(wave==3)
                {
                    var prisoners=Get<List<SpriteRenderer>>("captives");if(prisoners.Count>0){target=prisoners[0].transform.position;if(Vector2.Distance(pos,target)<1.4f&&Time.frameCount%10==0)keys.Add(Key.E);}
                }
                var drops=Get<IList>("gearDrops");
                if(drops.Count>0)
                {
                    Vector2 loot=Field<Vector2>(drops[0],"position");
                    if(wave!=2&&wave!=4&&(nearest==null||dist>4))target=loot;
                    if(Vector2.Distance(pos,loot)<1.5f&&Time.frameCount%10==0)keys.Add(Key.E);
                }
                Vector2 move=target-pos;
                if(move.x>.15f)keys.Add(Key.D);if(move.x<-.15f)keys.Add(Key.A);if(move.y>.15f)keys.Add(Key.W);if(move.y<-.15f)keys.Add(Key.S);
                string name=wave<3?"03-village":"04-fortress";
                if(age>8&&!captures.Contains(name)){captures.Add(name);StartCoroutine(Capture(name));}

            }
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys.ToArray()));
            var state=new MouseState{position=cameraView.WorldToScreenPoint(aim)};if(attack)state=state.WithButton(MouseButton.Left);if(block)state=state.WithButton(MouseButton.Right);InputSystem.QueueStateEvent(mouse,state);
            if(!done&&mode=="Victory"){done=true;StartCoroutine(Finish(true));} if(!done&&(mode=="Defeat"||Time.realtimeSinceStartup-started>360)){done=true;StartCoroutine(Finish(false));}
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();
            if(texture==null){errors.Add("Empty screenshot "+name);yield break;}
            int bright=0;for(int y=0;y<texture.height;y+=24)for(int x=0;x<texture.width;x+=24)if(texture.GetPixel(x,y).grayscale>.05f)bright++;
            if(bright<50)errors.Add("Black screenshot "+name);
            File.WriteAllBytes(Path.Combine(directory,name+".png"),texture.EncodeToPNG());Destroy(texture);
        }
        IEnumerator Finish(bool success)
        {
            yield return Capture("05-playtest-end");
            var report=new Report{passed=success&&errors.Count==0,wave=Get<int>("wave"),kills=Get<int>("kills"),health=Get<float>("health"),seconds=Time.realtimeSinceStartup-started,mode=Get<object>("mode").ToString(),screenshots=new List<string>(captures).ToArray(),errors=errors.ToArray()};
            File.WriteAllText(Path.Combine(directory,"playtest.json"),JsonUtility.ToJson(report,true));Application.Quit(report.passed?0:1);
        }
    }
}
#endif
