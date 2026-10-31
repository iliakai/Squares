# Current state

## Context restored 2026-10-31

User requested a new square-only 2D Unity roguelite, SDD, future hour-long autonomous
playtest/improvement sessions, and immediate pushes to https://github.com/iliakai/Squares.
Two earlier sessions created only an empty Git checkout, then failed with the sandbox
error `setup refresh had errors`. Requirements were recovered from local Codex history.

Working root: `P:\GameDevelopment\Squares`. Unity: `6000.3.26f1` with Windows support.
Sandboxed shell startup still fails; escalated commands and apply_patch work.

## Current: v0.3.0 — experience constellations

Specification 003 implemented. Requested hour started 2026-10-31 19:58:55 UTC, minimum
finish 20:58:55 UTC. The last verification/delivery iteration continued after the deadline.
No additional gameplay work was started after the deadline; final comparisons and delivery
are recorded in `docs/playtests/2026-10-31-constellations.md`.

- Six recognizable main-star figures: Cassiopeia, Big Dipper in Ursa Major, Orion, Cygnus,
  Lyra, Leo (5–9 stars). Pure C# matcher supports rotation, translation, scale, reflection,
  and small positional errors. Extra pickups are ignored; correspondence is one-to-one.
- A match consumes only its stars and creates one large golden pickup at the centroid,
  with exactly 10 times their total base value. 64-bit arithmetic; player XP bonus applied
  once on collection. Celestial pickups cannot become constellation stars again.
- Name, actual star positions and connecting lines appear, expand toward the viewer,
  fly offscreen and fade. Reward waits 2.5 seconds before becoming collectible.
- C opens a pausing atlas with six diagrams and discovery collection; CTRL holds harvesting
  so players can leave XP fields deliberately. Discovered atlas entries bank after a run.
- Existing saves remain compatible. No tests wrote to the user's real profile.

Final checks: 108,123 base simulation assertions plus 275 constellation assertions passed;
Windows x64 build passed. At 250 pickups, 100 bounded searches took about 199.5 ms total
(~2 ms/search, once per 30 ticks). Six noisy rotated/mirrored patterns, missing/stretched/
collinear negatives, arena-size shapes, >32-bit rewards, consumption, no recursive rewards,
learning bonus, grace period and queued level-ups are covered.

Actual Windows player, final seed 12035/core 0, 360 simulated seconds, no fixture:
1,348 kills, level 29, one boss, four discoveries, two collected rewards/330 base XP,
390 XP left on the arena, zero errors. Same final build with legal periodic CTRL holds:
1,387 kills, level 44, one boss, 59 discoveries, 58 rewards/9,270 base XP collected.
The strategic advantage is intentionally powerful; balance is a future human-feedback topic.

A six-minute real-time pilot on the preceding build found five natural constellations,
collected all five/750 XP and defeated a boss without errors. Explicit test-only fixtures
separately verified 15→150 and 21→210 rewards. Real animation and atlas screens inspected
at 1280×720; fixed atlas line transforms found during visual review. Logs in `Logs/Review003`.
These are automatic pilots, simulation tests and image review, not human manual play.

Play: `Builds/Windows/Squares.exe`. C: atlas, CTRL: hold XP, TAB: build, WASD: move,
SPACE: dash, Q: overload, 1/2/3: upgrade, R: one reroll, ESC: pause, F11: fullscreen.
Source delivered on origin/main; final source tree verified identical to the local commit.

## v0.2.0 — readable skills (historical)

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

Have the user play v0.3. On an explicit timed request, follow AGENTS.md's real-time
protocol and record observations in `docs/playtests/`. Prioritize first-run difficulty and
upgrade pacing, new run modifiers, and boss tells. The bot currently finds strong builds
and ends many runs at full hull; this is not proof of good balance or player retention.
Collect human feedback on which specialization and combo are understandable and worthwhile.
Freeform module construction, multiplayer, controller support, and cloud saves are not
implemented. Existing simulation boundaries prepare future networking; no lockstep guarantee.
