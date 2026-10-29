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
    // Real Input System touch events in a Windows player. This is NOT a phone test.
    public sealed class MobilePreview : MonoBehaviour
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        ArenaGame game;Touchscreen screen;string directory;
        readonly List<string> checks=new List<string>(),errors=new List<string>();
        [Serializable] class Report {public bool passed;public string platform="Windows player with virtual Input System touchscreen; no physical Android device";public string[] checks,errors;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Isolate(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-askich-mobile-preview")>=0)AskichProfile.SavePathOverride=Path.Combine(Path.GetTempPath(),"goida-mobile-"+Guid.NewGuid().ToString("N")+".json");}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-askich-mobile-preview")>=0)new GameObject("Mobile input integration").AddComponent<MobilePreview>();}
        T Get<T>(string name)=>(T)typeof(ArenaGame).GetField(name,Flags).GetValue(game);
        string Mode=>Get<object>("mode").ToString();
        void Check(bool ok,string name){checks.Add(name);if(!ok)errors.Add(name);}
        void Touch(int id,Vector2 ui,UnityEngine.InputSystem.TouchPhase phase)
        {
            Vector2 offset=ScreenLayout.Offset;float scale=ScreenLayout.Scale;
            InputSystem.QueueStateEvent(screen,new TouchState{touchId=id,phase=phase,position=new Vector2(offset.x+ui.x*scale,Screen.height-offset.y-ui.y*scale),pressure=phase==UnityEngine.InputSystem.TouchPhase.Ended?0:1});
        }
        IEnumerator Tap(float x,float y)
        {
            var p=new Vector2(x,y);Touch(1,p,UnityEngine.InputSystem.TouchPhase.Began);
            yield return new WaitForSeconds(.08f);Touch(1,p,UnityEngine.InputSystem.TouchPhase.Ended);
            yield return new WaitForSeconds(.1f);
        }
        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(directory,name+".png"),t.EncodeToPNG());Destroy(t);
        }
        IEnumerator Start()
        {
            game=FindFirstObjectByType<ArenaGame>();Application.runInBackground=true;Application.targetFrameRate=60;AudioListener.volume=0;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            var devices=new List<InputDevice>(InputSystem.devices);foreach(var d in devices)if(d is Mouse||d is Keyboard||d is Touchscreen)InputSystem.DisableDevice(d);
            screen=InputSystem.AddDevice<Touchscreen>();TouchControls.ForceEnabled=true;
            string[] args=Environment.GetCommandLineArgs();directory=args[Array.IndexOf(args,"-askich-preview-output")+1];Directory.CreateDirectory(directory);
            Application.logMessageReceived+=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception)errors.Add(message);};
            yield return new WaitForSeconds(.2f);
            yield return Tap(540,539);Check(Mode=="Settings","touch opens settings");
            Touch(1,new Vector2(520,292),UnityEngine.InputSystem.TouchPhase.Began);yield return new WaitForSeconds(.1f);
            Touch(1,new Vector2(820,292),UnityEngine.InputSystem.TouchPhase.Moved);yield return new WaitForSeconds(.1f);
            Touch(1,new Vector2(820,292),UnityEngine.InputSystem.TouchPhase.Ended);yield return new WaitForSeconds(.1f);
            Check(Mathf.Abs(Get<AskichProfile>("profile").effectsVolume-510f/660)<.01f,"touch drags sound slider");yield return Shot("touch-settings");
            yield return Tap(640,574);Check(Mode=="Menu","touch leaves settings");
            yield return Tap(640,388);Check(Mode=="Settlement","touch opens settlement");
            yield return Tap(900,620);Check(Get<bool>("townPaused"),"touch pauses town");
            yield return Tap(90,240);Check(Get<int>("selectedPlot")==0,"touch selects plot");
            var town=Get<AskichProfile>("profile").settlements[0];int wood=town.wood,stone=town.stone;
            yield return Tap(930,288);yield return Tap(1000,555);Check(town.pendingPlot==0,"touch starts construction");yield return Shot("touch-construction");
            yield return Tap(1000,555);Check(town.pendingPlot<0&&town.wood==wood&&town.stone==stone,"touch cancels and refunds once");
            yield return Tap(1195,181);Check(Get<int>("settlementTab")==3,"touch opens economy");yield return Shot("touch-economy");
            yield return Tap(1000,680);Check(Mode=="Menu","touch returns from settlement");
            yield return Tap(640,330);yield return Tap(270,580);Check(Mode=="Story","touch chooses faction");
            yield return Tap(745,568);Check(Mode=="Playing","touch starts battle");
            Vector2 before=Get<Vector2>("playerPos");
            Touch(1,TouchControls.Stick+Vector2.right*55,UnityEngine.InputSystem.TouchPhase.Began);
            Touch(2,TouchControls.Attack,UnityEngine.InputSystem.TouchPhase.Began);yield return new WaitForSeconds(.5f);
            Check(Get<Vector2>("playerPos").x>before.x+.4f&&Get<float>("stamina")<105,"two fingers move and attack together");yield return Shot("touch-battle");
            Touch(1,TouchControls.Stick+Vector2.right*55,UnityEngine.InputSystem.TouchPhase.Ended);
            Touch(2,TouchControls.Attack,UnityEngine.InputSystem.TouchPhase.Ended);yield return new WaitForSeconds(.1f);
            Check(!Get<TouchControls>("touchControls").attack&&Get<TouchControls>("touchControls").move==Vector2.zero,"released fingers stop controls");
            yield return Tap(TouchControls.Pause.x,TouchControls.Pause.y);Check(Mode=="Paused","touch pauses combat");yield return Shot("touch-pause");
            yield return Tap(640,345);Check(Mode=="Playing","touch resumes combat");
            File.WriteAllText(Path.Combine(directory,"touch-playtest.json"),JsonUtility.ToJson(new Report{passed=errors.Count==0,checks=checks.ToArray(),errors=errors.ToArray()},true));
            Debug.Log("GOIDA_TOUCH_"+(errors.Count==0?"PASS":"FAIL"));Application.Quit(errors.Count==0?0:1);
        }
    }
}
#endif
