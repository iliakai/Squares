using System;

namespace Squares.Simulation
{
    public enum Module
    {
        Kinetic, Power, Rate, Multishot, Pierce, Satellite, OrbitPower, OrbitRadius,
        Arc, Chain, Hull, Repair, Magnet, Thrusters, Dash, Critical, Regen, Learning, Overdrive, Capacitor,
        Blast, Frost, Ram, Count
    }

    public sealed class Upgrade
    {
        public readonly Module Id;
        public readonly string Name, Category, Description;
        public readonly int MaxRank;
        public Upgrade(Module id, string name, string category, string description, int maxRank)
        { Id = id; Name = name; Category = category; Description = description; MaxRank = maxRank; }
    }

    public static class Upgrades
    {
        public static readonly Upgrade[] All = {
            new Upgrade(Module.Kinetic, "Автопушка", "KINETIC", "Пули сами летят в ближайшего врага. Больше урона каждой пулей.", 5),
            new Upgrade(Module.Power, "Общий урон", "DAMAGE", "Усиливает пули, спутники, молнию, взрывы и таран.", 5),
            new Upgrade(Module.Rate, "Скорострельность", "KINETIC", "Пушка и молния срабатывают чаще. Спутники не ускоряются.", 5),
            new Upgrade(Module.Multishot, "Веер пуль", "KINETIC", "Ещё одна пуля в каждом залпе. Урон одной пули не уменьшается.", 4),
            new Upgrade(Module.Pierce, "Пули навылет", "KINETIC", "Пуля проходит сквозь врага и поражает следующего в ряду.", 3),
            new Upgrade(Module.Satellite, "Боевой спутник", "WEAPON", "Ещё один квадрат вращается вокруг тебя и бьёт врагов при касании.", 5),
            new Upgrade(Module.OrbitPower, "Тяжёлые спутники", "SATELLITE", "Спутники бьют сильнее и вращаются быстрее.", 5),
            new Upgrade(Module.OrbitRadius, "Широкая орбита", "SATELLITE", "Спутники летают дальше от тебя. Лови врагов на краю орбиты.", 3),
            new Upgrade(Module.Arc, "Цепная молния", "WEAPON", "Разряд автоматически перескакивает между близкими врагами.", 5),
            new Upgrade(Module.Chain, "Больше цепей", "ARC", "Каждый разряд молнии поражает ещё одну цель.", 5),
            new Upgrade(Module.Hull, "Больше здоровья", "SURVIVAL", "Увеличивает запас здоровья и сразу лечит на 30.", 5),
            new Upgrade(Module.Repair, "Лечение", "SURVIVAL", "Сразу восстанавливает 40% максимального здоровья. Не постоянный навык.", 0),
            new Upgrade(Module.Magnet, "Магнит опыта", "UTILITY", "Белые квадраты опыта притягиваются с большего расстояния.", 4),
            new Upgrade(Module.Thrusters, "Скорость движения", "UTILITY", "Быстрее бегай между врагами и собирай опыт.", 4),
            new Upgrade(Module.Dash, "Чаще рывок", "UTILITY", "Сокращает ожидание между рывками на SPACE.", 4),
            new Upgrade(Module.Critical, "Критический удар", "DAMAGE", "Повышает шанс двойного урона пуль, спутников и молнии.", 5),
            new Upgrade(Module.Regen, "Регенерация", "SURVIVAL", "Здоровье понемногу восстанавливается даже в бою.", 4),
            new Upgrade(Module.Learning, "Больше опыта", "UTILITY", "Каждый белый квадрат даёт больше опыта. Уровни приходят быстрее.", 4),
            new Upgrade(Module.Overdrive, "Предел урона", "ENDLESS", "Постоянно усиливает всё оружие. Можно брать сколько угодно.", 0),
            new Upgrade(Module.Capacitor, "Заряд перегрузки", "ENDLESS", "Убийства быстрее заряжают мощную способность на Q.", 0),
            new Upgrade(Module.Blast, "Взрывные пули", "KINETIC", "При попадании пуля взрывается квадратом и ранит соседних врагов.", 3),
            new Upgrade(Module.Frost, "Ледяные пули", "KINETIC", "Пули замедляют врагов. На боссах замедление вдвое слабее.", 3),
            new Upgrade(Module.Ram, "Удар рывком", "WEAPON", "Рывок на SPACE пробивает толпу. Каждый враг получает удар один раз за рывок.", 3)
        };

        public static bool Available(Module id, int[] ranks)
        {
            Upgrade u = All[(int)id];
            if (u.MaxRank > 0 && ranks[(int)id] >= u.MaxRank) return false;
            if ((id == Module.Multishot || id == Module.Pierce) && ranks[(int)Module.Kinetic] == 0) return false;
            if ((id == Module.OrbitPower || id == Module.OrbitRadius) && ranks[(int)Module.Satellite] == 0) return false;
            if (id == Module.Chain && ranks[(int)Module.Arc] == 0) return false;
            return true;
        }
    }
}
