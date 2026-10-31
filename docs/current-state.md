# Current state

## Context restored 2026-10-31

User requested a new square-only 2D Unity roguelite, SDD, future hour-long autonomous
playtest/improvement sessions, and immediate pushes to https://github.com/iliakai/Squares.
Two earlier sessions created only an empty Git checkout, then failed with the sandbox
error `setup refresh had errors`. Requirements were recovered from local Codex history.

Working root: `P:\GameDevelopment\Squares`. Unity: `6000.3.26f1` with Windows support.
Sandboxed shell startup still fails; escalated commands and apply_patch work.

## Current: v0.2.0 — readable skills

User requested a 30-minute improve/play/improve iteration. Started 19:02:27 UTC;
minimum finish 19:32:27 UTC. Work continued through the final verification after the deadline.
See specification 002 and `docs/playtests/2026-10-31-readable-skills.md` for the record.

- Russian menus, names, descriptions, before/after stats, and tactical hints.
- 23 upgrade types: new explosive bullets, slowing bullets, and dash impact.
- Level-five specializations with real costs: artillery, storm, scout. They do not consume
  the normal level-up choice. One free reroll per run, always offering something new.
- TAB pauses and shows weapon stats, every purchased skill, and three synergy recipes.
- Combat and previews share CombatStats. Explosions, cold, and ram have distinct feedback.

Play: `Builds/Windows/Squares.exe`. Controls: WASD/arrows, Space/Shift dash, Q overload,
1/2/3 upgrades, R reroll in a draft, TAB build, Esc pause, F11 fullscreen.
Existing profiles remain compatible; smoke tests never award flux to the real save.

Final v0.2 checks: 108,123 simulation assertions; Windows x64 build passed. Three 400-second
pilots defeated two bosses each. Actual Windows player at 1280×720: 240 simulated seconds,
591 kills, level 20, 19 ordinary choices plus specialization, one boss, 82 flux, zero errors;
both pause and build-screen pause checks passed. Inspected real menu, draft, combat,
specialization, build, and results images. Current logs/screenshots are in `Logs/Smoke/`;
Unity deletes project Temp on import, so durable review evidence must go in Logs.

These are automated gameplay and visual inspection, not human manual play. Starting build
was run with a real-time pilot and observed at 30-second checkpoints through 210 seconds;
subsequent Unity import cleared its scratch Temp artifacts. Updated build was replayed
with the accelerated pilot and explicit behavioral regression checks.

## Foundation v0.1.0 — historical context

Specification 001 implemented: pure C# fixed-step simulation; square-only procedural visuals,
audio, menu, three starting reactors (two unlockable), 20 upgrade types, kinetic/orbit/arc
weapons, two build synergies, dash, overload, four regular enemy types, elites, bosses every
180 seconds, endless escalation, pause, extraction/death/results, portable profile with
backup recovery, research purchases, and Windows build/smoke scripts.

Play: `Builds/Windows/Squares.exe`. Unity project: this root, scene `Assets/Scenes/Arena.unity`.
Controls: WASD/arrows, Space/Shift dash, Q overload, 1/2/3 upgrades, Esc pause, F11 fullscreen.
Save directory: `Builds/Windows/Saves` in the player; `Saves` in the Editor. No test rewards
were written to the user's real profile. The original interface was in English; v0.2 is Russian.

## Foundation verification (historical)

- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./tools/Build.ps1`:
  Unity import/compilation, 108,097 assertions, then Windows x64 Mono build passed.
- Checks include seeded replay, movement normalization/bounds, paused drafts, choice/rank
  validity, capped-build fallbacks, dash/overload gating, resource/entity bounds, death,
  save roundtrip/backup recovery, and purchase validation.
- Three normal-command automated pilots with max legitimate research ran 400 simulated
  seconds each: 1,750 / 1,707 / 1,773 kills; level 37; two bosses defeated in each run.
- Windows player, fresh Striker, seed 735193, 240 simulated seconds: 575 kills, level 21,
  20 drafts, one boss defeated, 80 flux earned, zero errors, pause passed.
- Repeated player run at 1280×720 gave the same fingerprint `12090407610100133376`
  as at 1600×900. Visually inspected gameplay, menu, draft, pause, and results screenshots.
  Current screenshots/log/report are in ignored `Temp/Smoke/`; build log in `Logs/Build.log`.
- Hidden Windows launches produce black captures even though gameplay runs. Smoke.ps1
  therefore captures only with explicit `-Visible`; hidden tests are not visual validation.

Those were automated accelerated gameplay and screenshot inspection, not manual play.

The user also opened a separate Unity Hub template at `Squares/Squares` during this session.
It is untouched and ignored; the actual game project is the root directory. Do not accidentally
edit or build the nested template.

## Delivery

Final Windows build and all simulation checks passed. Source is delivered on `origin/main`
at https://github.com/iliakai/Squares. If local Git requests a login, the connected GitHub
connector has write access: publish the same tree, verify its SHA, then fetch and track main;
preserve the local pre-publication commit on a backup branch. Generated builds,
logs, saves, and the separate nested Unity template are intentionally excluded from Git.
The Windows executable remains available locally at `Builds/Windows/Squares.exe`.

## Next iteration

Have the user play v0.2. On an explicit timed request, follow AGENTS.md's real-time
protocol and record observations in `docs/playtests/`. Prioritize first-run difficulty and
upgrade pacing, new run modifiers, and boss tells. The bot currently finds strong builds
and ends many runs at full hull; this is not proof of good balance or player retention.
Collect human feedback on which specialization and combo are understandable and worthwhile.
Freeform module construction, multiplayer, controller support, and cloud saves are not
implemented. Existing simulation boundaries prepare future networking; no lockstep guarantee.
