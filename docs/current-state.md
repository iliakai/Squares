# Current state

## Context restored 2026-10-31

User requested a new square-only 2D Unity roguelite, SDD, future hour-long autonomous
playtest/improvement sessions, and immediate pushes to https://github.com/iliakai/Squares.
Two earlier sessions created only an empty Git checkout, then failed with the sandbox
error `setup refresh had errors`. Requirements were recovered from local Codex history.

Working root: `P:\GameDevelopment\Squares`. Unity: `6000.3.26f1` with Windows support.
Sandboxed shell startup still fails; escalated commands and apply_patch work.

## Playable foundation v0.1.0 — implemented

Specification 001 implemented: pure C# fixed-step simulation; square-only procedural visuals,
audio, menu, three starting reactors (two unlockable), 20 upgrade types, kinetic/orbit/arc
weapons, two build synergies, dash, overload, four regular enemy types, elites, bosses every
180 seconds, endless escalation, pause, extraction/death/results, portable profile with
backup recovery, research purchases, and Windows build/smoke scripts.

Play: `Builds/Windows/Squares.exe`. Unity project: this root, scene `Assets/Scenes/Arena.unity`.
Controls: WASD/arrows, Space/Shift dash, Q overload, 1/2/3 upgrades, Esc pause, F11 fullscreen.
Save directory: `Builds/Windows/Saves` in the player; `Saves` in the Editor. No test rewards
were written to the user's real profile. Graphics and interface are currently in English.

## Verification

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

These were automated accelerated gameplay and screenshot inspection, not manual play and
not a one-hour playtest. No one-hour session was requested in this conversation.

The user also opened a separate Unity Hub template at `Squares/Squares` during this session.
It is untouched and ignored; the actual game project is the root directory. Do not accidentally
edit or build the nested template.

## Delivery

Final Windows build and all simulation checks passed. The playable foundation is committed
and delivered on `origin/main` at https://github.com/iliakai/Squares. Generated builds,
logs, saves, and the separate nested Unity template are intentionally excluded from Git.
The Windows executable remains available locally at `Builds/Windows/Squares.exe`.

## Next iteration

Have the user play the executable. On an explicit "Ещё час", follow AGENTS.md's real-time
protocol and record observations in `docs/playtests/`. Prioritize first-run difficulty and
upgrade pacing, new run modifiers, and boss tells. The bot currently finds strong builds
and ends many runs at full hull; this is not proof of good balance or player retention.
Freeform module construction, multiplayer, controller support, and cloud saves are not
implemented. Existing simulation boundaries prepare future networking; no lockstep guarantee.
