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
    /// Opt-in rendered playtest using ordinary keyboard/mouse input in this player only.
    /// Run a development build with -askich-preview -askich-preview-output ABSOLUTE_DIRECTORY.
    /// It never changes combat stats or actor positions and is absent from release players.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class ArenaPreview : MonoBehaviour
    {
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        ArenaGame game;
        Camera arenaCamera;
        Keyboard keyboard;
        Mouse mouse;
        readonly List<InputDevice> disabledDevices = new List<InputDevice>();
        readonly List<string> errors = new List<string>();
        readonly HashSet<string> validCaptures = new HashSet<string>();
        string directory;
        bool started, finished, menuCaptured, gameplayCaptured, upgradeCaptured;
        bool prologueCaptured, hangarCaptured, secondLocationCaptured;
        bool originalBackground;
        float originalVolume;
        int originalVSync, originalFrameRate;
        InputSettings.BackgroundBehavior originalInputBackground;
        float runStarted;
        int renderedFrames, selectedUpgrade = -1, observedWave;
        float waveBegan;

        [Serializable]
        sealed class PlaytestReport
        {
            public string kind = "Real-frame development player playtest";
            public string unityVersion;
            public bool passed;
            public string mode;
            public float seconds;
            public int frames;
            public int wave;
            public int kills;
            public int score;
            public float health;
            public int upgradeChosen;
            public int currencyCollected;
            public bool menuScreenshot;
            public bool gameplayScreenshot;
            public bool upgradeScreenshot;
            public bool prologueScreenshot;
            public bool hangarScreenshot;
            public bool secondLocationScreenshot;
            public string[] errors;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-askich-preview") < 0 || Array.IndexOf(args, "-askich-smoke") >= 0) return;
            new GameObject("Development preview player").AddComponent<ArenaPreview>();
        }

        IEnumerator Start()
        {
            originalBackground = Application.runInBackground;
            originalInputBackground = InputSystem.settings.backgroundBehavior;
            originalVolume = AudioListener.volume;
            originalVSync = QualitySettings.vSyncCount;
            originalFrameRate = Application.targetFrameRate;
            AudioListener.volume = 0;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Application.logMessageReceived += OnLog;
            game = FindFirstObjectByType<ArenaGame>();
            if (game == null) { Debug.LogError("ASKICH_PREVIEW_FAIL: No ArenaGame scene component."); Application.Quit(1); yield break; }
            // Preview may read the real cosmetic selection but never writes the player's profile.
            typeof(ArenaGame).GetField("persistenceEnabled", Fields).SetValue(game, false);
            arenaCamera = Get<Camera>("gameCamera");
            directory = OutputDirectory();
            Directory.CreateDirectory(directory);

            // These are devices inside this Unity process; the OS and other games are untouched.
            foreach (InputDevice device in InputSystem.devices)
                if (device.enabled && (device is Keyboard || device is Mouse)) disabledDevices.Add(device);
            foreach (InputDevice device in disabledDevices) InputSystem.DisableDevice(device);
            keyboard = InputSystem.AddDevice<Keyboard>("Preview keyboard");
            mouse = InputSystem.AddDevice<Mouse>("Preview mouse");
            SetInput(new List<Key>(), new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), false);

            yield return new WaitForSecondsRealtime(0.8f);
            yield return Capture("menu.png");
            menuCaptured = validCaptures.Contains("menu.png");
            Call("OpenShop");
            yield return new WaitForSecondsRealtime(0.25f);
            yield return Capture("hangar.png");
            hangarCaptured = validCaptures.Contains("hangar.png");
            Call("CloseShop");
            game.StartRun();
            yield return new WaitForSecondsRealtime(0.25f);
            yield return Capture("prologue.png");
            prologueCaptured = validCaptures.Contains("prologue.png");
            Call("ContinueStory", 0);
            runStarted = Time.unscaledTime;
            observedWave = Get<int>("wave"); waveBegan = runStarted;
            started = true;

            while (Time.unscaledTime - runStarted < 100f && !finished)
            {
                string mode = Mode();
                float elapsed = Time.unscaledTime - runStarted;
                if (!gameplayCaptured && elapsed >= 9f && mode == "Playing")
                {
                    yield return Capture("location1.png");
                    gameplayCaptured = validCaptures.Contains("location1.png");
                }
                if (mode == "Story")
                {
                    yield return new WaitForSecondsRealtime(0.3f);
                    yield return Capture("dialogue-" + Get<int>("wave") + ".png");
                    Call("ContinueStory", 0);
                }
                if (!secondLocationCaptured && mode == "Playing" && Get<int>("wave") >= 3 && Time.unscaledTime - waveBegan >= 6)
                {
                    yield return Capture("location2.png");
                    secondLocationCaptured = validCaptures.Contains("location2.png");
                }
                if (mode == "Upgrade")
                {
                    if (!upgradeCaptured)
                    {
                        yield return new WaitForSecondsRealtime(0.35f);
                        yield return Capture("upgrades.png");
                        upgradeCaptured = validCaptures.Contains("upgrades.png");
                    }
                    int[] offered = Get<int[]>("offered");
                    int choice = offered[0];
                    foreach (int candidate in offered) if (candidate == 0 || candidate == 1 || candidate == 3) { choice = candidate; break; }
                    if (Get<float>("health") < 65 && Array.IndexOf(offered, 2) >= 0) choice = 2;
                    Call("ApplyUpgrade", choice);
                    selectedUpgrade = choice;
                }
                if (mode == "Defeat" || mode == "Victory")
                {
                    yield return Capture(mode.ToLowerInvariant() + ".png");
                    finished = true;
                }
                if (menuCaptured && gameplayCaptured && upgradeCaptured && prologueCaptured && hangarCaptured && secondLocationCaptured && selectedUpgrade >= 0) finished = true;
                yield return null;
            }
            if (!gameplayCaptured && Mode() == "Playing")
            {
                yield return Capture("location1.png");
                gameplayCaptured = validCaptures.Contains("location1.png");
            }
            finished = true;
            SetInput(new List<Key>(), new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), false);
            var report = new PlaytestReport
            {
                unityVersion = Application.unityVersion,
                mode = Mode(),
                seconds = Time.unscaledTime - runStarted,
                frames = renderedFrames,
                wave = Get<int>("wave"),
                kills = Get<int>("kills"),
                score = Get<int>("score"),
                health = Get<float>("health"),
                upgradeChosen = selectedUpgrade,
                currencyCollected = Get<int>("runCredits"),
                menuScreenshot = menuCaptured,
                gameplayScreenshot = gameplayCaptured,
                upgradeScreenshot = upgradeCaptured,
                prologueScreenshot = prologueCaptured,
                hangarScreenshot = hangarCaptured,
                secondLocationScreenshot = secondLocationCaptured,
                errors = errors.ToArray()
            };
            report.passed = menuCaptured && gameplayCaptured && upgradeCaptured && prologueCaptured && hangarCaptured && secondLocationCaptured && report.wave >= 3
                && report.kills > 0 && report.health > 0 && report.currencyCollected > 0 && errors.Count == 0;
            File.WriteAllText(Path.Combine(directory, "playtest.json"), JsonUtility.ToJson(report, true));
            Debug.Log(report.passed ? "ASKICH_PREVIEW_PASS" : "ASKICH_PREVIEW_FAIL");
            yield return new WaitForSecondsRealtime(0.3f);
            Application.Quit(report.passed ? 0 : 1);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        void Update()
        {
            if (!started || finished || keyboard == null || game == null) return;
            renderedFrames++;
            int wave = Get<int>("wave");
            if (wave != observedWave) { observedWave = wave; waveBegan = Time.unscaledTime; }
            string mode = Mode();
            if (mode == "Paused")
            {
                // The normal focus-loss handler pauses; unattended play explicitly resumes it.
                FieldInfo field = typeof(ArenaGame).GetField("mode", Fields);
                field.SetValue(game, Enum.Parse(field.FieldType, "Playing"));
                mode = "Playing";
            }
            if (mode != "Playing")
            {
                SetInput(new List<Key>(), new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), false);
                return;
            }
            Vector2 pilot = Get<Vector2>("playerPos");
            float phase = (Time.unscaledTime - runStarted) * 0.48f;
            Vector2 waypoint = new Vector2(Mathf.Cos(phase) * 8, Mathf.Sin(phase) * 3.5f);
            Vector2 movement = (waypoint - pilot).normalized;
            IList enemies = Get<IList>("enemies");
            Vector2 aim = pilot + Vector2.right;
            float nearest = float.MaxValue;
            foreach (object enemy in enemies)
            {
                Vector2 position = (Vector2)enemy.GetType().GetField("pos").GetValue(enemy);
                Vector2 delta = position - pilot;
                float distance = delta.magnitude;
                if (distance < nearest) { nearest = distance; aim = position; }
                if (distance < 2.2f && distance > 0.01f) movement -= delta.normalized * (2.2f - distance) * 1.1f;
            }
            // Seek real collectible drops when there is room to do so, while aiming at threats.
            if (nearest > 3.2f)
            {
                IList drops = Get<IList>("shards");
                float closestDrop = 6f;
                foreach (object drop in drops)
                {
                    Vector2 position = (Vector2)drop.GetType().GetField("pos").GetValue(drop);
                    float distance = Vector2.Distance(position, pilot);
                    if (distance < closestDrop) { closestDrop = distance; movement = (position - pilot).normalized; }
                }
            }
            var keys = new List<Key>();
            if (movement.x > 0.2f) keys.Add(Key.D);
            else if (movement.x < -0.2f) keys.Add(Key.A);
            if (movement.y > 0.2f) keys.Add(Key.W);
            else if (movement.y < -0.2f) keys.Add(Key.S);
            if (nearest < 1.25f && Get<float>("dashCooldown") <= 0) keys.Add(Key.Space);
            Vector2 screenAim = arenaCamera.WorldToScreenPoint(aim);
            SetInput(keys, screenAim, enemies.Count > 0);
        }

        void SetInput(List<Key> keys, Vector2 position, bool firing)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys.ToArray()));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = (ushort)(firing ? 1 : 0) });
            InputSystem.Update();
            keyboard.MakeCurrent();
            mouse.MakeCurrent();
        }

        IEnumerator Capture(string fileName)
        {
            yield return new WaitForEndOfFrame();
            Texture2D texture = null;
            try
            {
                texture = ScreenCapture.CaptureScreenshotAsTexture();
                if (texture == null || texture.width < 16 || texture.height < 16)
                    throw new InvalidOperationException("The player did not render a usable frame.");
                Color32[] pixels = texture.GetPixels32();
                int visibleSamples = 0;
                int stride = Mathf.Max(1, pixels.Length / 4096);
                for (int i = 0; i < pixels.Length; i += stride)
                    if (pixels[i].r > 24 || pixels[i].g > 24 || pixels[i].b > 24) visibleSamples++;
                if (visibleSamples < 8)
                    throw new InvalidOperationException("Screenshot is black; launch a visible graphics window.");
                File.WriteAllBytes(Path.Combine(directory, fileName), texture.EncodeToPNG());
                validCaptures.Add(fileName);
            }
            catch (Exception error)
            {
                Debug.LogError("ASKICH_PREVIEW_CAPTURE_ERROR " + fileName + ": " + error.Message);
            }
            finally
            {
                if (texture != null) Destroy(texture);
            }
        }

        T Get<T>(string field) => (T)typeof(ArenaGame).GetField(field, Fields).GetValue(game);
        void Call(string name, params object[] arguments) => typeof(ArenaGame).GetMethod(name, Fields).Invoke(game, arguments);
        string Mode() => typeof(ArenaGame).GetField("mode", Fields).GetValue(game).ToString();
        void OnLog(string message, string stack, LogType type)
        {
            if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && errors.Count < 20) errors.Add(message);
        }

        static string OutputDirectory()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-askich-preview-output");
            if (index < 0) return Path.Combine(Application.persistentDataPath, "Preview");
            if (index + 1 >= args.Length || !Path.IsPathRooted(args[index + 1]))
                throw new ArgumentException("-askich-preview-output requires an absolute directory.");
            return args[index + 1];
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            foreach (InputDevice device in disabledDevices) if (device.added) InputSystem.EnableDevice(device);
            Application.runInBackground = originalBackground;
            InputSystem.settings.backgroundBehavior = originalInputBackground;
            AudioListener.volume = originalVolume;
            QualitySettings.vSyncCount = originalVSync;
            Application.targetFrameRate = originalFrameRate;
        }
    }
}
#endif
