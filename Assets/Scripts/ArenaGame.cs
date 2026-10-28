using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Askich
{
    /// <summary>Small, deterministic-order arena simulation. Visual objects are only presentation.</summary>
    public sealed partial class ArenaGame : MonoBehaviour
    {
        enum Mode { Menu, Story, Playing, Upgrade, Paused, Shop, Defeat, Victory }
        sealed class Enemy
        {
            public SpriteRenderer view;
            public Vector2 pos, chargeDirection;
            public int kind;
            public float hp, maxHp, radius, timer, warning, flash;
        }
        sealed class Projectile
        {
            public SpriteRenderer view;
            public Vector2 pos, velocity;
            public float life, damage;
            public int pierce;
            public bool hostile;
            public readonly HashSet<Enemy> hit = new HashSet<Enemy>();
        }
        sealed class Spark { public SpriteRenderer view; public Vector2 pos, velocity; public float life, total; public Color color; }
        sealed class Pickup { public SpriteRenderer view; public Vector2 pos; public float age; }

        readonly List<Enemy> enemies = new List<Enemy>();
        readonly List<Projectile> shots = new List<Projectile>();
        readonly List<Spark> sparks = new List<Spark>();
        readonly List<Pickup> pickups = new List<Pickup>();
        readonly Stack<SpriteRenderer> shotPool = new Stack<SpriteRenderer>();
        readonly Stack<SpriteRenderer> sparkPool = new Stack<SpriteRenderer>();
        Transform world, actors, effects;
        Camera gameCamera;
        SpriteRenderer player, aimLine;
        SynthAudio sound;
        Mode mode;
        Vector2 playerPos, facing = Vector2.right, dashDirection;
        float health = 100, maxHealth = 100, damage = 22, fireInterval = 0.17f, moveSpeed = 5.2f;
        float fireTimer, dashTimer, dashCooldown, hurtTimer, elapsed, waveBanner, spawnTimer, shake, trailTimer;
        int wave, remaining, score, kills, bestScore, spread, piercing;
        int[] offered = new int[3];
        bool savedResult, captureRequested;
        string notice = "";
        float noticeTime;

        void Awake()
        {
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 1;
            LoadProgress();
            var cameraObject = new GameObject("Arena camera", typeof(Camera), typeof(AudioListener));
            gameCamera = cameraObject.GetComponent<Camera>();
            gameCamera.orthographic = true; gameCamera.orthographicSize = 8.1f;
            gameCamera.transform.position = new Vector3(0, 0, -10);
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = new Color(0.019f, 0.032f, 0.061f);
            sound = gameObject.AddComponent<SynthAudio>();
            world = new GameObject("Arena geometry").transform; world.SetParent(transform);
            actors = new GameObject("Actors").transform; actors.SetParent(transform);
            effects = new GameObject("Effects").transform; effects.SetParent(transform);
            CreateArena();
            player = NeonVisual.Body("Pilot", "ship", NeonVisual.Cyan, Vector2.zero, 0.9f, actors);
            ApplySkin();
            aimLine = NeonVisual.Make("Aim", "square", new Color(0.3f, 1, 0.95f, 0.25f), Vector2.zero, new Vector2(1.1f, 0.025f), 4, actors);
            mode = Mode.Menu;
            Debug.Log("ASKICH_READY");
        }

        void CreateArena()
        {
            bool foundry = LocationIndex == 1;
            Color border = foundry ? new Color(0.7f, 0.32f, 0.17f) : new Color(0.15f, 0.5f, 0.52f);
            Color accent = foundry ? NeonVisual.Gold : NeonVisual.Cyan;
            gameCamera.backgroundColor = foundry ? new Color(0.055f, 0.022f, 0.029f) : new Color(0.019f, 0.032f, 0.061f);
            NeonVisual.Make("Arena floor", "square", foundry ? new Color(0.09f, 0.043f, 0.045f) : new Color(0.035f, 0.063f, 0.095f), Vector2.zero, new Vector2(27.2f, 13.2f), -20, world);
            Color grid = foundry ? new Color(0.2f, 0.075f, 0.065f, 0.7f) : new Color(0.075f, 0.13f, 0.17f, 0.7f);
            for (int x = -12; x <= 12; x++) NeonVisual.Make("Grid vertical", "square", grid, new Vector2(x, 0), new Vector2(0.018f, 12.85f), -18, world);
            for (int y = -6; y <= 6; y++) NeonVisual.Make("Grid horizontal", "square", grid, new Vector2(0, y), new Vector2(26.8f, 0.018f), -18, world);
            NeonVisual.Make("Core halo", "glow", foundry ? new Color(1, 0.24f, 0.09f, 0.25f) : new Color(0.06f, 0.65f, 0.6f, 0.13f), Vector2.zero, Vector2.one * 11, -17, world);
            NeonVisual.Make("Core outer", "ring", foundry ? new Color(0.4f, 0.14f, 0.1f) : new Color(0.12f, 0.26f, 0.31f), Vector2.zero, Vector2.one * 3.9f, -16, world);
            NeonVisual.Make("Core inner", "ring", foundry ? new Color(0.46f, 0.22f, 0.08f) : new Color(0.12f, 0.26f, 0.31f), Vector2.zero, Vector2.one * 3.4f, -16, world);
            NeonVisual.Make("Core mark", foundry ? "hex" : "diamond", foundry ? new Color(0.35f, 0.13f, 0.075f) : new Color(0.06f, 0.11f, 0.15f), Vector2.zero, Vector2.one * 1.7f, -15, world);
            for (int side = -1; side <= 1; side += 2)
            {
                NeonVisual.Make("Perimeter", "square", border, new Vector2(0, side * 6.4f), new Vector2(27, 0.04f), -10, world);
                NeonVisual.Make("Perimeter", "square", border, new Vector2(side * 13.1f, 0), new Vector2(0.04f, 13.6f), -10, world);
                for (int x = -11; x <= 11; x += 2) NeonVisual.Make("Floor tick", "square", border * 0.7f, new Vector2(x, side * 6.2f), new Vector2(0.3f, 0.035f), -10, world);
                for (int other = -1; other <= 1; other += 2)
                {
                    NeonVisual.Make("Corner light", "square", accent, new Vector2(side * 12.6f, other * 5.95f), new Vector2(0.6f, 0.06f), -9, world);
                    NeonVisual.Make("Corner light", "square", accent, new Vector2(side * 12.87f, other * 5.7f), new Vector2(0.06f, 0.6f), -9, world);
                    if (!foundry)
                    {
                        NeonVisual.Make("Frozen loading bay", "square", new Color(0.055f, 0.12f, 0.17f), new Vector2(side * 9.2f, other * 4.7f), new Vector2(3.2f, 1.5f), -16, world);
                        NeonVisual.Make("Dock rail", "square", new Color(0.12f, 0.27f, 0.32f), new Vector2(side * 9.2f, other * 4.7f), new Vector2(3.2f, 0.07f), -15, world);
                    }
                }
            }
            if (foundry)
            {
                for (int branch = 0; branch < 12; branch++)
                {
                    float angle = branch * 30;
                    Vector2 direction = Rotate(Vector2.right, angle);
                    var root = NeonVisual.Make("Living root", "square", new Color(0.27f, 0.09f, 0.085f), direction * 3.4f, new Vector2(3.2f, 0.075f), -16, world);
                    root.transform.rotation = Quaternion.Euler(0, 0, angle);
                    NeonVisual.Make("Memory pod", "hex", new Color(0.23f, 0.085f, 0.09f), direction * 5, Vector2.one * 0.8f, -15, world);
                    NeonVisual.Make("Pod glow", "glow", new Color(1, 0.32f, 0.12f, 0.3f), direction * 5, Vector2.one * 1.5f, -14, world);
                }
            }
        }

        void RebuildArena()
        {
            if (world != null) Destroy(world.gameObject);
            world = new GameObject(LocationName).transform; world.SetParent(transform);
            CreateArena();
        }

        public void StartRun()
        {
            ClearCombat();
            playerPos = Vector2.zero; facing = Vector2.right;
            health = maxHealth = 100; damage = 22; fireInterval = 0.17f; moveSpeed = 5.2f;
            spread = piercing = score = kills = wave = runCredits = 0;
            rescuedArchive = false;
            fireTimer = dashTimer = dashCooldown = hurtTimer = elapsed = shake = 0;
            savedResult = false; RebuildArena(); ApplySkin(); ShowStory();
            Debug.Log("ASKICH_RUN_STARTED");
        }

        void NextWave()
        {
            wave++; remaining = ArenaRules.WaveSize(wave); spawnTimer = 1.8f; waveBanner = 2.3f;
            if (wave == 3)
            {
                RebuildArena(); playerPos = new Vector2(-8.5f, 0); spawnTimer = 3.3f; waveBanner = 3.3f;
                notice = "Шлюз открыт: Живая кузница"; noticeTime = 4;
                Debug.Log("ASKICH_LOCATION_2");
            }
            mode = Mode.Playing; sound.Play("upgrade");
            Debug.Log("ASKICH_WAVE_" + wave);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.mKey.wasPressedThisFrame) sound.Toggle();
                if (keyboard.f12Key.wasPressedThisFrame && !captureRequested) { captureRequested = true; StartCoroutine(Capture()); }
                if (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame)
                {
                    if (mode == Mode.Playing) mode = Mode.Paused;
                    else if (mode == Mode.Paused) mode = Mode.Playing;
                    else if (mode == Mode.Shop) CloseShop();
                }
                if ((mode == Mode.Menu || mode == Mode.Defeat || mode == Mode.Victory) && keyboard.enterKey.wasPressedThisFrame) StartRun();
                else if ((mode == Mode.Defeat || mode == Mode.Victory) && keyboard.rKey.wasPressedThisFrame) StartRun();
                else if (mode == Mode.Story)
                {
                    if (keyboard.enterKey.wasPressedThisFrame && !StoryHasChoice) ContinueStory();
                    else if (StoryHasChoice && keyboard.digit1Key.wasPressedThisFrame) ContinueStory(0);
                    else if (StoryHasChoice && keyboard.digit2Key.wasPressedThisFrame) ContinueStory(1);
                }
                else if (mode == Mode.Upgrade)
                {
                    if (keyboard.digit1Key.wasPressedThisFrame) ApplyUpgrade(offered[0]);
                    else if (keyboard.digit2Key.wasPressedThisFrame) ApplyUpgrade(offered[1]);
                    else if (keyboard.digit3Key.wasPressedThisFrame) ApplyUpgrade(offered[2]);
                }
            }
            float dt = Mathf.Min(Time.deltaTime, 0.04f);
            noticeTime -= dt;
            // Keep the entire arena visible at any aspect ratio, reserving room for the HUD.
            gameCamera.orthographicSize = Mathf.Max(8.1f, 14.4f / Mathf.Max(0.3f, gameCamera.aspect));
            if (mode == Mode.Playing)
            {
                elapsed += dt; waveBanner -= dt;
                UpdatePlayer(dt); UpdateEnemies(dt);
                if (mode == Mode.Playing) UpdateShots(dt);
                if (mode == Mode.Playing) UpdatePickups(dt);
                if (mode == Mode.Playing) UpdateShards(dt);
                if (mode == Mode.Playing) spawnTimer -= dt;
                if (mode == Mode.Playing && remaining > 0 && spawnTimer <= 0)
                {
                    SpawnEnemy(); remaining--; spawnTimer = Mathf.Max(0.28f, 0.85f - wave * 0.09f);
                }
                if (remaining == 0 && enemies.Count == 0 && mode == Mode.Playing) WaveComplete();
            }
            if (mode != Mode.Paused) UpdateSparks(dt);
            player.transform.position = playerPos;
            player.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg);
            player.color = hurtTimer > 0 && Mathf.Sin(Time.unscaledTime * 45) > 0 ? Color.white : PilotColor;
            aimLine.gameObject.SetActive(mode == Mode.Playing);
            aimLine.transform.position = playerPos + facing * 1.2f;
            aimLine.transform.rotation = player.transform.rotation;
            shake = Mathf.MoveTowards(shake, 0, dt * 2.5f);
            gameCamera.transform.position = new Vector3(Mathf.Sin(Time.unscaledTime * 91) * shake, Mathf.Cos(Time.unscaledTime * 73) * shake, -10);
        }

        void UpdatePlayer(float dt)
        {
            Keyboard k = Keyboard.current; Mouse m = Mouse.current;
            Vector2 input = Vector2.zero;
            if (k != null)
            {
                input.x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
                input.y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0);
            }
            input = Vector2.ClampMagnitude(input, 1);
            if (m != null)
            {
                Vector2 aim = (Vector2)gameCamera.ScreenToWorldPoint(m.position.ReadValue()) - playerPos;
                if (aim.sqrMagnitude > 0.01f) facing = aim.normalized;
            }
            dashCooldown = Mathf.Max(0, dashCooldown - dt); hurtTimer = Mathf.Max(0, hurtTimer - dt);
            if (k != null && (k.spaceKey.wasPressedThisFrame || k.leftShiftKey.wasPressedThisFrame || k.rightShiftKey.wasPressedThisFrame) && dashCooldown <= 0)
            {
                dashDirection = input.sqrMagnitude > 0 ? input : facing;
                dashTimer = 0.16f; dashCooldown = 1.8f; hurtTimer = Mathf.Max(hurtTimer, 0.27f); sound.Play("dash");
            }
            if (dashTimer > 0)
            {
                dashTimer -= dt; playerPos += dashDirection * 23 * dt;
                Burst(playerPos, PilotColor, 2, 1.3f);
            }
            else playerPos += input * moveSpeed * dt;
            playerPos = ArenaRules.Clamp(playerPos);
            fireTimer -= dt;
            if (m != null && m.leftButton.isPressed && fireTimer <= 0)
            {
                fireTimer = fireInterval;
                Fire(playerPos + facing * 0.5f, facing, false, damage, piercing);
                for (int i = 1; i <= spread; i++)
                {
                    Fire(playerPos + facing * 0.5f, Rotate(facing, 8 * i), false, damage * 0.72f, piercing);
                    Fire(playerPos + facing * 0.5f, Rotate(facing, -8 * i), false, damage * 0.72f, piercing);
                }
                sound.Play("shot"); Burst(playerPos + facing * 0.65f, PilotColor, 2, 2);
            }
            trailTimer -= dt;
            if (input.sqrMagnitude > 0 && trailTimer <= 0) { trailTimer = 0.045f; Burst(playerPos - facing * 0.25f, PilotColor * 0.7f, 1, 0.4f); }
        }

        void SpawnEnemy()
        {
            int kind = wave >= 3 && UnityEngine.Random.value < 0.23f ? 2 : wave >= 2 && UnityEngine.Random.value < 0.34f ? 1 : 0;
            Vector2 pos;
            // Spawns stay on the perimeter and away from the pilot.
            do
            {
                int side = UnityEngine.Random.Range(0, 4);
                pos = side < 2 ? new Vector2(side == 0 ? -12.4f : 12.4f, UnityEngine.Random.Range(-5.7f, 5.7f)) : new Vector2(UnityEngine.Random.Range(-12.2f, 12.2f), side == 2 ? -5.8f : 5.8f);
            } while ((pos - playerPos).sqrMagnitude < 16);
            float hp = (kind == 2 ? 110 : kind == 1 ? 44 : 32) * (1 + (wave - 1) * 0.12f);
            Color color = kind == 2 ? NeonVisual.Gold : kind == 1 ? NeonVisual.Violet : NeonVisual.Coral;
            enemies.Add(new Enemy { pos = pos, kind = kind, hp = hp, maxHp = hp, radius = kind == 2 ? 0.6f : 0.4f, timer = UnityEngine.Random.Range(0.7f, 1.8f),
                view = NeonVisual.Body(kind == 0 ? "Hunter" : kind == 1 ? "Sentry" : "Brute", kind == 2 ? "hex" : kind == 1 ? "diamond" : "circle", color, pos, kind == 2 ? 1.25f : 0.85f, actors) });
            Burst(pos, color, 10, 3);
        }

        void UpdateEnemies(float dt)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy e = enemies[i]; Vector2 delta = playerPos - e.pos; float distance = delta.magnitude;
                Vector2 direction = delta / Mathf.Max(0.01f, distance); e.timer -= dt; e.flash -= dt;
                Vector2 movement = direction * (2.1f + wave * 0.13f);
                if (e.kind == 1)
                {
                    movement = direction * (distance > 5.5f ? 1.45f : distance < 4 ? -1.4f : 0) + new Vector2(-direction.y, direction.x) * 0.6f;
                    if (e.timer <= 0) { Fire(e.pos + direction * 0.5f, direction, true, 12, 0); e.timer = 1.75f - wave * 0.08f; Burst(e.pos, NeonVisual.Violet, 4, 1); }
                }
                if (e.kind == 2)
                {
                    movement = direction * 1.15f;
                    if (e.timer <= 0 && e.warning <= 0) { e.warning = 0.95f; e.chargeDirection = direction; }
                    if (e.warning > 0)
                    {
                        e.warning -= dt; movement = e.warning > 0.36f ? Vector2.zero : e.chargeDirection * 10;
                        if (e.warning <= 0) e.timer = 2.9f;
                    }
                }
                Vector2 separation = Vector2.zero;
                foreach (Enemy other in enemies)
                {
                    if (ReferenceEquals(e, other)) continue;
                    Vector2 away = e.pos - other.pos; float sq = away.sqrMagnitude;
                    if (sq > 0.001f && sq < 1.15f) separation += away.normalized * (1.15f - sq) * 1.2f;
                }
                e.pos = ArenaRules.Clamp(e.pos + (movement + separation) * dt, e.radius);
                e.view.transform.position = e.pos;
                e.view.transform.Rotate(0, 0, (e.kind == 1 ? 35 : 12) * dt);
                Color color = e.kind == 2 ? NeonVisual.Gold : e.kind == 1 ? NeonVisual.Violet : NeonVisual.Coral;
                e.view.color = e.flash > 0 || e.warning > 0.36f && Mathf.Sin(Time.time * 24) > 0 ? Color.white : color;
                if ((e.pos - playerPos).sqrMagnitude < (e.radius + 0.28f) * (e.radius + 0.28f)) Hurt(e.kind == 2 ? 24 : 14);
            }
        }

        void Fire(Vector2 pos, Vector2 direction, bool hostile, float power, int pierce)
        {
            SpriteRenderer view = shotPool.Count > 0 ? shotPool.Pop() : NeonVisual.Make("Pulse", "square", Color.white, pos, Vector2.one, 15, actors);
            view.gameObject.SetActive(true); view.color = hostile ? NeonVisual.Violet : PilotColor;
            view.transform.localScale = hostile ? new Vector2(0.32f, 0.16f) : new Vector2(0.5f, 0.09f);
            view.transform.position = pos; view.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            shots.Add(new Projectile { view = view, pos = pos, velocity = direction * (hostile ? 6.7f : 22), hostile = hostile, damage = power, life = 3, pierce = pierce });
        }

        void UpdateShots(float dt)
        {
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                Projectile p = shots[i]; Vector2 previous = p.pos; p.pos += p.velocity * dt; p.life -= dt;
                if (p.hostile)
                {
                    if (ArenaRules.SegmentHits(previous, p.pos, playerPos, 0.38f))
                    {
                        Hurt(p.damage); p.life = 0;
                        if (mode != Mode.Playing) return;
                    }
                }
                else for (int j = enemies.Count - 1; j >= 0; j--)
                {
                    Enemy e = enemies[j];
                    if (p.hit.Contains(e) || !ArenaRules.SegmentHits(previous, p.pos, e.pos, e.radius + 0.09f)) continue;
                    p.hit.Add(e); e.hp -= p.damage; e.flash = 0.07f;
                    Burst(e.pos, e.view.color, 4, 2.5f);
                    if (e.hp <= 0) KillEnemy(j);
                    if (p.pierce-- <= 0) { p.life = 0; break; }
                }
                p.view.transform.position = p.pos;
                if (p.life <= 0 || Mathf.Abs(p.pos.x) > 13.2f || Mathf.Abs(p.pos.y) > 6.5f)
                {
                    p.view.gameObject.SetActive(false); shotPool.Push(p.view); shots.RemoveAt(i);
                }
            }
        }

        void KillEnemy(int index)
        {
            Enemy e = enemies[index]; kills++; score += (e.kind + 1) * 100 + wave * 10;
            DropShard(e.pos, 6 + e.kind * 4 + wave);
            Burst(e.pos, e.kind == 2 ? NeonVisual.Gold : e.kind == 1 ? NeonVisual.Violet : NeonVisual.Coral, 16, 4);
            if (kills % 7 == 0)
            {
                var view = NeonVisual.Body("Repair cell", "diamond", NeonVisual.Cyan, e.pos, 0.5f, actors);
                pickups.Add(new Pickup { pos = e.pos, view = view });
            }
            Destroy(e.view.gameObject); enemies.RemoveAt(index); shake = Mathf.Max(shake, 0.045f);
        }

        void Hurt(float amount)
        {
            if (hurtTimer > 0 || mode != Mode.Playing) return;
            health = Mathf.Max(0, health - amount); hurtTimer = 0.7f; shake = 0.18f;
            sound.Play("hit"); Burst(playerPos, NeonVisual.Coral, 14, 4);
            if (health <= 0) { mode = Mode.Defeat; Finish(false); sound.Play("lose"); }
        }

        void UpdatePickups(float dt)
        {
            for (int i = pickups.Count - 1; i >= 0; i--)
            {
                Pickup p = pickups[i]; p.age += dt;
                if ((p.pos - playerPos).sqrMagnitude < 4) p.pos = Vector2.MoveTowards(p.pos, playerPos, dt * 8);
                p.view.transform.position = p.pos; p.view.transform.Rotate(0, 0, dt * 90);
                if ((p.pos - playerPos).sqrMagnitude < 0.4f)
                {
                    health = Mathf.Min(maxHealth, health + 18); score += 25;
                    sound.Play("upgrade"); Burst(p.pos, NeonVisual.Cyan, 6, 2); Destroy(p.view.gameObject); pickups.RemoveAt(i);
                }
            }
        }

        void WaveComplete()
        {
            if (mode != Mode.Playing) return;
            foreach (Projectile p in shots) { p.view.gameObject.SetActive(false); shotPool.Push(p.view); } shots.Clear();
            BankRemainingShards(); AddCredits(25 * wave);
            if (wave >= ArenaRules.FinalWave) { mode = Mode.Victory; score += Mathf.RoundToInt(health) * 20; Finish(true); sound.Play("upgrade"); return; }
            health = Mathf.Min(maxHealth, health + 20);
            var choices = new List<int> { 0, 1, 2, 3, 4, 5 };
            if (spread >= 2) choices.Remove(3);
            for (int i = 0; i < 3; i++) { int n = UnityEngine.Random.Range(0, choices.Count); offered[i] = choices[n]; choices.RemoveAt(n); }
            ShowStory(); sound.Play("upgrade");
        }

        void ApplyUpgrade(int upgrade)
        {
            if (mode != Mode.Upgrade) return;
            switch (upgrade)
            {
                case 0: damage *= 1.3f; break;
                case 1: fireInterval *= 0.8f; break;
                case 2: maxHealth += 30; health = Mathf.Min(maxHealth, health + 60); break;
                case 3: spread++; break;
                case 4: moveSpeed *= 1.15f; break;
                case 5: piercing++; break;
            }
            notice = UpgradeName(upgrade); noticeTime = 3; NextWave();
        }

        void Finish(bool victory)
        {
            if (savedResult) return; savedResult = true;
            if (score > bestScore) { bestScore = score; SaveProgress(); }
            Debug.Log(victory ? "ASKICH_VICTORY" : "ASKICH_DEFEAT");
        }

        void ClearCombat()
        {
            foreach (Enemy e in enemies) Destroy(e.view.gameObject); enemies.Clear();
            foreach (Projectile p in shots) { p.view.gameObject.SetActive(false); shotPool.Push(p.view); } shots.Clear();
            foreach (Spark s in sparks) { s.view.gameObject.SetActive(false); sparkPool.Push(s.view); } sparks.Clear();
            foreach (Pickup p in pickups) Destroy(p.view.gameObject); pickups.Clear();
            foreach (Shard shard in shards) Destroy(shard.view.gameObject); shards.Clear();
        }

        void Burst(Vector2 pos, Color color, int count, float speed)
        {
            for (int i = 0; i < count && sparks.Count < 260; i++)
            {
                SpriteRenderer view = sparkPool.Count > 0 ? sparkPool.Pop() : NeonVisual.Make("Spark", "square", color, pos, Vector2.one * 0.07f, 17, effects);
                view.gameObject.SetActive(true); view.color = color; view.transform.position = pos;
                float life = UnityEngine.Random.Range(0.18f, 0.55f);
                sparks.Add(new Spark { pos = pos, view = view, color = color, life = life, total = life, velocity = UnityEngine.Random.insideUnitCircle * speed });
            }
        }

        void UpdateSparks(float dt)
        {
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                Spark s = sparks[i]; s.life -= dt; s.pos += s.velocity * dt; s.velocity *= Mathf.Exp(-4 * dt);
                s.view.transform.position = s.pos; s.view.color = new Color(s.color.r, s.color.g, s.color.b, Mathf.Max(0, s.life / s.total));
                if (s.life <= 0) { s.view.gameObject.SetActive(false); sparkPool.Push(s.view); sparks.RemoveAt(i); }
            }
        }

        static Vector2 Rotate(Vector2 v, float degrees) => (Vector2)(Quaternion.Euler(0, 0, degrees) * v);
        void OnApplicationFocus(bool focused) { if (!focused && mode == Mode.Playing) mode = Mode.Paused; }
        void OnDestroy() { if (gameCamera != null) Destroy(gameCamera.gameObject); }
        System.Collections.IEnumerator Capture()
        {
            yield return new WaitForEndOfFrame();
            string directory = System.IO.Path.Combine(Application.persistentDataPath, "Screenshots");
            System.IO.Directory.CreateDirectory(directory);
            string path = System.IO.Path.Combine(directory, "askich-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png");
            ScreenCapture.CaptureScreenshot(path); Debug.Log("ASKICH_SCREENSHOT " + path);
            notice = "Скриншот сохранён"; noticeTime = 3; captureRequested = false;
        }
    }
}
