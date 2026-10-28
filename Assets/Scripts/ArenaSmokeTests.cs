#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace Askich
{
    public sealed class ArenaSmokeTests : MonoBehaviour
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        ArenaGame game;
        [Serializable] public class Result { public string name; public bool passed; public string detail; }
        [Serializable] public class Report { public string suite="Askich medieval runtime integration"; public bool passed; public Result[] tests; }
        readonly List<Result> results=new List<Result>();
        static string isolated;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Isolate()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-askich-smoke")<0) return;
            isolated=Path.Combine(Path.GetTempPath(),"askich-medieval-"+Guid.NewGuid().ToString("N")+".json"); AskichProfile.SavePathOverride=isolated;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap() { if(isolated!=null) new GameObject("Smoke tests").AddComponent<ArenaSmokeTests>(); }
        T Get<T>(string name) => (T)typeof(ArenaGame).GetField(name,Flags).GetValue(game);
        void Set(string name,object value) => typeof(ArenaGame).GetField(name,Flags).SetValue(game,value);
        object Call(string name,params object[] args) => typeof(ArenaGame).GetMethod(name,Flags).Invoke(game,args);
        string Mode => typeof(ArenaGame).GetField("mode",Flags).GetValue(game).ToString();
        void Check(bool value,string detail) { if(!value) throw new Exception(detail); }
        void Test(string name,Action action)
        {
            try { game.StartRun(); action(); results.Add(new Result{name=name,passed=true,detail="OK"}); }
            catch(Exception e) { results.Add(new Result{name=name,passed=false,detail=(e.InnerException??e).Message}); }
        }
        void Begin(int faction=0) { Set("faction",faction); game.StartRun(); Call("ContinueStory",false); }
        IList Enemies => Get<IList>("enemies");
        object Enemy(Vector2 position)
        {
            Set("remaining",3); Call("SpawnEnemy"); object e=Enemies[Enemies.Count-1]; Field(e,"pos",position); return e;
        }
        void Field(object obj,string name,object value) => obj.GetType().GetField(name).SetValue(obj,value);
        T Field<T>(object obj,string name) => (T)obj.GetType().GetField(name).GetValue(obj);
        IEnumerator Start()
        {
            yield return null;
            game=FindFirstObjectByType<ArenaGame>(); game.enabled=false;
            Test("all_atlas_frames_are_available",()=> {for(int r=0;r<7;r++) for(int c=0;c<4;c++) Check(MedievalArt.Warrior(r,c)!=null,"warrior atlas"); for(int i=0;i<16;i++) Check(MedievalArt.Prop(i)!=null,"prop atlas");});
            Test("prologue_gates_campaign",()=>Check(Mode=="Story"&&Get<int>("wave")==0,"prologue missing"));
            Test("three_factions_have_distinct_stats",()=> {Begin(0);Check(Get<float>("reach")==2.2f,"spear reach");Begin(1);Check(Get<float>("maxHealth")==135,"knight health");Begin(2);Check(Get<float>("damage")==34,"axe damage");});
            Test("light_attack_cone_and_distance",()=> {Begin();Set("playerPos",Vector2.zero);Set("facing",Vector2.right);var front=Enemy(Vector2.right);var rear=Enemy(Vector2.left);var far=Enemy(Vector2.right*4);Call("Attack",false);Check(Field<float>(front,"hp")==52,"damage before windup");Call("TickStrike",.11f);Check(Field<float>(front,"hp")==24,"front damage");Check(Field<float>(rear,"hp")==52&&Field<float>(far,"hp")==52,"cone or range");});
            Test("heavy_attack_spends_stamina_and_kills_once",()=> {Begin(2);Set("playerPos",Vector2.zero);Set("facing",Vector2.right);Enemy(Vector2.right);Call("Attack",true);Call("TickStrike",.24f);Check(Get<int>("kills")==1&&Get<float>("stamina")==92,"heavy attack");Call("Attack",true);Check(Get<int>("kills")==1&&Get<float>("stamina")==92,"cooldown");});
            Test("exhaustion_prevents_attack",()=> {Begin();Set("stamina",0f);Set("attackTimer",0f);Call("Attack",true);Check(Get<float>("attackTimer")==0,"free heavy attack");});
            Test("shield_is_directional",()=> {Begin(1);Set("playerPos",Vector2.zero);Set("facing",Vector2.right);Set("blocking",true);Call("Hurt",20f,Vector2.right);Check(Get<float>("health")>130,"front block");Set("hurtTimer",0f);float before=Get<float>("health");Call("Hurt",20f,Vector2.left);Check(Mathf.Approximately(before-Get<float>("health"),20),"back was blocked");});
            Test("dodge_cost_and_invulnerability",()=> {Begin();Call("Dodge",Vector2.left);float hp=Get<float>("health");Call("Hurt",90f,Vector2.left);Check(Get<float>("health")==hp&&Get<float>("stamina")==85&&Get<float>("dodgeTimer")>0,"dodge");});
            Test("defeat_stops_damage_and_result_rewrites",()=> {Begin();Call("Hurt",500f,Vector2.left);Check(Mode=="Dying","death animation");Call("TickDeath",1.1f);Check(Mode=="Defeat","death");Call("Finish",true);Check(Mode=="Defeat","result rewritten");});
            Test("wagon_requires_proximity_and_arrival",()=> {Begin();Call("NextWave");Set("remaining",0);Set("playerPos",new Vector2(8,0));float x=Get<Vector2>("objectivePos").x;Call("Tick",.05f);Check(Get<Vector2>("objectivePos").x==x,"wagon moved alone");Set("playerPos",Get<Vector2>("objectivePos"));Call("Tick",.05f);Check(Get<Vector2>("objectivePos").x>x,"escort stalled");Check(!(bool)Call("ObjectiveComplete"),"premature objective");});
            Test("rescue_requires_nearby_interaction",()=> {Begin();Call("NextWave");Call("NextWave");Call("Rescue");Check(Get<int>("rescued")==0,"remote rescue");Set("playerPos",new Vector2(-6,0));Call("Rescue");Call("Rescue");Check(Get<int>("rescued")==1,"duplicate rescue");Check(game.LocationIndex==1,"fortress transition");});
            Test("siege_failure_ends_campaign",()=> {Begin();Call("NextWave");Call("DamageObjective",1000f);Check(Mode=="Defeat","wagon destruction");});
            Test("temple_choice_is_single_use",()=> {Begin();Set("wave",3);Call("CompleteMission");float dmg=Get<float>("damage");Call("ContinueStory",false);float after=Get<float>("damage");Call("ContinueStory",false);Check(Mathf.Approximately(after,dmg*1.15f)&&Get<float>("damage")==after,"choice repeated");});
            Test("ally_choice_spawns_companion",()=> {Begin();Set("wave",3);Call("CompleteMission");Call("ContinueStory",true);Call("Upgrade",0);Check(Get<SpriteRenderer>("ally")!=null&&Get<int>("wave")==4,"ally missing");});
            Test("upgrades_apply_once_and_advance",()=> {Begin();Call("CompleteMission");Call("ContinueStory",false);float hp=Get<float>("maxHealth");Call("Upgrade",1);Call("Upgrade",1);Check(Get<float>("maxHealth")==hp+25&&Get<int>("wave")==2,"upgrade repeated");});
            Test("five_missions_reach_victory_and_save_faction",()=> {Begin(2);for(int i=1;i<=5;i++) {Set("remaining",0);Call("CompleteMission");if(i<5){Call("ContinueStory",true);Call("Upgrade",1);}}Check(Mode=="Victory"&&(Get<AskichProfile>("profile").completedFactions&4)!=0,"victory");});
            Test("wardrobe_wallet_and_no_double_charge",()=> {var p=Get<AskichProfile>("profile");p.credits=179;p.unlockedSkins=1;Check(!(bool)Call("BuySkin",1),"free unlock");p.credits=700;Check((bool)Call("BuySkin",2)&&p.credits==180,"price");Call("BuySkin",2);Check(p.credits==180,"double charge");});
            Test("v1_profile_migrates_without_losing_currency",()=> {File.WriteAllText(isolated,"{\"version\":1,\"credits\":345,\"unlockedSkins\":3,\"selectedSkin\":1,\"bestScore\":900}");var p=AskichProfile.Load();Check(p.version==4&&p.credits==345&&p.selectedSkin==1&&p.selectedFaction==0,"migration");p.selectedFaction=2;p.completedFactions=5;Check(p.Save(),"save");var restored=AskichProfile.Load();Check(restored.selectedFaction==2&&restored.completedFactions==5,"round trip");});
            Test("corrupt_profile_falls_back",()=> {File.WriteAllText(isolated,"corrupt");Check(AskichProfile.Load().credits==0,"fallback");});
            Test("restart_resets_campaign_not_wallet",()=> {Begin();Call("AddSilver",42);int credits=Get<AskichProfile>("profile").credits;Enemy(Vector2.zero);game.StartRun();Check(Enemies.Count==0&&Get<int>("wave")==0&&!Get<bool>("orderAlly")&&Get<AskichProfile>("profile").credits==credits,"restart");});
            Test("equipment_pickup_is_local_and_persistent",()=> {Begin(0);Set("playerPos",Vector2.zero);Set("kills",3);Call("DropGear",Vector2.right*4);Call("PickGear");Check((Get<AskichProfile>("profile").ownedItems&1)==0,"remote pickup");Set("playerPos",Vector2.right*4);Call("PickGear");Check((Get<AskichProfile>("profile").ownedItems&1)!=0,"pickup missing");Check((bool)Call("EquipItem",0),"equip");Begin(0);Check(Get<float>("damage")==34,"bonus missing");Begin(1);Check(Get<float>("damage")==26,"foreign item bonus");});
            Test("unowned_equipment_cannot_be_equipped",()=>Check(!(bool)Call("EquipItem",8),"unowned item"));
            Test("nine_house_sprites_available",()=> {for(int f=0;f<3;f++)for(int t=0;t<3;t++)Check(MedievalArt.House(f,t)!=null,"house atlas");});
            Test("settlement_queue_debits_once_and_completes",()=> {var t=new Settlement();t.Validate();int wood=t.wood;Check(t.Build(0,2),"build rejected");Check(!t.Build(1,2)&&t.wood==wood-45,"double build");t.AdvanceDay();Check(t.buildings[0]==2&&t.levels[0]==1&&t.pendingPlot==-1,"construction");Check(!t.Build(0,3),"overlap");});
            Test("settlement_prerequisites_and_insufficient_resources",()=> {var t=new Settlement();t.Validate();Check(!t.Build(7,1),"early hall");t.wood=0;Check(!t.Build(0,2),"free building");Check(!t.Reform(false,2),"early reform");});
            Test("settlement_production_hunger_and_defence",()=> {var t=new Settlement();t.Validate();int wood=t.wood;t.AdvanceDay();Check(t.wood==wood+22&&t.population==13,"production");t.buildings[11]=0;t.levels[11]=0;t.food=0;t.AdvanceDay();Check(t.morale<70&&t.population==12,"hunger");});
            Test("settlement_raid_is_single_reward_per_cooldown",()=> {var t=new Settlement();t.Validate();Check(!t.ResolveRaid(0,0),"raid without barracks");t.buildings[0]=7;t.levels[0]=1;Check(t.ResolveRaid(0,0),"prepared raid");int wood=t.wood;Check(!t.ResolveRaid(0,0)&&t.wood==wood,"duplicate loot");Check(t.soldiers==3,"casualties");});
            Test("settlement_reform_constraints_and_cost",()=> {var t=new Settlement();t.Validate();t.levels[7]=2;t.buildings[0]=10;t.levels[0]=1;Check(t.Reform(false,2)&&t.government==2&&t.food==60&&t.legitimacy==35,"veche reform");Check(!t.Reform(false,1),"reform bypass");});
            Test("settlement_profile_roundtrip_isolated_factions",()=> {var p=Get<AskichProfile>("profile");p.Validate();p.settlements[0].wood=777;p.settlements[1].wood=123;p.ownedItems|=1;Check(p.Save(),"save");var restored=AskichProfile.Load();Check(restored.settlements[0].wood==777&&restored.settlements[1].wood==123&&(restored.ownedItems&1)!=0,"roundtrip");});
            Test("settlement_reachable_progression_without_resource_cheats",()=> {
                var t=new Settlement();t.Validate();
                void BuildEventually(int plot,int type){int guard=0;while(t.BuildReason(plot,type)!=""&&guard++<100)t.AdvanceDay();Check(t.Build(plot,type),"Cannot build "+type+": "+t.BuildReason(plot,type));while(t.pendingPlot>=0)t.AdvanceDay();}
                BuildEventually(0,3);BuildEventually(1,5);BuildEventually(2,2);BuildEventually(3,6);BuildEventually(4,7);BuildEventually(5,8);BuildEventually(7,1);
                BuildEventually(11,3);BuildEventually(0,3);BuildEventually(3,6);BuildEventually(8,9);BuildEventually(8,9);BuildEventually(9,10);BuildEventually(10,2);BuildEventually(7,1);
                while(t.legitimacy<50||t.food<40)t.AdvanceDay();Check(t.Reform(true,1),"confederation unreachable");Check(t.Hall==3&&t.constitution==1,"progression");
            });
            Test("touch_multitouch_and_press_edges",()=> {var input=new TouchControls();var points=new[]{TouchControls.Stick+Vector2.right*68,TouchControls.Attack,TouchControls.Heavy};input.Evaluate(points);Check(input.move.x==1&&input.attack&&input.heavy,"multitouch");input.Evaluate(points);Check(!input.heavy&&input.attack,"held heavy repeats");input.Evaluate(new Vector2[0]);Check(input.move==Vector2.zero&&!input.attack&&!input.block,"release");input.Evaluate(new[]{TouchControls.Shield,TouchControls.Dodge,TouchControls.Interact});Check(input.block&&input.dodge&&input.interact,"combat buttons");});
            Test("viewport_coordinate_roundtrip",()=> {float scale=ScreenLayout.Scale;Vector2 point=new Vector2(740,430),offset=ScreenLayout.Offset;Vector2 screen=new Vector2(offset.x+point.x*scale,Screen.height-offset.y-point.y*scale);Check(Vector2.Distance(ScreenLayout.ToUI(screen),point)<.01f,"coordinate mapping");});
            Test("legacy_file_import_preserves_original_and_existing_target",()=>{
                string old=isolated+".old",dest=isolated+".new";
                File.WriteAllText(old,"{\"version\":3,\"credits\":432,\"ownedItems\":7}");string before=File.ReadAllText(old);
                Check(AskichProfile.ImportLegacy(old,dest),"no migration");var p=JsonUtility.FromJson<AskichProfile>(File.ReadAllText(dest));
                Check(p.credits==432&&p.ownedItems==7&&p.version==4,"lost progress");
                Check(!AskichProfile.ImportLegacy(old,dest)&&File.ReadAllText(old)==before,"overwrite");File.Delete(old);File.Delete(dest);
            });
            Test("all_animation_states_and_alpha",()=>{for(int s=0;s<6;s++)for(int f=0;f<4;f++)Check(AnimatedArt.Rus(s,f)!=null,"Rus animation");for(int k=0;k<3;k++)for(int f=0;f<4;f++)Check(AnimatedArt.Villager(k,f)!=null,"resident");});
            Test("heavy_stagger_and_dodge_cancel_windup",()=>{Begin(0);Set("playerPos",Vector2.zero);Set("facing",Vector2.right);var e=Enemy(Vector2.right);Field(e,"hp",200f);Field(e,"winding",true);Call("Attack",true);Call("TickStrike",.24f);Check(Field<float>(e,"stagger")>0&&!Field<bool>(e,"winding"),"stagger");float afterHeavy=Field<float>(e,"hp");Set("attackTimer",0f);Call("Attack",false);Call("Dodge",Vector2.left);Call("TickStrike",1f);Check(Mathf.Approximately(Field<float>(e,"hp"),afterHeavy),"cancelled strike dealt damage");});
            Test("clock_events_stop_and_resolve_once",()=>{var t=new Settlement();t.Validate();Check(!t.AdvanceClock(15)&&t.day==1,"early day");Check(t.AdvanceClock(15)&&t.day==2,"clock");while(t.day<7)t.AdvanceClock(30);Check(t.pendingEvent==1,"event");int day=t.day;t.AdvanceClock(30);Check(t.day==day,"event did not pause");int wood=t.wood;Check(t.ChooseEvent(0)&&t.wood==wood-25,"event cost");Check(!t.ChooseEvent(0),"duplicate choice");});
            Test("building_move_preserves_level_and_forbids_capital",()=>{var t=new Settlement();t.Validate();Check(t.MoveBuilding(6,0)&&t.buildings[0]==2&&t.levels[0]==1&&t.buildings[6]==0,"move");Check(!t.MoveBuilding(7,1)&&!t.MoveBuilding(0,7),"capital moved");});
            Test("forge_requires_inputs_and_veche_respects_capacity",()=>{var t=new Settlement();t.Validate();t.buildings[0]=6;t.levels[0]=1;int iron=t.iron;t.AdvanceDay();Check(t.iron==iron,"free iron");t.buildings[1]=5;t.levels[1]=1;t.AdvanceDay();Check(t.iron==iron+8&&t.ore==2,"production chain");t.population=t.Capacity-1;t.government=2;t.AdvanceDay();Check(t.population==t.Capacity,"population overflow");});
            Test("first_fifteen_minutes_reach_hall_two_without_cheats",()=>{
                var t=new Settlement();t.Validate();int[] plots={0,1,2,3,4,5,7},types={3,5,2,6,7,8,1};int task=0;
                for(int second=0;second<900;second++){
                    if(t.pendingEvent!=0)t.ChooseEvent(1);
                    if(task<plots.Length&&t.pendingPlot<0&&t.Build(plots[task],types[task]))task++;
                    t.AdvanceClock(1);
                }
                Check(t.Hall>=2&&task==7&&t.population>=18&&t.Sum(8)>0,"15-minute path stalled: day "+t.day+" / task "+task+" / "+t.NextObjective);
            });
            Test("workshop_atlas_contains_eight_buildings",()=>{for(int type=3;type<=10;type++)Check(WorkshopArt.Building(type)!=null,"workshop "+type);});
            Test("trade_batches_require_market_and_debit_atomically",()=>{var t=new Settlement();t.Validate();Check(!t.TradeBatch(0,1),"trade before market");t.buildings[0]=9;t.levels[0]=1;t.wood=149;int stone=t.stone;Check(!t.TradeBatch(0,5)&&t.wood==149&&t.stone==stone,"partial trade");t.wood=150;Check(t.TradeBatch(0,5)&&t.wood==0&&t.stone==stone+100,"batch cost");Check(!t.TradeBatch(-1,1)&&!t.TradeBatch(0,0)&&!t.TradeBatch(0,11),"invalid offer");});
            Test("trade_all_goods_and_capacity_guard",()=>{var t=new Settlement();t.Validate();t.buildings[0]=9;t.levels[0]=1;int iron=t.iron,food=t.food,ore=t.ore;t.wood=120;Check(t.Trade(1)&&t.iron==iron+10,"iron");Check(t.Trade(2)&&t.food==food+30,"food");Check(t.Trade(3)&&t.ore==ore+20,"ore");t.stone=99990;int wood=t.wood;Check(!t.Trade(0)&&t.wood==wood,"warehouse overflow charged");});
            Test("construction_cancel_refunds_once_and_keeps_previous_level",()=>{var t=new Settlement();t.Validate();int wood=t.wood,stone=t.stone;Check(t.Build(0,2)&&t.CancelConstruction(),"cancel");Check(t.wood==wood&&t.stone==stone&&t.buildings[0]==0&&!t.CancelConstruction(),"duplicate refund");t.levels[7]=2;Check(t.Build(6,2)&&t.CancelConstruction()&&t.levels[6]==1,"upgrade lost old house");});
            Test("construction_cancel_respects_storage_capacity",()=>{var t=new Settlement();t.Validate();Check(t.Build(0,2),"build");t.wood=99999;int plot=t.pendingPlot;Check(!t.CancelConstruction()&&t.pendingPlot==plot&&t.wood==99999,"overflow cancellation lost materials");});
            bool passed=results.TrueForAll(r=>r.passed);
            string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-askich-test-output");string output=at>=0?args[at+1]:Path.Combine(Application.persistentDataPath,"smoke.json");
            File.WriteAllText(output,JsonUtility.ToJson(new Report{passed=passed,tests=results.ToArray()},true));
            Debug.Log("ASKICH_SMOKE_"+(passed?"PASS":"FAIL")+" "+results.Count);
            Set("persistenceEnabled",false);if(File.Exists(isolated))File.Delete(isolated);AskichProfile.SavePathOverride=null;Application.Quit(passed?0:1);
        }
    }
}
#endif
