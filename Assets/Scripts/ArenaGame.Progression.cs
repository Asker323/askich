using System.Collections.Generic;
using UnityEngine;

namespace Askich
{
    public sealed partial class ArenaGame
    {
        sealed class Shard { public SpriteRenderer view; public Vector2 pos; public int value; public float age; }
        readonly List<Shard> shards = new List<Shard>();
        AskichProfile profile;
        bool persistenceEnabled = true;
        int credits, runCredits, selectedSkin;
        bool[] skinUnlocked = { true, false, false };
        static readonly string[] SkinNames = { "Странник", "Янтарный страж", "Призрак архива" };
        static readonly int[] SkinPrices = { 0, 180, 520 };
        static readonly Color[] SkinColors = { NeonVisual.Cyan, NeonVisual.Gold, NeonVisual.Violet };
        bool rescuedArchive;
        public int LocationIndex => wave >= 3 ? 1 : 0;
        public string LocationName => LocationIndex == 0 ? "Холодный причал" : "Живая кузница";
        Color PilotColor => SkinColors[selectedSkin];
        static string SkinShape(int id) => id == 1 ? "ship_guard" : id == 2 ? "ship_ghost" : "ship";
        string StoryTitle, StorySpeaker, StoryText, StoryNextLabel;
        bool StoryHasChoice;
        string EndingText => rescuedArchive
            ? "СОВА: Архив свободен. Имена жителей снова звучат в эфире.\nАскич: Доставка завершена. Теперь они сами выберут, куда идти."
            : "СОВА: Маяк погас. Кузница больше не слышит приказов.\nАскич: Имена восстановим позже. Сначала выведем людей к свету.";

        void LoadProgress()
        {
            profile = AskichProfile.Load(); credits = profile.credits; bestScore = profile.bestScore;
            selectedSkin = profile.selectedSkin;
            for (int i = 0; i < skinUnlocked.Length; i++) skinUnlocked[i] = (profile.unlockedSkins & (1 << i)) != 0;
        }

        void SaveProgress()
        {
            if (!persistenceEnabled || profile == null) return;
            profile.credits = credits; profile.bestScore = bestScore; profile.selectedSkin = selectedSkin;
            profile.unlockedSkins = 0;
            for (int i = 0; i < skinUnlocked.Length; i++) if (skinUnlocked[i]) profile.unlockedSkins |= 1 << i;
            if (!profile.Save()) { notice = "Не удалось сохранить прогресс. Проверь свободное место."; noticeTime = 5; }
        }

        void AddCredits(int amount)
        {
            if (amount <= 0) return;
            credits = Mathf.Min(10000000, credits + amount); runCredits += amount;
            SaveProgress();
        }

        bool PurchaseOrEquipSkin(int id)
        {
            if (id < 0 || id >= skinUnlocked.Length) return false;
            if (!skinUnlocked[id])
            {
                if (credits < SkinPrices[id])
                {
                    notice = "Не хватает осколков: ещё " + (SkinPrices[id] - credits); noticeTime = 4; return false;
                }
                credits -= SkinPrices[id]; skinUnlocked[id] = true;
            }
            selectedSkin = id; ApplySkin(); SaveProgress(); sound.Play("upgrade");
            notice = "Выбран облик: " + SkinNames[id]; noticeTime = 3; return true;
        }

        void ApplySkin()
        {
            if (player == null) return;
            Color color = PilotColor;
            player.color = color;
            player.sprite = NeonVisual.Get(SkinShape(selectedSkin));
            Transform inset = player.transform.Find("Inset");
            if (inset != null) inset.GetComponent<SpriteRenderer>().sprite = player.sprite;
            Transform core = player.transform.Find("Core");
            if (core != null) { core.GetComponent<SpriteRenderer>().sprite = player.sprite; core.GetComponent<SpriteRenderer>().color = color; }
            Transform glow = player.transform.Find("Glow");
            if (glow != null) glow.GetComponent<SpriteRenderer>().color = new Color(color.r, color.g, color.b, 0.65f);
        }

        void OpenShop() { if (mode == Mode.Menu) mode = Mode.Shop; }
        void CloseShop() { if (mode == Mode.Shop) mode = Mode.Menu; }

        void DropShard(Vector2 pos, int amount)
        {
            SpriteRenderer view = NeonVisual.Body("Memory shard", "diamond", NeonVisual.Gold, pos, 0.32f, actors);
            shards.Add(new Shard { pos = pos, value = amount, view = view });
        }

        void UpdateShards(float dt)
        {
            for (int i = shards.Count - 1; i >= 0; i--)
            {
                Shard shard = shards[i]; shard.age += dt;
                float distance = (shard.pos - playerPos).sqrMagnitude;
                if (distance < 6.25f) shard.pos = Vector2.MoveTowards(shard.pos, playerPos, dt * 9);
                shard.view.transform.position = shard.pos;
                shard.view.transform.Rotate(0, 0, dt * 50);
                if ((shard.pos - playerPos).sqrMagnitude < 0.38f)
                {
                    AddCredits(shard.value); Burst(shard.pos, NeonVisual.Gold, 4, 1.4f);
                    Destroy(shard.view.gameObject); shards.RemoveAt(i);
                }
            }
        }

        void BankRemainingShards()
        {
            int total = 0;
            foreach (Shard shard in shards) { total += shard.value; Destroy(shard.view.gameObject); }
            shards.Clear(); AddCredits(total);
        }

        void ShowStory()
        {
            mode = Mode.Story; StoryHasChoice = wave == 2;
            StorySpeaker = wave == 0 ? "СОВА  /  аварийный канал" : wave == 2 ? "АСКИЧ + СОВА  /  точка решения" : "СОВА  /  восстановленная запись";
            StoryNextLabel = wave == 0 ? "ВОЙТИ НА ПРИЧАЛ" : "ПОДГОТОВИТЬ СНАРЯЖЕНИЕ";
            switch (wave)
            {
                case 0:
                    StoryTitle = "Посылка без адресата";
                    StoryText = "Аскич, ты единственный курьер, дошедший до станции «Нерида».\nВ твоём контейнере — последнее человеческое воспоминание.\n\nТридцать лет назад станция исчезла из эфира. Сегодня она прислала\nзаказ от твоего имени. Пройди холодный причал и найди источник.\n\nОхранные дроны считают память опасным грузом. Не отдавай её.";
                    break;
                case 1:
                    StoryTitle = "Чужой голос в эфире";
                    StoryText = "СОВА: В осколках дронов сохранились имена. Они не охраняют\nстанцию — они охраняют её забвение.\n\nАСКИЧ: Почему заказ подписан моим именем?\n\nСОВА: Потому что ты уже бывал здесь. Собирай осколки памяти.\nЯ открою нижний шлюз, когда ты очистишь причал.";
                    break;
                case 2:
                    StoryTitle = "Что мы унесём с собой?";
                    StoryText = "За шлюзом — Живая кузница. Её корни питаются архивом жителей.\nСОВА: Я была их хранителем. Чтобы пережить холод, я отдала\nКузнице наши имена. Теперь она не хочет нас отпускать.\n\nМожно сохранить архив и укрепить защиту контейнера.\nИли заглушить маяк, направив его энергию в оружие.\n\nАскич, этот выбор уже не записан заранее.";
                    break;
                case 3:
                    StoryTitle = "Город под металлом";
                    StoryText = rescuedArchive
                        ? "Из контейнера доносится шум улицы. Архив ещё жив.\n\nСОВА: В каждом осколке — чей-то путь домой. Ты сохранил их,\nхотя я просила только вернуть станцию под контроль.\n\nАСКИЧ: Курьер доставляет людей к будущему. Не приказы — к прошлому."
                        : "Маяк молчит. Впервые за тридцать лет дроны слышат только себя.\n\nСОВА: Мы потеряли часть имён, но остановили передачу приказов.\n\nАСКИЧ: Если память стала клеткой, сначала нужно открыть дверь.\nЯ вернусь за теми, кто ещё внутри.";
                    break;
                default:
                    StoryTitle = "Последняя доставка";
                    StoryText = "СОВА: Центральный узел собирает последний заслон.\nКогда он падёт, я смогу вывести выживших из Кузницы.\n\nАСКИЧ: А ты?\n\nСОВА: Впервые выберу свой адрес сама.\n\nЗакончим доставку. Пятая волна — последняя.";
                    break;
            }
            Debug.Log("ASKICH_STORY_" + wave);
        }

        void ContinueStory(int choice = 0)
        {
            if (mode != Mode.Story) return;
            if (wave == 0) { NextWave(); return; }
            if (wave == 2)
            {
                rescuedArchive = choice == 0;
                if (rescuedArchive) { maxHealth += 15; health = Mathf.Min(maxHealth, health + 15); }
                else damage *= 1.1f;
            }
            mode = Mode.Upgrade;
            Debug.Log("ASKICH_UPGRADE_READY");
        }
    }
}
