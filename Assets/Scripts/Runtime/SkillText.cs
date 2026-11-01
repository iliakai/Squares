using System.Globalization;
using Squares.Simulation;

namespace Squares
{
    public static class SkillText
    {
        public static string N(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);
        private static string Change(string name, float before, float after, string suffix = "") =>
            name + ": " + N(before) + " → " + N(after) + suffix;
        public static string Category(string category)
        {
            switch (category)
            {
                case "KINETIC": return "ПУЛИ";
                case "SATELLITE": return "СПУТНИКИ";
                case "ARC": return "МОЛНИЯ";
                case "WEAPON": return "БОЕВОЙ НАВЫК";
                case "DAMAGE": return "УРОН";
                case "SURVIVAL": return "ВЫЖИВАНИЕ";
                case "ENDLESS": return "БЕЗ ЛИМИТА";
                default: return "ПОДДЕРЖКА";
            }
        }
        public static string Numbers(World w, Module id)
        {
            CombatStats a = w.Stats, b = w.Preview(id);
            switch (id)
            {
                case Module.Kinetic: return Change("Урон пули", a.BulletDamage, b.BulletDamage) + "\n" + Change("Пуль в залпе", a.Shots, b.Shots);
                case Module.Power:
                case Module.Overdrive: return Change("Урон пули", a.BulletDamage, b.BulletDamage) +
                    (a.Satellites > 0 ? "\n" + Change("Урон спутника", a.OrbitDamage, b.OrbitDamage) :
                    a.ArcDamage > 0 ? "\n" + Change("Урон молнии", a.ArcDamage, b.ArcDamage) : "\nУсиливает и будущее оружие");
                case Module.Rate: return Change("Залпов / сек", 1 / a.ShotPeriod, 1 / b.ShotPeriod) + "\n" + Change("Зарядка молнии", a.ArcPeriod, b.ArcPeriod, " с");
                case Module.Multishot: return Change("Пуль в залпе", a.Shots, b.Shots) + "\nУрон каждой: " + N(b.BulletDamage);
                case Module.Pierce: return Change("Целей одной пулей", a.PierceTargets, b.PierceTargets) + "\nПробитие без потери урона";
                case Module.Satellite: return Change("Спутников-щитов", a.Satellites, b.Satellites) + "\nУрон касания: " + N(b.OrbitDamage);
                case Module.OrbitPower: return Change("Урон касания", a.OrbitDamage, b.OrbitDamage) + "\n" + Change("Оборотов / сек", a.OrbitSpeed / 6.2831853f, b.OrbitSpeed / 6.2831853f);
                case Module.OrbitRadius: return Change("Радиус орбиты", a.OrbitRadius, b.OrbitRadius) + "\nСпутники также крупнее";
                case Module.Arc: return Change("Урон разряда", a.ArcDamage, b.ArcDamage) + "\n" + Change("Целей разрядом", a.ArcDamage > 0 ? a.ArcTargets : 0, b.ArcTargets);
                case Module.Chain: return Change("Целей разрядом", a.ArcTargets, b.ArcTargets) + "\nУрон каждой: " + N(b.ArcDamage);
                case Module.Hull: return Change("Макс. здоровье", w.MaxHull, w.MaxHull + 20) + "\n" + Change("Здоровье сейчас", w.Hull, System.Math.Min(w.MaxHull + 20, w.Hull + 30));
                case Module.Repair: return Change("Здоровье сейчас", w.Hull, System.Math.Min(w.MaxHull, w.Hull + w.MaxHull * .4f)) + "\nМаксимум не меняется";
                case Module.Magnet: return Change("Радиус сбора", a.MagnetRadius, b.MagnetRadius) + "\nНе нужно касаться опыта";
                case Module.Thrusters: return Change("Скорость", a.Speed, b.Speed) + "\nСкорость рывка не меняется";
                case Module.Dash: return Change("Ожидание рывка", a.DashPeriod, b.DashPeriod, " с") + "\nSPACE: сквозь опасность";
                case Module.Critical: return Change("Шанс двойного урона", a.CriticalChance * 100, b.CriticalChance * 100, "%") + "\nКритический удар: ровно ×2";
                case Module.Regen: return Change("Здоровье / сек", a.Regen, b.Regen) + "\nРаботает без остановки";
                case Module.Learning: return Change("Бонус опыта навыка", w.Rank(id) * 15, (w.Rank(id) + 1) * 15, "%") + "\nСкладывается с исследованием";
                case Module.Capacitor: return Change("Заряд за убийство", 2 * (1 + w.Rank(id) * .08f), 2 * (1 + (w.Rank(id) + 1) * .08f)) + "\nКомбо даёт заряд сверху";
                case Module.Blast: return Change("Урон соседям", a.BlastFraction * 100, b.BlastFraction * 100, "% пули") + "\n" + Change("Радиус взрыва", a.BlastRadius, b.BlastRadius);
                case Module.Frost: return Change("Замедление", a.SlowFraction * 100, b.SlowFraction * 100, "%") + "\n" + Change("Длительность", a.SlowDuration, b.SlowDuration, " с");
                case Module.Ram: return Change("Урон рывка", a.RamDamage, b.RamDamage) + "\nПо каждой цели на пути";
                default: return "";
            }
        }
        public static string Tip(Module id)
        {
            switch (id)
            {
                case Module.Multishot: case Module.Pierce: return "План: веер 2 + пробитие 1 дают отталкивание пулями.";
                case Module.Satellite: case Module.Arc: return "План: спутники 2 + молния 2 открывают ионную бурю.";
                case Module.Frost: case Module.Blast: return "План: лёд 2 + взрывы 2 усиливают взрыв по замедленным на 50%.";
                case Module.Ram: case Module.Dash: return "Тактика: прорывайся через край толпы, оставляя путь к отступлению.";
                case Module.OrbitRadius: case Module.OrbitPower: return "Тактика: веди врагов вдоль орбиты, сохраняя дистанцию.";
                case Module.Chain: return "Тактика: собирай врагов в цепочку на расстоянии прыжка молнии.";
                case Module.Repair: return "Выбор на сейчас: выжить вместо усиления. При полном здоровье бесполезен.";
                case Module.Magnet: case Module.Learning: return "Вложение в будущее: быстрее собирай опыт для следующих навыков.";
                default: return "TAB: сравни характеристики и посмотри план своей сборки.";
            }
        }
        public static string Specialty(Specialization specialization)
        {
            switch (specialization)
            {
                case Specialization.Siege: return "Артиллерист";
                case Specialization.Storm: return "Повелитель бури";
                case Specialization.Scout: return "Разведчик";
                default: return "Специализация на уровне 5";
            }
        }
    }
}
