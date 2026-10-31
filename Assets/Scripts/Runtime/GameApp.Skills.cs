using Squares.Simulation;
using UnityEngine;

namespace Squares
{
    public sealed partial class GameApp
    {
        private Color SkillColor(Module id)
        {
            if (id == Module.Blast) return SquareView.Gold;
            if (id == Module.Frost || id == Module.Arc || id == Module.Chain) return SquareView.Blue;
            string category = Upgrades.All[(int)id].Category;
            return category == "SURVIVAL" ? SquareView.Mint : category == "WEAPON" ? SquareView.Gold : SquareView.Blue;
        }

        private void SkillDraft()
        {
            Shade();
            if (world.IsSpecializationDraft) { SpecializationCards(); return; }
            float x = (uiWidth - 1360) / 2, y = (uiHeight - 630) / 2 + 25;
            TextAt(new Rect(x, y - 78, 1360, 45), "КАК УСИЛИМ СБОРКУ?", 34, Text, true);
            TextAt(new Rect(x, y - 24, 1360, 28), "УРОВЕНЬ " + world.Level + "  /  1–3: ВЫБОР  /  TAB: СБОРКА  /  ВРЕМЯ ОСТАНОВЛЕНО", 16, SquareView.Mint);
            for (int i = 0; i < 3; i++)
            {
                Module id = world.Choices[i]; Upgrade u = Upgrades.All[(int)id];
                float cx = x + i * 462; Color accent = SkillColor(id);
                PanelAt(new Rect(cx, y + 22, 436, 518), accent);
                SkillIcon(new Rect(cx + 24, y + 44, 58, 58), id, accent);
                TextAt(new Rect(cx + 101, y + 49, 311, 24), "0" + (i + 1) + " / " + SkillText.Category(u.Category), 16, accent);
                string rank = id == Module.Repair ? "ОДНОРАЗОВОЕ ДЕЙСТВИЕ" : world.Rank(id) == 0 ? "НОВЫЙ НАВЫК" : "РАНГ " + world.Rank(id) + " → " + (world.Rank(id) + 1);
                TextAt(new Rect(cx + 101, y + 80, 311, 24), rank, 15, Muted);
                TextAt(new Rect(cx + 24, y + 124, 388, 42), u.Name, 28, Text, true);
                TextAt(new Rect(cx + 24, y + 180, 388, 82), u.Description, 20, Text, wrap: true);
                Fill(new Rect(cx + 24, y + 272, 388, 1), LineColor);
                TextAt(new Rect(cx + 24, y + 290, 388, 95), SkillText.Numbers(world, id), 21, accent, true, wrap: true);
                TextAt(new Rect(cx + 24, y + 393, 388, 68), SkillText.Tip(id), 17, Muted, wrap: true);
                if (ButtonAt(new Rect(cx + 24, y + 474, 388, 44), "ВЫБРАТЬ [ " + (i + 1) + " ]", accent)) { Pick(i); break; }
            }
            if (ButtonAt(new Rect(x, y + 560, 415, 42), world.RerollsRemaining > 0 ? "R / ОБНОВИТЬ ВЫБОР: 1 РАЗ" : "ОБНОВЛЕНИЕ ИСПОЛЬЗОВАНО", Muted, world.RerollsRemaining > 0)) world.Reroll();
            TextAt(new Rect(x + 450, y + 564, 910, 48), "Числа — для твоей сборки, без временной перегрузки.\nРецепты сочетаний и все купленные навыки — на TAB.", 16, Muted, wrap: true);
        }

        private void SpecializationCards()
        {
            float x = (uiWidth - 1360) / 2, y = (uiHeight - 570) / 2;
            TextAt(new Rect(x, y - 75, 1360, 44), "ВЫБЕРИ СВОЮ ТАКТИКУ", 34, Text, true);
            TextAt(new Rect(x, y - 22, 1360, 32), "ОДНА СПЕЦИАЛИЗАЦИЯ НА ЗАБЕГ. ПОСЛЕ НЕЁ ТЫ ПОЛУЧИШЬ ОБЫЧНЫЙ НАВЫК ЭТОГО УРОВНЯ.", 16, SquareView.Mint);
            string[] benefits = { "Пули наносят на 60% больше урона.\nСильнее против боссов и одиночных целей.",
                "Молния +40% урона, спутники +60%.\nМолния сразу открывается, если её нет.",
                "Бег на 25% быстрее, радиус магнита +1.5.\nСобирай больше опыта и выбирай позиции." };
            string[] costs = { "ЦЕНА: скорость движения −15%.", "ЦЕНА: урон пуль −25%.", "ЦЕНА: максимальное здоровье −20%." };
            CombatStats current = world.Stats;
            for (int i = 0; i < 3; i++)
            {
                float cx = x + i * 462; Color accent = i == 0 ? SquareView.Gold : i == 1 ? SquareView.Blue : SquareView.Mint;
                CombatStats next = world.PreviewSpecialization(i);
                PanelAt(new Rect(cx, y + 30, 436, 460), accent);
                SkillIcon(new Rect(cx + 24, y + 54, 52, 52), i == 0 ? Module.Kinetic : i == 1 ? Module.Arc : Module.Thrusters, accent);
                TextAt(new Rect(cx + 24, y + 127, 388, 48), SkillText.Specialty((Specialization)(i + 1)), 27, Text, true);
                TextAt(new Rect(cx + 24, y + 188, 388, 95), benefits[i], 20, Text, wrap: true);
                TextAt(new Rect(cx + 24, y + 292, 388, 55), costs[i], 18, SquareView.Red, true, wrap: true);
                string preview = i == 0 ? "Пуля: " + SkillText.N(current.BulletDamage) + " → " + SkillText.N(next.BulletDamage) :
                    i == 1 ? "Молния: " + SkillText.N(current.ArcDamage) + " → " + SkillText.N(next.ArcDamage) :
                    "Здоровье: " + SkillText.N(world.MaxHull) + " → " + SkillText.N(world.MaxHull * .8f);
                TextAt(new Rect(cx + 24, y + 357, 388, 40), preview, 22, accent, true);
                if (ButtonAt(new Rect(cx + 24, y + 418, 388, 45), "ВЫБРАТЬ [ " + (i + 1) + " ]", accent)) { Pick(i); break; }
            }
            TextAt(new Rect(x, y + 518, 1360, 48), "Сильная сборка — это не всё сразу: усиливай выбранный стиль и учитывай его слабое место.", 18, Muted);
        }

        private void BuildScreen()
        {
            Shade(); float x = (uiWidth - 1380) / 2, y = (uiHeight - 738) / 2;
            PanelAt(new Rect(x, y, 1380, 738), SquareView.Mint);
            TextAt(new Rect(x + 28, y + 22, 1000, 42), "ТВОЯ СБОРКА / " + SkillText.Specialty(world.Specialization), 28, Text, true);
            if (ButtonAt(new Rect(x + 1110, y + 24, 240, 38), "ЗАКРЫТЬ [ TAB ]", SquareView.Mint)) buildOpen = false;
            TextAt(new Rect(x + 28, y + 77, 1300, 25), "ИГРА НА ПАУЗЕ. ХАРАКТЕРИСТИКИ БЕЗ ВРЕМЕННОЙ ПЕРЕГРУЗКИ.", 15, Muted);
            CombatStats s = world.Stats;
            string[] labels = { "АВТОПУШКА", "СПУТНИКИ", "ЦЕПНАЯ МОЛНИЯ", "ДВИЖЕНИЕ И ВЫЖИВАНИЕ" };
            string[] values = {
                "Урон пули: " + SkillText.N(s.BulletDamage) + " / пуль: " + s.Shots + "\nЗалпов/сек: " + SkillText.N(1 / s.ShotPeriod) + " / целей: " + s.PierceTargets,
                "Количество: " + s.Satellites + " / урон: " + (s.Satellites > 0 ? SkillText.N(s.OrbitDamage) : "—") + "\nРадиус: " + SkillText.N(s.OrbitRadius) + " / удары не чаще 0.35 с",
                s.ArcDamage > 0 ? "Урон: " + SkillText.N(s.ArcDamage) + " / целей: " + s.ArcTargets + "\nРазряд каждые " + SkillText.N(s.ArcPeriod) + " с / дальность 9" : "Навык ещё не открыт\nВозьми «Цепную молнию» при повышении уровня",
                "Скорость: " + SkillText.N(s.Speed) + " / рывок: " + SkillText.N(s.DashPeriod) + " с\nМагнит: " + SkillText.N(s.MagnetRadius) + " / лечение: " + SkillText.N(s.Regen) + "/с"
            };
            for (int i = 0; i < 4; i++)
            {
                float cx = x + 28 + i * 337;
                TextAt(new Rect(cx, y + 128, 318, 26), labels[i], 16, SquareView.Mint, true);
                TextAt(new Rect(cx, y + 166, 318, 70), values[i], 16, Text, wrap: true);
            }
            Fill(new Rect(x + 28, y + 250, 1324, 1), LineColor);
            TextAt(new Rect(x + 28, y + 270, 600, 28), "НАВЫКИ В ЭТОМ ЗАБЕГЕ", 18, Text, true);
            int item = 0;
            foreach (Upgrade u in Upgrades.All)
            {
                int rank = world.Rank(u.Id); if (rank <= 0) continue;
                int column = item / 8, row = item % 8; item++;
                float cx = x + 28 + column * 324, cy = y + 316 + row * 38;
                SkillIcon(new Rect(cx, cy + 1, 24, 24), u.Id, SkillColor(u.Id));
                TextAt(new Rect(cx + 35, cy, 285, 30), u.Name + "  " + rank + (u.MaxRank > 0 ? "/" + u.MaxRank : ""), 16, Text);
            }
            float rx = x + 1020;
            Fill(new Rect(rx - 24, y + 270, 1, 400), LineColor);
            TextAt(new Rect(rx, y + 272, 322, 30), "ПЛАН СОЧЕТАНИЙ", 18, SquareView.Gold, true);
            Recipe(rx, y + 321, "ИОННАЯ БУРЯ", world.HasStorm,
                "Молния " + world.Rank(Module.Arc) + "/2 + спутники " + world.Rank(Module.Satellite) + "/2", "+2 цели молнии, +30% урона спутников");
            Recipe(rx, y + 431, "ПРОБИВАЮЩИЙ ВЕЕР", world.HasShatter,
                "Веер " + world.Rank(Module.Multishot) + "/2 + пробитие " + world.Rank(Module.Pierce) + "/1", "Пули отталкивают врагов");
            Recipe(rx, y + 541, "ЛЕДЯНОЙ РАЗЛОМ", world.HasIcebreak,
                "Лёд " + world.Rank(Module.Frost) + "/2 + взрыв " + world.Rank(Module.Blast) + "/2", "+50% урона взрыва замедленным");
            TextAt(new Rect(x + 28, y + 689, 1320, 30), "SPACE: рывок с неуязвимостью.  Q: при 100% — взрыв, лечение 12 и усиление оружия на 5 секунд.", 16, Muted);
        }
        private void Recipe(float x, float y, string name, bool active, string requirement, string effect)
        {
            TextAt(new Rect(x, y, 322, 25), (active ? "✓ " : "□ ") + name, 16, active ? SquareView.Mint : SquareView.Gold, true);
            TextAt(new Rect(x, y + 31, 322, 26), requirement, 15, Text);
            TextAt(new Rect(x, y + 61, 322, 43), effect, 14, Muted, wrap: true);
        }

        private void SkillIcon(Rect r, Module id, Color color)
        {
            Fill(r, new Color(color.r * .1f, color.g * .1f, color.b * .1f));
            float u = r.width / 8;
            if (id == Module.Multishot || id == Module.Kinetic || id == Module.Pierce || id == Module.Rate)
            {
                int count = id == Module.Multishot ? 3 : 1;
                for (int i = 0; i < count; i++) Fill(new Rect(r.x + 2 * u, r.y + (2 + i * 1.5f) * u, 4 * u, u), color);
            }
            else if (id == Module.Satellite || id == Module.OrbitRadius || id == Module.OrbitPower)
            {
                Fill(new Rect(r.x + 3 * u, r.y + 3 * u, 2 * u, 2 * u), color);
                Fill(new Rect(r.x + u, r.y + u, 1.5f * u, 1.5f * u), color);
                Fill(new Rect(r.x + 5.5f * u, r.y + 5.5f * u, 1.5f * u, 1.5f * u), color);
            }
            else if (id == Module.Arc || id == Module.Chain)
                for (int i = 0; i < 5; i++) Fill(new Rect(r.x + (2 + i % 2) * u, r.y + (1.5f + i) * u, 2.5f * u, u), color);
            else if (id == Module.Blast || id == Module.Frost)
            {
                Fill(new Rect(r.x + u, r.y + u, 6 * u, 6 * u), color);
                Fill(new Rect(r.x + 1.5f * u, r.y + 1.5f * u, 5 * u, 5 * u), Panel);
                Fill(new Rect(r.x + 3 * u, r.y + 3 * u, 2 * u, 2 * u), color);
                if (id == Module.Frost)
                { Fill(new Rect(r.x + 3.5f * u, r.y, u, r.height), color); Fill(new Rect(r.x, r.y + 3.5f * u, r.width, u), color); }
            }
            else if (id == Module.Ram || id == Module.Dash || id == Module.Thrusters)
                for (int i = 0; i < 3; i++) Fill(new Rect(r.x + (1 + i * 2) * u, r.y + (3 - i * .5f) * u, 1.5f * u, (2 + i) * u), color);
            else
            {
                Fill(new Rect(r.x + 3 * u, r.y + u, 2 * u, 6 * u), color);
                Fill(new Rect(r.x + u, r.y + 3 * u, 6 * u, 2 * u), color);
            }
        }
    }
}
