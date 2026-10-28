#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace Askich
{
    // Executes public economy commands; no free resources, levels, armies, or equipment.
    // UI navigation uses reflection. This is a rendered integration demo, not a mouse-input test.
    public sealed class SettlementPreview : MonoBehaviour
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        ArenaGame game;string directory;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Isolate(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-askich-town-preview")>=0)AskichProfile.SavePathOverride=Path.Combine(Path.GetTempPath(),"askich-town-"+Guid.NewGuid().ToString("N")+".json");}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-askich-town-preview")>=0)new GameObject("Settlement preview").AddComponent<SettlementPreview>();}
        void Set(string n,object value)=>typeof(ArenaGame).GetField(n,Flags).SetValue(game,value);
        T Get<T>(string n)=>(T)typeof(ArenaGame).GetField(n,Flags).GetValue(game);
        void Mode(string name){var f=typeof(ArenaGame).GetField("mode",Flags);f.SetValue(game,Enum.Parse(f.FieldType,name));}
        void Build(Settlement t,int plot,int type)
        {
            int guard=0;while(t.BuildReason(plot,type)!=""&&guard++<100)t.AdvanceDay();
            if(!t.Build(plot,type))throw new Exception("Unreachable building: "+type);
            while(t.pendingPlot>=0)t.AdvanceDay();
        }
        IEnumerator Shot(string name)
        {
            yield return null;yield return new WaitForEndOfFrame();
            var image=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(directory,name+".png"),image.EncodeToPNG());Destroy(image);
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;AudioListener.volume=0;
            game=FindFirstObjectByType<ArenaGame>();Set("persistenceEnabled",false);
            string[] args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-askich-preview-output");directory=args[i+1];Directory.CreateDirectory(directory);
            yield return Shot("menu");
            Mode("Settings");yield return Shot("settings");
            var p=Get<AskichProfile>("profile");p.Validate();
            Mode("Armory");yield return Shot("armory-rus");
            Set("faction",1);yield return Shot("armory-france");
            Set("faction",2);yield return Shot("armory-norway");
            Set("faction",0);Mode("Settlement");yield return Shot("settlement-new");
            var t=p.settlements[0];
            if(!t.Build(0,3))throw new Exception("First farm rejected");
            Set("selectedPlot",0);yield return Shot("construction");t.AdvanceClock(30);Set("selectedPlot",7);
            while(t.day<7)t.AdvanceClock(30);
            yield return Shot("village-event");if(!t.ChooseEvent(0))t.ChooseEvent(1);
            Build(t,1,5);Build(t,2,2);Build(t,3,6);Build(t,4,7);Build(t,5,8);Build(t,7,1);
            yield return Shot("settlement-tier2");
            Build(t,11,3);Build(t,0,3);Build(t,3,6);Build(t,8,9);Build(t,8,9);Build(t,9,10);Build(t,10,2);Build(t,7,1);
            Build(t,6,2);Build(t,6,2);
            while(t.legitimacy<50||t.food<40)t.AdvanceDay();
            if(!t.Reform(true,1))throw new Exception("Confederation unreachable");
            while(t.legitimacy<50||t.food<40)t.AdvanceDay();
            if(!t.Reform(false,2))throw new Exception("Veche unreachable");
            Set("selectedPlot",6);yield return Shot("settlement-tier3");
            Set("settlementTab",3);yield return Shot("economy");
            while(t.wood<150)t.AdvanceDay();if(!t.TradeBatch(3,5))throw new Exception("Trade rejected");
            yield return Shot("trade-result");
            Set("settlementTab",1);yield return Shot("constitution");
            Set("settlementTab",2);yield return Shot("raids");
            while(t.soldiers<6){if(!t.Recruit())t.AdvanceDay();}
            Set("raidTarget",0);Set("raidTactic",0);Set("raidElapsed",0f);Set("raidFinished",false);Mode("Raid");
            yield return new WaitForSeconds(3.5f);yield return Shot("raid-battle");
            yield return new WaitForSeconds(3);yield return Shot("raid-result");
            File.WriteAllText(Path.Combine(directory,"settlement-playtest.json"),"{\"passed\":true,\"commands\":\"normal economy commands; reflective UI navigation\",\"day\":"+t.day+",\"hall\":"+t.Hall+",\"constitution\":"+t.constitution+",\"government\":"+t.government+",\"raidWins\":"+t.raidWins+"}");
            TouchControls.ForceEnabled=true;game.StartRun();typeof(ArenaGame).GetMethod("ContinueStory",Flags).Invoke(game,new object[]{false});
            yield return new WaitForSeconds(2);yield return Shot("touch-controls");
            Application.Quit(0);
        }
    }
}
#endif
