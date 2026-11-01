using System;
using Squares.Simulation;
using UnityEngine;

namespace Squares.Editor
{
    public static class ProjectileDefenseChecks
    {
        private static int assertions;
        private static void Check(bool condition, string message)
        { assertions++; if (!condition) throw new Exception("PROJECTILE_DEFENSE_FAILED: " + message); }
        private static World Arena(int core = 0)
        {
            var w = new World(4021, core); w.Ranks[(int)Module.Kinetic] = 0; return w;
        }
        private static Bullet Shot(int id, V2 position, V2 velocity, bool hostile, int pierce = 0) => new Bullet {
            Id = id, Position = position, Velocity = velocity, Hostile = hostile,
            Life = 2, Size = hostile ? .16f : .1f, Damage = 19, Pierce = pierce
        };

        public static void Run()
        {
            assertions = 0;
            var headOn = Arena();
            headOn.Ranks[(int)Module.Blast] = 1; headOn.Ranks[(int)Module.Frost] = 1;
            headOn.Bullets.Add(Shot(1001, new V2(-1, 5), new V2(120, 0), false, 3));
            headOn.Bullets.Add(Shot(1002, new V2(1, 5), new V2(-120, 0), true));
            headOn.Step(default);
            Check(headOn.Bullets.Count == 0 && headOn.ProjectilesIntercepted == 1, "Fast opposite bullets annihilate, even with pierce");
            bool parry = false, explosion = false;
            foreach (WorldEvent e in headOn.Events) { parry |= e.Kind == EffectKind.Parry; explosion |= e.Kind == EffectKind.Blast || e.Kind == EffectKind.Chill; }
            Check(parry && !explosion, "Parry feedback without splash or frost");
            var cross = Arena();
            cross.Bullets.Add(Shot(1003, new V2(-1, 5), new V2(120, 0), false));
            cross.Bullets.Add(Shot(1004, new V2(0, 4), new V2(0, 120), true));
            cross.Step(default);
            Check(cross.Bullets.Count == 0 && cross.ProjectilesIntercepted == 1, "Crossing paths collide between endpoints");
            var miss = Arena();
            miss.Bullets.Add(Shot(1005, new V2(-1, 5), new V2(120, 0), false));
            miss.Bullets.Add(Shot(1006, new V2(1, 5.4f), new V2(-120, 0), true));
            miss.Step(default);
            Check(miss.Bullets.Count == 2 && miss.ProjectilesIntercepted == 0, "Close parallel miss does not cancel");
            var allies = Arena();
            allies.Bullets.Add(Shot(1007, new V2(-1, 5), new V2(120, 0), false));
            allies.Bullets.Add(Shot(1008, new V2(1, 5), new V2(-120, 0), false));
            allies.Step(default);
            Check(allies.Bullets.Count == 2, "Friendly bullets do not collide with each other");
            var shield = Arena(1);
            for (int i = 0; i < 3; i++) shield.Bullets.Add(Shot(1010 + i, new V2(3, (i - 1) * .2f), new V2(-240, 0), true));
            shield.Step(default);
            Check(shield.Bullets.Count == 0 && shield.ShieldBlocks == 3 && shield.Hull == shield.MaxHull,
                "A satellite blocks multiple incoming bullets before hull impact");
            Check(shield.Rank(Module.Satellite) == 1, "Shield survives interception");
            var gap = Arena(1);
            gap.Bullets.Add(Shot(1020, new V2(0, 3), new V2(0, -240), true)); gap.Step(default);
            Check(gap.ShieldBlocks == 0 && gap.Hull == gap.MaxHull - 19, "Gap in orbit remains vulnerable");
            var movingShield = Arena(1);
            movingShield.Bullets.Add(Shot(1021, new V2(1.65f, -.45f), default, true));
            movingShield.Step(new Command(new V2(0, 1), true));
            Check(movingShield.ShieldBlocks == 1 && movingShield.Bullets.Count == 0, "Moving shield covers its swept path");
            var early = Arena();
            early.Bullets.Add(Shot(1030, new V2(3, 0), new V2(-360, 0), true));
            early.Bullets.Add(Shot(1031, new V2(1.5f, 0), default, false)); early.Step(default);
            Check(early.Hull == early.MaxHull && early.ProjectilesIntercepted == 1, "Interception before hull prevents damage");
            var late = Arena();
            late.Bullets.Add(Shot(1032, new V2(3, 0), new V2(-360, 0), true));
            late.Bullets.Add(Shot(1033, new V2(-3, 0), default, false)); late.Step(default);
            Check(late.Hull == late.MaxHull - 19 && late.ProjectilesIntercepted == 0 && late.Bullets.Count == 1,
                "Later interception cannot undo earlier hull damage");
            var single = Arena();
            single.Bullets.Add(Shot(1040, new V2(-2, 5), new V2(240, 0), false));
            single.Bullets.Add(Shot(1041, new V2(-1, 5), default, true));
            single.Bullets.Add(Shot(1042, new V2(1, 5), default, true)); single.Step(default);
            Check(single.ProjectilesIntercepted == 1 && single.Bullets.Count == 1 && single.Bullets[0].Id == 1042,
                "One friendly bullet cannot intercept twice after destruction");
            for (int pierce = 0; pierce < 2; pierce++)
            {
                var order = Arena();
                var enemy = new Enemy { Id = 1050, Position = new V2(-1, 5), HP = 100, MaxHP = 100, Size = .2f, Timer = 100 };
                order.Enemies.Add(enemy);
                order.Bullets.Add(Shot(1051, new V2(0, 5), new V2(-360, 0), false, pierce));
                order.Bullets.Add(Shot(1052, new V2(-6, 5), new V2(360, 0), true)); order.Step(default);
                Check(enemy.HP == 81 && order.ProjectilesIntercepted == pierce,
                    "Enemy hit consumes a normal bullet; piercing bullet can continue to intercept");
            }
            Debug.Log("SQUARES_PROJECTILE_DEFENSE_PASS assertions=" + assertions);
        }
    }
}
