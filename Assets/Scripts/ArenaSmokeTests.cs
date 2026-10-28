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
    /// <summary>
    /// Opt-in integration checks for a development player. Not included in release players.
    /// Run with -askich-smoke [-askich-test-output absolute/results.json].
    /// </summary>
    public sealed class ArenaSmokeTests : MonoBehaviour
    {
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        readonly List<Result> results = new List<Result>();
        readonly List<InputDevice> disabledDevices = new List<InputDevice>();
        ArenaGame game;
        Keyboard testKeyboard;
        static string originalSavePath;
        static string testSavePath;

        [Serializable]
        sealed class Result
        {
            public string name;
            public bool passed;
            public string detail;
        }

        [Serializable]
        sealed class Report
        {
            public string suite = "Askich runtime smoke";
            public string unityVersion;
            public string utc;
            public bool passed;
            public Result[] tests;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void IsolateProfile()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-askich-smoke") < 0) return;
            originalSavePath = AskichProfile.SavePathOverride;
            testSavePath = Path.Combine(Path.GetTempPath(), "askich-smoke-" + Guid.NewGuid().ToString("N") + ".json");
            AskichProfile.SavePathOverride = testSavePath;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-askich-smoke") < 0) return;
            new GameObject("Development smoke runner").AddComponent<ArenaSmokeTests>();
        }

        IEnumerator Start()
        {
            // Let the scene's Awake and initial Input System update complete first.
            yield return null;
            bool passed = false;
            try
            {
                game = FindFirstObjectByType<ArenaGame>();
                Require(game != null, "The built scene did not contain ArenaGame.");
                game.enabled = false;
                foreach (InputDevice device in InputSystem.devices)
                {
                    if (device.enabled && (device is Keyboard || device is Mouse)) disabledDevices.Add(device);
                }
                foreach (InputDevice device in disabledDevices) InputSystem.DisableDevice(device);
                testKeyboard = InputSystem.AddDevice<Keyboard>("Smoke keyboard");
                ReleaseKeys();

                Check("prologue_gates_first_wave", TestStartRun);
                Check("swept_projectile_kills_enemy_and_scores", TestProjectileKill);
                Check("enemy_contact_causes_defeat", TestContactDefeat);
                Check("lethal_hit_stops_scoring_healing_and_spawning_in_same_frame", TestDefeatStopsFrame);
                Check("dash_input_grants_temporary_invulnerability", TestDash);
                Check("pause_stops_simulation_time", TestPause);
                Check("wave_completion_offers_three_unique_upgrades", TestUpgradeOffer);
                Check("selected_upgrade_changes_stats_and_advances_wave", TestUpgradeApplication);
                Check("five_waves_reach_victory", TestVictory);
                Check("restart_clears_actors_and_upgrade_state", TestRestart);
                Check("collectible_currency_survives_defeat_and_restart", TestCurrency);
                Check("locked_skins_require_currency_and_equip_without_repaying", TestSkinShop);
                Check("story_choice_changes_stats_and_is_single_use", TestStoryChoice);
                Check("story_key_does_not_also_select_upgrade", TestStoryKey);
                Check("wave_three_changes_location", TestLocations);
                Check("profile_round_trip_preserves_wallet_unlocks_and_record", TestProfileRoundTrip);
                Check("corrupt_profile_falls_back_to_safe_defaults", TestCorruptProfile);
                passed = results.Count == 17 && results.TrueForAll(result => result.passed);
            }
            catch (Exception error)
            {
                results.Add(new Result { name = "smoke_harness", passed = false, detail = Explain(error) });
            }
            finally
            {
                if (testKeyboard != null) InputSystem.RemoveDevice(testKeyboard);
                foreach (InputDevice device in disabledDevices) if (device.added) InputSystem.EnableDevice(device);
                // All saves were redirected before the scene Awake; never touch the real profile.
                if (game != null) Set("persistenceEnabled", false);
                AskichProfile.SavePathOverride = originalSavePath;
                if (!string.IsNullOrEmpty(testSavePath) && File.Exists(testSavePath)) File.Delete(testSavePath);
            }

            var report = new Report
            {
                unityVersion = Application.unityVersion,
                utc = DateTime.UtcNow.ToString("o"),
                passed = passed,
                tests = results.ToArray()
            };
            try
            {
                string output = OutputPath();
                string directory = Path.GetDirectoryName(output);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(output, JsonUtility.ToJson(report, true));
            }
            catch (Exception error)
            {
                passed = false;
                Debug.LogError("ASKICH_SMOKE_REPORT_ERROR " + error.Message);
            }
            Debug.Log(passed ? "ASKICH_SMOKE_PASS" : "ASKICH_SMOKE_FAIL");
            Application.Quit(passed ? 0 : 1);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        void TestStartRun()
        {
            game.StartRun();
            Require(Mode() == "Story" && Get<int>("wave") == 0, "The prologue did not precede combat.");
            Call("Update");
            Require(Mode() == "Story" && List("enemies").Count == 0, "Enemies appeared while reading the prologue.");
            Call("ContinueStory", 0);
            AssertInitialState();
            Require(Get<float>("spawnTimer") > 0, "First wave did not provide a spawn grace period.");
        }

        void TestProjectileKill()
        {
            StartPlaying();
            Call("SpawnEnemy");
            object enemy = List("enemies")[0];
            SetObject(enemy, "pos", new Vector2(1.2f, 0));
            SetObject(enemy, "hp", 32f);
            int beforeScore = Get<int>("score");
            // Neither end of this 2.2-unit step overlaps the enemy: this exercises swept hits.
            Call("Fire", Vector2.zero, Vector2.right, false, 100f, 0);
            Call("UpdateShots", 0.1f);
            Require(List("enemies").Count == 0, "Projectile crossed the enemy without killing it.");
            Require(List("shots").Count == 0, "Non-piercing projectile survived its hit.");
            Require(Get<int>("kills") == 1 && Get<int>("score") > beforeScore, "Kill did not update the score.");
        }

        void TestContactDefeat()
        {
            StartPlaying();
            Call("SpawnEnemy");
            SetObject(List("enemies")[0], "pos", Vector2.zero);
            Set("health", 10f);
            Call("UpdateEnemies", 0f);
            Require(Get<float>("health") == 0 && Mode() == "Defeat", "Lethal enemy contact did not enter Defeat.");
            Require(Get<bool>("savedResult"), "Defeat did not finish the run.");
        }

        void TestDefeatStopsFrame()
        {
            // Cover defeat in both UpdateEnemies and UpdateShots; later stages must stop.
            foreach (bool projectile in new[] { false, true })
            {
                StartPlaying();
                Set("kills", 6);
                Call("SpawnEnemy");
                SetObject(List("enemies")[0], "pos", Vector2.zero);
                Call("KillEnemy", 0); // Place a real repair pickup directly under the pilot.
                Require(List("pickups").Count == 1, "Defeat fixture did not create a repair pickup.");

                Call("SpawnEnemy");
                object target = List("enemies")[0];
                SetObject(target, "pos", new Vector2(2, 0));
                SetObject(target, "hp", 1f);
                SetObject(target, "kind", 0);
                Call("Fire", new Vector2(2, 0), Vector2.left, false, 100f, 0);
                if (projectile)
                {
                    // Reverse projectile traversal processes this lethal shot before our shot.
                    Call("Fire", Vector2.zero, Vector2.right, true, 100f, 0);
                }
                else
                {
                    Call("SpawnEnemy");
                    SetObject(List("enemies")[1], "pos", Vector2.zero);
                    SetObject(List("enemies")[1], "kind", 0);
                }

                Set("health", 1f);
                Set("hurtTimer", 0f);
                Set("remaining", 1);
                Set("spawnTimer", -1f); // A spawn is due, but must not occur after death.
                int beforeScore = Get<int>("score");
                int beforeKills = Get<int>("kills");
                int beforeEnemies = List("enemies").Count;
                int beforeCurrency = Get<int>("credits");
                int beforeShards = List("shards").Count;
                Call("Update");
                string cause = projectile ? "projectile" : "contact";
                Require(Mode() == "Defeat" && Get<float>("health") == 0,
                    "Lethal " + cause + " was followed by a repair pickup in the same frame.");
                Require(Get<int>("score") == beforeScore && Get<int>("kills") == beforeKills,
                    "Combat continued scoring after lethal " + cause + ".");
                Require(List("pickups").Count == 1 && List("enemies").Count == beforeEnemies
                    && Get<int>("remaining") == 1,
                    "Actors were collected, killed or spawned after lethal " + cause + ".");
                Require(Get<int>("credits") == beforeCurrency && List("shards").Count == beforeShards,
                    "Currency was collected after lethal " + cause + ".");
            }
        }

        void TestDash()
        {
            StartPlaying();
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.Space));
            InputSystem.Update();
            Call("UpdatePlayer", 0f);
            Require(Get<float>("dashTimer") > 0 && Get<float>("dashCooldown") > 0, "Space did not start a dash.");
            Call("Hurt", 500f);
            Require(Get<float>("health") == 100 && Mode() == "Playing", "Damage bypassed dash invulnerability.");
            ReleaseKeys();
            // Advance the actual player timer, then verify protection expires.
            Call("UpdatePlayer", 0.3f);
            Call("Hurt", 500f);
            Require(Mode() == "Defeat", "Dash protection did not expire.");
        }

        void TestPause()
        {
            ReleaseKeys();
            StartPlaying();
            Set("elapsed", 12.5f);
            SetMode("Paused");
            int beforeRemaining = Get<int>("remaining");
            Call("Update");
            Require(Mode() == "Paused", "Pause unexpectedly resumed.");
            Require(Get<float>("elapsed") == 12.5f && Get<int>("remaining") == beforeRemaining,
                "Paused Update advanced simulation time or wave spawns.");
        }

        void TestUpgradeOffer()
        {
            StartPlaying();
            Set("health", 60f);
            Call("Fire", new Vector2(8, 4), Vector2.right, true, 10f, 0);
            CompleteCurrentWave();
            Require(Mode() == "Story", "Wave completion did not reveal its story beat.");
            Call("ContinueStory", 0);
            Require(Mode() == "Upgrade", "An empty completed wave did not enter upgrade selection.");
            int[] choices = Get<int[]>("offered");
            Require(choices.Length == 3 && new HashSet<int>(choices).Count == 3, "Upgrade choices are not three unique cards.");
            foreach (int choice in choices) Require(choice >= 0 && choice <= 5, "An invalid upgrade was offered.");
            Require(Get<float>("health") > 60, "Wave completion did not repair the pilot.");
            Require(List("shots").Count == 0, "Wave completion left projectiles active.");
        }

        void TestUpgradeApplication()
        {
            StartPlaying();
            CompleteToUpgrade();
            int choice = Get<int[]>("offered")[0];
            string field = new[] { "damage", "fireInterval", "maxHealth", "spread", "moveSpeed", "piercing" }[choice];
            double before = Convert.ToDouble(Field(field).GetValue(game));
            Call("ApplyUpgrade", choice);
            double after = Convert.ToDouble(Field(field).GetValue(game));
            Require(choice == 1 ? after < before : after > before, "Selected upgrade did not improve its stat.");
            Require(Mode() == "Playing" && Get<int>("wave") == 2, "Upgrade did not advance to wave two.");
            Require(Get<int>("remaining") > 0, "New wave has no queued enemies.");
            Call("ApplyUpgrade", choice);
            Require(Convert.ToDouble(Field(field).GetValue(game)) == after && Get<int>("wave") == 2,
                "Upgrade was applied outside the selection screen.");
        }

        void TestVictory()
        {
            StartPlaying();
            for (int completed = 1; completed <= 5; completed++)
            {
                Require(Get<int>("wave") == completed, "Wave progression skipped or repeated a wave.");
                CompleteCurrentWave();
                if (completed < 5)
                {
                    Require(Mode() == "Story", "An intermediate wave did not reveal its story.");
                    Call("ContinueStory", 0);
                    Require(Mode() == "Upgrade", "Story continuation did not offer an upgrade.");
                    Call("ApplyUpgrade", Get<int[]>("offered")[0]);
                }
            }
            Require(Mode() == "Victory" && Get<bool>("savedResult"), "Clearing wave five did not finish in Victory.");
            Require(Get<int>("score") > 0, "Victory did not award its survival bonus.");
        }

        void TestRestart()
        {
            StartPlaying();
            CompleteToUpgrade();
            Call("ApplyUpgrade", 3);
            CompleteToUpgrade();
            Call("ApplyUpgrade", 5);
            CompleteToUpgrade();
            Call("ApplyUpgrade", 2);
            Set("kills", 6);
            Call("SpawnEnemy");
            Call("KillEnemy", 0); // Seventh kill creates a repair pickup.
            Call("SpawnEnemy");
            Call("Fire", Vector2.zero, Vector2.right, false, 10f, 0);
            Require(List("enemies").Count > 0 && List("shots").Count > 0 && List("pickups").Count > 0,
                "Restart fixture did not contain the expected combat actors.");
            game.StartRun();
            Require(Mode() == "Story" && Get<int>("wave") == 0, "Restart skipped the prologue.");
            Call("ContinueStory", 0);
            AssertInitialState();
            foreach (string list in new[] { "enemies", "shots", "sparks", "pickups", "shards" })
                Require(List(list).Count == 0, "Restart left active " + list + ".");
        }

        void TestCurrency()
        {
            StartPlaying();
            int before = Get<int>("credits");
            Call("SpawnEnemy");
            SetObject(List("enemies")[0], "pos", Vector2.zero);
            Call("KillEnemy", 0);
            Require(List("shards").Count > 0, "Defeated enemy did not drop collectible currency.");
            Require(Get<int>("credits") == before, "Currency was awarded before collecting the drop.");
            int value = 0;
            foreach (object shard in List("shards"))
            {
                SetObject(shard, "pos", Vector2.zero);
                value += Convert.ToInt32(shard.GetType().GetField("value").GetValue(shard));
            }
            Call("UpdateShards", 0f);
            Require(value > 0 && List("shards").Count == 0, "Currency pickup did not consume its drop.");
            Require(Get<int>("credits") == before + value && Get<int>("runCredits") == value, "Currency pickup updated the wrong balances.");
            Call("UpdateShards", 0f);
            Require(Get<int>("credits") == before + value, "A collected shard paid twice.");
            Require(AskichProfile.Load().credits == before + value, "Collected currency was not saved.");
            Call("Hurt", 500f);
            Require(Mode() == "Defeat" && Get<int>("credits") == before + value, "Defeat removed the wallet.");
            game.StartRun();
            Require(Get<int>("credits") == before + value && Get<int>("runCredits") == 0, "Restart did not retain only persistent currency.");
        }

        void TestSkinShop()
        {
            SetMode("Menu");
            Call("OpenShop");
            Require(Mode() == "Shop", "Hangar did not open from the menu.");
            Set("credits", 179);
            Set("skinUnlocked", new[] { true, false, false });
            Set("selectedSkin", 0);
            bool denied = (bool)CallResult("PurchaseOrEquipSkin", 1);
            Require(!denied && Get<int>("credits") == 179 && !Get<bool[]>("skinUnlocked")[1], "A locked skin could be bought without enough currency.");
            Set("credits", 700);
            Require((bool)CallResult("PurchaseOrEquipSkin", 1), "First locked skin could not be purchased.");
            Require(Get<int>("credits") == 520 && Get<bool[]>("skinUnlocked")[1] && Get<int>("selectedSkin") == 1,
                "First skin price, unlock or equipment was incorrect.");
            Require((bool)CallResult("PurchaseOrEquipSkin", 2), "Second locked skin could not be purchased.");
            Require(Get<int>("credits") == 0 && Get<bool[]>("skinUnlocked")[2] && Get<int>("selectedSkin") == 2,
                "Second skin price, unlock or equipment was incorrect.");
            Require((bool)CallResult("PurchaseOrEquipSkin", 1) && Get<int>("credits") == 0 && Get<int>("selectedSkin") == 1,
                "Equipping an owned skin charged currency again.");
            Require(!(bool)CallResult("PurchaseOrEquipSkin", -1) && !(bool)CallResult("PurchaseOrEquipSkin", 3),
                "Shop accepted an invalid skin index.");
            AskichProfile saved = AskichProfile.Load();
            Require(saved.credits == 0 && (saved.unlockedSkins & 7) == 7 && saved.selectedSkin == 1,
                "Purchased skins or equipment were not persisted.");
            Call("CloseShop");
            Require(Mode() == "Menu", "Closing the hangar did not return to the menu.");
        }

        void TestStoryChoice()
        {
            foreach (int choice in new[] { 0, 1 })
            {
                StartPlaying();
                CompleteToUpgrade(); Call("ApplyUpgrade", Get<int[]>("offered")[0]);
                CompleteCurrentWave();
                Require(Mode() == "Story" && Get<int>("wave") == 2, "The junction dialogue is not before location two.");
                float beforeHealth = Get<float>("maxHealth"), beforeDamage = Get<float>("damage");
                Call("ContinueStory", choice);
                Require(Mode() == "Upgrade", "The story choice did not continue to upgrades.");
                if (choice == 0)
                    Require(Get<bool>("rescuedArchive") && Mathf.Approximately(Get<float>("maxHealth"), beforeHealth + 15), "Rescuing the archive did not grant its promised bonus.");
                else
                    Require(!Get<bool>("rescuedArchive") && Mathf.Approximately(Get<float>("damage"), beforeDamage * 1.1f), "Silencing the beacon did not grant its promised bonus.");
                float afterHealth = Get<float>("maxHealth"), afterDamage = Get<float>("damage");
                Call("ContinueStory", choice);
                Require(Get<float>("maxHealth") == afterHealth && Get<float>("damage") == afterDamage, "A dialogue choice could be claimed twice.");
            }
        }

        void TestStoryKey()
        {
            StartPlaying();
            CompleteToUpgrade(); Call("ApplyUpgrade", Get<int[]>("offered")[0]);
            CompleteCurrentWave();
            Require(Mode() == "Story" && Get<int>("wave") == 2, "The choice dialogue was not prepared.");
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.Digit1));
            InputSystem.Update();
            Call("Update");
            ReleaseKeys();
            Require(Mode() == "Upgrade" && Get<int>("wave") == 2,
                "A single story-choice key also selected an upgrade and skipped the upgrade screen.");
            Require(Get<bool>("rescuedArchive"), "The first keyboard choice did not rescue the archive.");
        }

        void TestLocations()
        {
            StartPlaying();
            Require((int)typeof(ArenaGame).GetProperty("LocationIndex").GetValue(game) == 0, "First wave is not at the cold dock.");
            for (int i = 0; i < 2; i++) { CompleteToUpgrade(); Call("ApplyUpgrade", Get<int[]>("offered")[0]); }
            Require(Get<int>("wave") == 3 && (int)typeof(ArenaGame).GetProperty("LocationIndex").GetValue(game) == 1,
                "The third wave did not move to the living foundry.");
            Require(Get<Transform>("world").childCount > 0, "The second location has no geometry.");
        }

        void TestProfileRoundTrip()
        {
            var profile = new AskichProfile { credits = 734, unlockedSkins = 7, selectedSkin = 2, bestScore = 42000 };
            Require(profile.Save(), "Profile save reported failure.");
            AskichProfile loaded = AskichProfile.Load();
            Require(loaded.credits == 734 && loaded.unlockedSkins == 7 && loaded.selectedSkin == 2 && loaded.bestScore == 42000,
                "A saved profile did not survive a fresh load.");
        }

        void TestCorruptProfile()
        {
            File.WriteAllText(testSavePath, "{broken json");
            AskichProfile profile = AskichProfile.Load();
            Require(profile.credits >= 0 && (profile.unlockedSkins & 1) == 1 && profile.selectedSkin == 0,
                "Invalid profile data did not fall back to the default skin and a safe balance.");
            profile.credits = -20; profile.selectedSkin = 99; profile.unlockedSkins = 0; profile.Validate();
            Require(profile.credits >= 0 && profile.selectedSkin == 0 && (profile.unlockedSkins & 1) == 1,
                "Malformed profile fields could create a negative balance or equip a locked skin.");
        }

        void StartPlaying()
        {
            ReleaseKeys(); game.StartRun(); Call("ContinueStory", 0);
            Require(Mode() == "Playing", "Prologue did not start the first wave.");
        }

        void CompleteToUpgrade()
        {
            CompleteCurrentWave();
            Require(Mode() == "Story", "Intermediate wave did not reveal story.");
            Call("ContinueStory", 0);
            Require(Mode() == "Upgrade", "Story did not lead to an upgrade.");
        }

        void AssertInitialState()
        {
            Require(Mode() == "Playing" && Get<int>("wave") == 1 && Get<int>("remaining") > 0, "Run did not start on wave one.");
            Require(Get<float>("health") == 100 && Get<float>("maxHealth") == 100, "Health was not reset.");
            Require(Get<float>("damage") == 22 && Mathf.Approximately(Get<float>("fireInterval"), 0.17f)
                && Mathf.Approximately(Get<float>("moveSpeed"), 5.2f), "Weapon or movement upgrades survived restart.");
            Require(Get<int>("spread") == 0 && Get<int>("piercing") == 0, "Projectile upgrades survived restart.");
            Require(Get<int>("score") == 0 && Get<int>("kills") == 0 && Get<float>("elapsed") == 0, "Run counters were not reset.");
            Require(Get<Vector2>("playerPos") == Vector2.zero && !Get<bool>("savedResult"), "Pilot or result state was not reset.");
        }

        void CompleteCurrentWave()
        {
            Require(List("enemies").Count == 0, "Wave fixture still contains enemies.");
            Set("remaining", 0);
            Call("Update");
        }

        void ReleaseKeys()
        {
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            InputSystem.Update();
        }

        void Check(string name, Action action)
        {
            try { action(); results.Add(new Result { name = name, passed = true, detail = "passed" }); }
            catch (Exception error) { results.Add(new Result { name = name, passed = false, detail = Explain(error) }); }
        }

        static string Explain(Exception error)
        {
            while (error is TargetInvocationException && error.InnerException != null) error = error.InnerException;
            return error.GetType().Name + ": " + error.Message;
        }

        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static FieldInfo Field(string name) => typeof(ArenaGame).GetField(name, PrivateInstance) ?? throw new MissingFieldException(name);
        T Get<T>(string name) => (T)Field(name).GetValue(game);
        void Set(string name, object value) => Field(name).SetValue(game, value);
        IList List(string name) => Get<IList>(name);
        string Mode() => Field("mode").GetValue(game).ToString();
        void SetMode(string value) => Set("mode", Enum.Parse(Field("mode").FieldType, value));
        void Call(string name, params object[] arguments) => CallResult(name, arguments);
        object CallResult(string name, params object[] arguments)
        {
            MethodInfo method = typeof(ArenaGame).GetMethod(name, PrivateInstance) ?? throw new MissingMethodException(name);
            return method.Invoke(game, arguments);
        }
        static void SetObject(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(name);
            field.SetValue(target, value);
        }

        static string OutputPath()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-askich-test-output");
            if (index < 0) return Path.Combine(Application.persistentDataPath, "askich-smoke-results.json");
            if (index + 1 >= args.Length || !Path.IsPathRooted(args[index + 1]))
                throw new ArgumentException("-askich-test-output requires an absolute file path.");
            return args[index + 1];
        }
    }
}
#endif
