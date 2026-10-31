# 001 — Playable foundation

Status: implemented and validated locally; delivery tracked in current-state.md.
Engine: Unity 6000.3.26f1, Windows x64, built-in renderer.

## Requirements and acceptance

1. A clean checkout imports, generates one arena scene, and builds an executable using
   `tools/Build.ps1`. No downloaded art or optional render pipelines required.
2. Menu explains movement, dash, automatic attacks, upgrade picks, and overload. Starting
   a run works by click or Enter. Esc freezes/resumes combat; retry returns to a fresh run.
3. Simulation ticks at 60 Hz. Identical seed and tick commands give the same state on the
   same runtime. Movement clamps to arena bounds. Diagonal movement is normalized.
4. Three weapons, at least 15 meaningful upgrades, three distinct starting reactors,
   three-card drafts, rank caps, and a repair fallback. A draft pauses simulation until chosen.
   Surplus XP can queue further drafts and must not disappear.
5. At least four enemy behaviors, elites, increasing sectors, a boss at 180 seconds, and
   continued scaling after victory. Enemy/projectile counts have safety caps.
6. Dash has cooldown/invulnerability; overload requires full charge, heals, damages enemies,
   buffs attack rate, and consumes charge. Contact damage has a grace period.
7. Player can die or end the expedition through the pause menu, view kills/time/level/bosses/earned flux, bank exactly once, retry, and buy
   persistent reactor/research unlocks. Menu cannot spend currency twice with a negative result.
8. Health, XP, charge, cooldown, time, sector, active build, damage feedback, upgrade effects,
   enemy tells, and boss health are visible at 1280×720 or higher. All rendered shapes are
   squares/rectangles (text excepted). Audio and flashes can be disabled in the pause menu.
9. Checks cover deterministic replay, bounded movement, paused drafts, upgrade validity,
   dash and overload gating, progression, safety caps, and automated combat through a boss.
   A Windows player smoke run verifies boot/input adapter/draft/combat/results without exceptions.
10. Update state and commit/push source to the connected GitHub remote after validation.

## Architecture

`Squares.Simulation`: plain C# vectors, RNG, entities, upgrade catalog, fixed-step world.
`Squares.Runtime`: Unity input, pooled square renderers, procedural audio, GUI, portable saves.
`Squares.Editor`: scene setup, executable build, deterministic regression checks.

Commands contain movement, dash, and overload. Persistent research is passed once at run
creation. Runtime never edits simulation entities for gameplay. The simulation publishes
bounded visual/audio events. No UnityEngine, realtime clock, disk, or global RNG in simulation.
Same-runtime repeatability is tested; cross-platform float determinism is not promised.

## Evidence

Fill `docs/current-state.md` with commands, output markers, and known limits after checking.
Automated play is not manual play. A requested one-hour session is a separate timed task.
