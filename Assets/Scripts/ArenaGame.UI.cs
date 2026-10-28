using UnityEngine;
using UnityEngine.InputSystem;

namespace Askich
{
    public sealed partial class ArenaGame
    {
        const float UiWidth = 1280, UiHeight = 720;
        static readonly Color Ink = new Color(0.9f, 0.95f, 0.96f);
        static readonly Color MutedInk = new Color(0.44f, 0.58f, 0.64f);
        static readonly Color Panel = new Color(0.028f, 0.052f, 0.078f, 0.98f);
        Font font;
        GUIStyle label;

        void OnGUI()
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (label == null) label = new GUIStyle(GUI.skin.label) { font = font, richText = false, wordWrap = true, padding = new RectOffset(0, 0, 0, 0) };
            float scale = Mathf.Min(Screen.width / UiWidth, Screen.height / UiHeight);
            Vector2 offset = new Vector2((Screen.width - UiWidth * scale) / 2, (Screen.height - UiHeight * scale) / 2);
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one * scale);
            if (mode == Mode.Menu) Menu();
            else if (mode == Mode.Story) StoryScreen();
            else if (mode == Mode.Shop) ShopScreen();
            else Hud();
            if (mode == Mode.Playing)
            {
                if (waveBanner > 0)
                {
                    Box(new Rect(460, 115, 360, 65), new Color(0.025f, 0.05f, 0.08f, 0.9f));
                    Text(new Rect(460, 122, 360, 25), "ВОЛНА " + wave.ToString("00") + " / 05", 19, LocationIndex == 0 ? NeonVisual.Cyan : NeonVisual.Gold, TextAnchor.MiddleCenter, true);
                    Text(new Rect(460, 150, 360, 22), wave == 1 ? "Удерживай ЛКМ, чтобы стрелять" : wave == 2 ? "Снайперы на периметре" : wave == 3 ? "Тяжёлые дроны готовят рывок" : "Не останавливайся", 13, MutedInk, TextAnchor.MiddleCenter);
                }
                Crosshair(scale, offset);
            }
            if (mode == Mode.Upgrade) UpgradeScreen();
            if (mode == Mode.Paused) PauseScreen();
            if (mode == Mode.Defeat || mode == Mode.Victory) ResultScreen();
            if (noticeTime > 0 && mode == Mode.Playing)
                Text(new Rect(430, 585, 420, 28), notice, 16, NeonVisual.Cyan, TextAnchor.MiddleCenter);
            if (noticeTime > 0 && mode == Mode.Shop)
            {
                Box(new Rect(330, 626, 620, 34), new Color(0.06f, 0.12f, 0.15f));
                Text(new Rect(344, 628, 592, 30), notice, 14, Ink, TextAnchor.MiddleCenter);
            }
            GUI.matrix = old;
        }

        void Hud()
        {
            Box(new Rect(0, 0, 1280, 77), new Color(0.018f, 0.032f, 0.055f, 0.98f));
            Box(new Rect(0, 76, 1280, 1), new Color(0.14f, 0.25f, 0.29f));
            Text(new Rect(28, 12, 190, 30), "ASKICH", 27, Ink, TextAnchor.MiddleLeft, true);
            Text(new Rect(30, 46, 190, 18), LocationName.ToUpperInvariant(), 10, LocationIndex == 0 ? NeonVisual.Cyan : NeonVisual.Gold);
            Text(new Rect(242, 13, 132, 17), "ПРОЧНОСТЬ", 10, MutedInk);
            Text(new Rect(376, 12, 74, 18), Mathf.CeilToInt(health) + " / " + maxHealth, 11, Ink, TextAnchor.UpperRight);
            Box(new Rect(242, 39, 208, 8), new Color(0.1f, 0.17f, 0.21f));
            Box(new Rect(242, 39, 208 * Mathf.Clamp01(health / maxHealth), 8), health < 30 ? NeonVisual.Coral : NeonVisual.Cyan);
            Text(new Rect(242, 52, 208, 14), hurtTimer > 0 ? "ЩИТ АКТИВЕН" : "РЕМОНТНЫЕ ЯЧЕЙКИ ЛЕЧАТ", 9, MutedInk);
            Text(new Rect(490, 12, 112, 18), "ВОЛНА", 10, MutedInk);
            Text(new Rect(490, 33, 112, 32), wave.ToString("00") + " / 05", 24, Ink, TextAnchor.UpperLeft, true);
            Text(new Rect(633, 12, 118, 18), "ЦЕЛЕЙ", 10, MutedInk);
            Text(new Rect(633, 34, 118, 28), (remaining + enemies.Count).ToString("00"), 23, NeonVisual.Coral, TextAnchor.UpperLeft, true);
            Text(new Rect(776, 12, 132, 18), "СЧЁТ", 10, MutedInk);
            Text(new Rect(776, 34, 132, 28), score.ToString("N0"), 23, Ink, TextAnchor.UpperLeft, true);
            Text(new Rect(937, 12, 150, 18), "КРЕДИТЫ  /  +" + runCredits, 10, MutedInk);
            Text(new Rect(937, 34, 150, 28), credits.ToString("N0"), 23, NeonVisual.Gold, TextAnchor.UpperLeft, true);
            Text(new Rect(1150, 18, 100, 22), TimeText(elapsed), 19, MutedInk, TextAnchor.MiddleRight);
            Text(new Rect(1130, 47, 120, 17), sound.Muted ? "M  ЗВУК ВЫКЛ." : "M  ЗВУК ВКЛ.", 9, MutedInk, TextAnchor.MiddleRight);
            Box(new Rect(0, 651, 1280, 69), new Color(0.018f, 0.032f, 0.055f, 0.98f));
            Box(new Rect(0, 651, 1280, 1), new Color(0.14f, 0.25f, 0.29f));
            Key(new Rect(30, 671, 54, 27), "WASD"); Text(new Rect(94, 675, 115, 23), "движение", 12, MutedInk);
            Key(new Rect(215, 671, 43, 27), "ЛКМ"); Text(new Rect(268, 675, 110, 23), "стрельба", 12, MutedInk);
            Key(new Rect(382, 671, 67, 27), "ПРОБЕЛ");
            Text(new Rect(459, 672, 150, 26), dashCooldown > 0 ? "рывок  " + dashCooldown.ToString("0.0") + " с" : "рывок готов", 12, dashCooldown > 0 ? MutedInk : NeonVisual.Cyan);
            Box(new Rect(459, 698, 100, 3), new Color(0.1f, 0.17f, 0.21f));
            Box(new Rect(459, 698, 100 * (1 - dashCooldown / 1.8f), 3), NeonVisual.Cyan);
            Text(new Rect(668, 675, 380, 24), "ИМПУЛЬС " + Mathf.RoundToInt(damage) + "  /  " + (1 / fireInterval).ToString("0.0") + "/С  /  СТВОЛОВ " + (1 + 2 * spread), 11, MutedInk);
            Key(new Rect(1118, 671, 43, 27), "ESC"); Text(new Rect(1171, 675, 83, 23), "пауза", 12, MutedInk);
        }

        void Menu()
        {
            Box(new Rect(0, 0, 1280, 720), new Color(0.012f, 0.024f, 0.038f, 0.88f));
            Box(new Rect(72, 58, 4, 24), NeonVisual.Cyan);
            Text(new Rect(90, 60, 570, 24), "ASKER323  /  ИСТОРИЯ О ПОСЛЕДНЕМ СИГНАЛЕ", 12, NeonVisual.Cyan);
            Text(new Rect(66, 133, 710, 112), "ASKICH", 94, Ink, TextAnchor.UpperLeft, true);
            Text(new Rect(73, 253, 650, 45), "Там, где молчит маяк", 29, NeonVisual.Cyan, TextAnchor.UpperLeft, true);
            Text(new Rect(74, 316, 578, 82), "Пройди через холодный причал и живую кузницу.\nСражайся, меняй корабль и реши судьбу архива.", 20, MutedInk);
            if (Button(new Rect(74, 425, 355, 59), "НАЧАТЬ ИСТОРИЮ    →", true)) StartRun();
            if (Button(new Rect(74, 499, 355, 49), "АНГАР  /  ОБЛИКИ КОРАБЛЯ", false)) OpenShop();
            Text(new Rect(448, 506, 272, 25), credits.ToString("N0") + " КРЕДИТОВ", 16, NeonVisual.Gold, TextAnchor.MiddleLeft, true);
            Text(new Rect(448, 532, 272, 19), "ОТКРЫТО ОБЛИКОВ: " + UnlockedSkinCount() + " / 3", 10, MutedInk);
            Text(new Rect(75, 566, 580, 23), "ENTER — начать    /    WASD — движение    /    ЛКМ — огонь", 12, MutedInk);
            if (Button(new Rect(74, 605, 168, 35), sound.Muted ? "ЗВУК ВЫКЛ." : "ЗВУК ВКЛ.", false)) sound.Toggle();
            if (Button(new Rect(258, 605, 171, 35), "ВЫЙТИ", false)) Quit();

            Box(new Rect(801, 131, 1, 460), new Color(0.13f, 0.23f, 0.27f));
            Text(new Rect(840, 136, 355, 20), "ТВОЙ КОРАБЛЬ", 11, MutedInk, TextAnchor.MiddleCenter);
            SkinPortrait(new Rect(886, 169, 262, 250), selectedSkin);
            Text(new Rect(831, 425, 374, 31), SkinNames[Mathf.Clamp(selectedSkin, 0, 2)], 21, Ink, TextAnchor.MiddleCenter, true);
            Text(new Rect(852, 477, 334, 55), "5 ВОЛН  /  2 ЛОКАЦИИ\nОДИН ВЫБОР, КОТОРЫЙ ОСТАНЕТСЯ", 12, MutedInk, TextAnchor.MiddleCenter);
            Text(new Rect(847, 559, 344, 25), "ПРОБЕЛ — рывок сквозь опасность", 12, NeonVisual.Cyan, TextAnchor.MiddleCenter);
            Box(new Rect(72, 662, 1136, 1), new Color(0.13f, 0.23f, 0.27f));
            Text(new Rect(72, 681, 600, 20), "ПРОТОКОЛ ASKICH     /     РЕКОРД  " + bestScore.ToString("N0"), 11, MutedInk);
            Text(new Rect(950, 681, 258, 20), "UNITY 6  •  v" + Application.version, 11, MutedInk, TextAnchor.UpperRight);
        }

        void UpgradeScreen()
        {
            Overlay();
            Text(new Rect(150, 123, 980, 27), "ПЕРИМЕТР ОЧИЩЕН  /  ВОЛНА " + wave.ToString("00"), 13, NeonVisual.Cyan, TextAnchor.MiddleCenter, true);
            Text(new Rect(150, 161, 980, 61), "Выбери преимущество", 39, Ink, TextAnchor.MiddleCenter, true);
            Text(new Rect(150, 231, 980, 26), "+20 прочности. Один модуль для следующего участка пути.", 15, MutedInk, TextAnchor.MiddleCenter);
            for (int i = 0; i < 3; i++)
            {
                float x = 155 + i * 330;
                Rect card = new Rect(x, 298, 310, 252);
                bool hover = card.Contains(Event.current.mousePosition);
                Box(card, hover ? new Color(0.06f, 0.15f, 0.19f) : Panel);
                Box(new Rect(x, 298, 310, 3), hover ? NeonVisual.Cyan : new Color(0.16f, 0.32f, 0.37f));
                Key(new Rect(x + 24, 321, 34, 29), (i + 1).ToString());
                Text(new Rect(x + 24, 368, 263, 40), UpgradeName(offered[i]), 23, Ink, TextAnchor.UpperLeft, true);
                Text(new Rect(x + 24, 419, 263, 65), UpgradeDescription(offered[i]), 15, MutedInk);
                Text(new Rect(x + 24, 505, 263, 26), "УСТАНОВИТЬ    →", 12, NeonVisual.Cyan, TextAnchor.UpperLeft, true);
                if (GUI.Button(card, GUIContent.none, GUIStyle.none)) { ApplyUpgrade(offered[i]); break; }
            }
            Text(new Rect(150, 586, 980, 27), "Нажми на карточку или клавишу 1 / 2 / 3", 13, MutedInk, TextAnchor.MiddleCenter);
        }

        void PauseScreen()
        {
            Overlay(); Box(new Rect(409, 194, 462, 350), Panel);
            Text(new Rect(435, 226, 410, 22), "СИГНАЛ ПРИОСТАНОВЛЕН", 12, NeonVisual.Cyan, TextAnchor.MiddleCenter);
            Text(new Rect(435, 269, 410, 57), "Пауза", 44, Ink, TextAnchor.MiddleCenter, true);
            if (Button(new Rect(455, 356, 370, 53), "ПРОДОЛЖИТЬ", true)) mode = Mode.Playing;
            if (Button(new Rect(455, 426, 175, 43), sound.Muted ? "ЗВУК ВЫКЛ." : "ЗВУК ВКЛ.", false)) sound.Toggle();
            if (Button(new Rect(646, 426, 179, 43), "В МЕНЮ", false)) { ClearCombat(); mode = Mode.Menu; playerPos = Vector2.zero; }
            Text(new Rect(435, 492, 410, 23), "ESC или P — продолжить", 12, MutedInk, TextAnchor.MiddleCenter);
        }

        void ResultScreen()
        {
            Overlay(); bool win = mode == Mode.Victory;
            Color accent = win ? NeonVisual.Cyan : NeonVisual.Coral;
            Box(new Rect(270, 108, 740, 510), Panel);
            Box(new Rect(270, 108, 740, 3), accent);
            Text(new Rect(300, 135, 680, 24), win ? "ПОСЛЕДНИЙ СИГНАЛ ПРИНЯТ" : "СИГНАЛ ПОТЕРЯН", 13, accent, TextAnchor.MiddleCenter, true);
            Text(new Rect(292, 174, 696, 56), win ? "Твоя история осталась" : "Путь ещё не закончен", 35, Ink, TextAnchor.MiddleCenter, true);
            Text(new Rect(318, 245, 644, 81), win ? EndingText : "Причал помнит каждый вылет. Кредиты, заработанные в бою, помогут выбрать новый облик в ангаре. Попробуй другой маршрут и береги рывок.", 16, MutedInk, TextAnchor.MiddleCenter);
            ResultFact(315, "СЧЁТ", score.ToString("N0"), accent);
            ResultFact(483, "ДРОНОВ", kills.ToString(), Ink);
            ResultFact(651, "ВРЕМЯ", TimeText(elapsed), Ink);
            ResultFact(819, "ЗА ВЫЛЕТ", "+" + runCredits.ToString("N0"), NeonVisual.Gold);
            if (Button(new Rect(328, 447, 624, 53), "НОВАЯ ИСТОРИЯ    →", true)) StartRun();
            if (Button(new Rect(328, 517, 302, 41), "В АНГАР", false)) { ClearCombat(); mode = Mode.Menu; playerPos = Vector2.zero; OpenShop(); }
            if (Button(new Rect(650, 517, 302, 41), "В ГЛАВНОЕ МЕНЮ", false)) { ClearCombat(); mode = Mode.Menu; playerPos = Vector2.zero; }
            Text(new Rect(320, 579, 640, 20), "R ИЛИ ENTER — НОВЫЙ ВЫЛЕТ", 10, MutedInk, TextAnchor.MiddleCenter);
        }

        void ResultFact(float x, string caption, string value, Color color)
        {
            Text(new Rect(x, 348, 146, 20), caption, 10, MutedInk, TextAnchor.MiddleCenter);
            Text(new Rect(x, 377, 146, 39), value, 27, color, TextAnchor.MiddleCenter, true);
        }

        void Crosshair(float scale, Vector2 offset)
        {
            if (Mouse.current == null) return;
            Vector2 p = Mouse.current.position.ReadValue(); p.y = Screen.height - p.y; p = (p - offset) / scale;
            Color c = new Color(0.6f, 1, 0.94f, 0.8f);
            Box(new Rect(p.x - 11, p.y, 6, 1), c); Box(new Rect(p.x + 5, p.y, 6, 1), c);
            Box(new Rect(p.x, p.y - 11, 1, 6), c); Box(new Rect(p.x, p.y + 5, 1, 6), c);
        }

        static string UpgradeName(int id)
        {
            switch (id) { case 0: return "Перегрузка"; case 1: return "Быстрый цикл"; case 2: return "Новая броня"; case 3: return "Мультивыстрел"; case 4: return "Ускоритель"; default: return "Сквозной импульс"; }
        }
        static string UpgradeDescription(int id)
        {
            switch (id)
            {
                case 0: return "+30% урона\nКаждый импульс бьёт сильнее.";
                case 1: return "+25% скорострельности\nМеньше пауз между выстрелами.";
                case 2: return "+30 к максимальной прочности\nИ восстановление 60 прочности.";
                case 3: return "+2 боковых импульса\nБоковые наносят 72% урона.";
                case 4: return "+15% скорости\nДержи дистанцию от дронов.";
                default: return "+1 пробиваемая цель\nПробивай плотные группы.";
            }
        }
        static string TimeText(float seconds) => ((int)seconds / 60).ToString("00") + ":" + ((int)seconds % 60).ToString("00");
        void Overlay() => Box(new Rect(0, 77, 1280, 574), new Color(0.012f, 0.022f, 0.039f, 0.92f));
        void Key(Rect rect, string value) { Box(rect, new Color(0.08f, 0.14f, 0.18f)); Text(rect, value, 10, Ink, TextAnchor.MiddleCenter, true); }
        void Box(Rect rect, Color color) { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old; }
        void Text(Rect rect, string value, int size, Color color, TextAnchor anchor = TextAnchor.UpperLeft, bool bold = false)
        {
            label.fontSize = size; label.normal.textColor = color; label.alignment = anchor; label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            GUI.Label(rect, value, label);
        }
        bool Button(Rect rect, string title, bool primary)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            Box(rect, primary ? (hover ? new Color(0.55f, 1, 0.93f) : NeonVisual.Cyan) : hover ? new Color(0.1f, 0.2f, 0.24f) : new Color(0.065f, 0.11f, 0.15f));
            Text(rect, title, primary ? 15 : 11, primary ? new Color(0.02f, 0.09f, 0.1f) : Ink, TextAnchor.MiddleCenter, true);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }
        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
