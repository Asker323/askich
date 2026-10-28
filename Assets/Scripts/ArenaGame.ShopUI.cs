using UnityEngine;

namespace Askich
{
    public sealed partial class ArenaGame
    {
        void StoryScreen()
        {
            Box(new Rect(0, 0, 1280, 720), new Color(0.012f, 0.024f, 0.04f, 0.94f));
            Color accent = LocationIndex == 0 ? NeonVisual.Cyan : NeonVisual.Gold;
            Box(new Rect(70, 58, 4, 24), accent);
            Text(new Rect(90, 59, 620, 25), "ASKICH  /  КАНАЛ СВЯЗИ", 13, accent, TextAnchor.MiddleLeft, true);
            Text(new Rect(800, 60, 408, 23), wave == 0 ? "ПЕРЕД ВЫЛЕТОМ" : "ПОСЛЕ ВОЛНЫ " + wave.ToString("00"), 11, MutedInk, TextAnchor.MiddleRight);
            Box(new Rect(71, 111, 1137, 1), new Color(0.13f, 0.23f, 0.27f));

            Box(new Rect(72, 161, 216, 354), Panel);
            Box(new Rect(72, 161, 216, 2), accent);
            DrawShape(new Rect(87, 178, 186, 186), "glow", new Color(accent.r, accent.g, accent.b, 0.65f));
            DrawShape(new Rect(120, 212, 120, 120), "ring", accent);
            DrawShape(new Rect(147, 238, 66, 66), "diamond", accent);
            DrawShape(new Rect(161, 253, 38, 38), "diamond", Panel);
            Text(new Rect(95, 367, 170, 26), "СВЯЗЬ УСТАНОВЛЕНА", 10, accent, TextAnchor.MiddleCenter, true);
            for (int i = 0; i < 17; i++)
            {
                float bar = 10 + Mathf.Abs(Mathf.Sin(i * 1.75f + Time.unscaledTime * 1.5f)) * 24;
                Box(new Rect(113 + i * 8, 421 - bar * 0.5f, 3, bar), new Color(accent.r, accent.g, accent.b, 0.65f));
            }
            Text(new Rect(95, 467, 170, 26), "ЗАПИСЬ " + wave.ToString("00"), 11, MutedInk, TextAnchor.MiddleCenter);

            Text(new Rect(333, 154, 860, 72), StoryTitle, 38, Ink, TextAnchor.UpperLeft, true);
            Text(new Rect(335, 236, 850, 25), (StorySpeaker ?? "СВЯЗЬ").ToUpperInvariant(), 12, accent, TextAnchor.MiddleLeft, true);
            Text(new Rect(334, 277, 846, 179), StoryText, 18, Ink);
            if (StoryHasChoice)
            {
                Text(new Rect(335, 463, 850, 22), "ВЫБЕРИ, ЧТО СОХРАНИТЬ. РЕШЕНИЕ ПОВЛИЯЕТ НА ЭПИЛОГ.", 11, MutedInk);
                if (StoryChoice(new Rect(334, 494, 412, 122), "1", "Спасти архив", "+15 к максимальной прочности", NeonVisual.Cyan)) ContinueStory(0);
                else if (StoryChoice(new Rect(769, 494, 412, 122), "2", "Заглушить маяк", "+10% к урону импульса", NeonVisual.Gold)) ContinueStory(1);
            }
            else
            {
                if (Button(new Rect(334, 520, 420, 57), StoryNextLabel + "    →", true)) ContinueStory();
                Text(new Rect(335, 598, 844, 22), "ENTER — ПРОДОЛЖИТЬ", 11, MutedInk);
            }
            Box(new Rect(72, 662, 1136, 1), new Color(0.13f, 0.23f, 0.27f));
            Text(new Rect(72, 681, 600, 20), "ТВОЙ ПУТЬ  /  " + LocationName.ToUpperInvariant(), 11, MutedInk);
            Text(new Rect(950, 681, 258, 20), "СИГНАЛ ЕЩЁ ЖИВ", 11, accent, TextAnchor.UpperRight);
        }

        bool StoryChoice(Rect card, string key, string title, string benefit, Color accent)
        {
            bool hover = card.Contains(Event.current.mousePosition);
            Box(card, hover ? new Color(0.07f, 0.15f, 0.18f) : Panel);
            Box(new Rect(card.x, card.y, card.width, 2), accent);
            Key(new Rect(card.x + 20, card.y + 22, 29, 29), key);
            Text(new Rect(card.x + 64, card.y + 21, card.width - 84, 37), title, 22, Ink, TextAnchor.UpperLeft, true);
            Text(new Rect(card.x + 22, card.y + 76, card.width - 44, 26), benefit, 14, accent);
            return GUI.Button(card, GUIContent.none, GUIStyle.none);
        }

        void ShopScreen()
        {
            Box(new Rect(0, 0, 1280, 720), new Color(0.012f, 0.024f, 0.04f, 0.96f));
            Text(new Rect(70, 47, 600, 30), "ASKICH / АНГАР", 14, NeonVisual.Cyan, TextAnchor.UpperLeft, true);
            Text(new Rect(67, 87, 800, 65), "Корабль с твоим характером", 38, Ink, TextAnchor.UpperLeft, true);
            DrawShape(new Rect(963, 56, 24, 24), "diamond", NeonVisual.Gold);
            Text(new Rect(1003, 51, 208, 38), credits.ToString("N0"), 30, NeonVisual.Gold, TextAnchor.MiddleRight, true);
            Text(new Rect(958, 97, 253, 21), "КРЕДИТЫ ИЗ ТВОИХ ВЫЛЕТОВ", 10, MutedInk, TextAnchor.UpperRight);

            for (int id = 0; id < 3; id++)
            {
                float x = 70 + id * 389;
                Rect card = new Rect(x, 178, 363, 422);
                Color accent = SkinColors[id];
                bool unlocked = IsSkinUnlocked(id);
                bool selected = unlocked && selectedSkin == id;
                bool affordable = unlocked || credits >= SkinPrices[id];
                Box(card, selected ? new Color(0.045f, 0.105f, 0.13f) : Panel);
                Box(new Rect(x, 178, 363, selected ? 4 : 2), accent);
                Text(new Rect(x + 22, 198, 319, 23), selected ? "УСТАНОВЛЕН НА КОРАБЛЬ" : unlocked ? "В ТВОЕЙ КОЛЛЕКЦИИ" : "ДОСТУПЕН ЗА КРЕДИТЫ", 10, selected ? accent : MutedInk, TextAnchor.MiddleCenter, true);
                SkinPortrait(new Rect(x + 87, 229, 190, 170), id);
                Text(new Rect(x + 20, 411, 323, 39), SkinNames[id], 25, Ink, TextAnchor.MiddleCenter, true);
                string description = id == 0 ? "Холодный свет первого вылета.\nДля тех, кто только начинает путь." : id == 1 ? "Тёплый отблеск живой кузницы.\nОгонь остаётся с тобой." : "Фиолетовый след забытого сигнала.\nАрхив помнит своего пилота.";
                Text(new Rect(x + 26, 460, 311, 52), description, 14, MutedInk, TextAnchor.MiddleCenter);
                Rect action = new Rect(x + 24, 534, 315, 44);
                if (selected)
                {
                    Box(action, new Color(accent.r * 0.14f, accent.g * 0.14f, accent.b * 0.14f));
                    Text(action, "ОБЛИК ВЫБРАН", 12, accent, TextAnchor.MiddleCenter, true);
                }
                else
                {
                    string title = unlocked ? "ВЫБРАТЬ ОБЛИК" : "КУПИТЬ  /  " + SkinPrices[id] + " КР.";
                    if (Button(action, title, affordable)) { PurchaseOrEquipSkin(id); break; }
                }
            }
            Box(new Rect(70, 672, 1140, 1), new Color(0.13f, 0.23f, 0.27f));
            if (Button(new Rect(70, 623, 215, 37), "←  В ГЛАВНОЕ МЕНЮ", false)) CloseShop();
            Text(new Rect(73, 689, 870, 19), "Облики меняют только внешний вид. Кредиты зарабатываются в бою.", 11, MutedInk);
            Text(new Rect(1040, 689, 170, 19), "ESC — НАЗАД", 10, MutedInk, TextAnchor.UpperRight);
        }

        int UnlockedSkinCount()
        {
            int count = 0;
            for (int id = 0; id < 3; id++) if (IsSkinUnlocked(id)) count++;
            return count;
        }

        bool IsSkinUnlocked(int id) => skinUnlocked != null && id < skinUnlocked.Length && skinUnlocked[id];

        void SkinPortrait(Rect bounds, int id)
        {
            Color accent = SkinColors[Mathf.Clamp(id, 0, 2)];
            string shape = SkinShape(id);
            float size = Mathf.Min(bounds.width, bounds.height);
            Vector2 center = bounds.center;
            DrawShape(new Rect(center.x - size * 0.55f, center.y - size * 0.55f, size * 1.1f, size * 1.1f), "glow", accent);
            DrawShape(new Rect(center.x - size * 0.41f, center.y - size * 0.41f, size * 0.82f, size * 0.82f), "ring", new Color(accent.r, accent.g, accent.b, 0.3f));
            Matrix4x4 old = GUI.matrix;
            GUIUtility.RotateAroundPivot(-90, center);
            DrawShape(new Rect(center.x - size * 0.29f, center.y - size * 0.29f, size * 0.58f, size * 0.58f), shape, accent);
            DrawShape(new Rect(center.x - size * 0.21f, center.y - size * 0.21f, size * 0.42f, size * 0.42f), shape, new Color(0.035f, 0.075f, 0.12f));
            DrawShape(new Rect(center.x - size * 0.1f, center.y - size * 0.1f, size * 0.2f, size * 0.2f), shape, accent);
            GUI.matrix = old;
        }

        void DrawShape(Rect rect, string shape, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, NeonVisual.Get(shape).texture, ScaleMode.ScaleToFit, true);
            GUI.color = old;
        }
    }
}
