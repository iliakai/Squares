# Squares — instructions for subsequent sessions

Work only inside `P:\GameDevelopment\Squares`. Remote: https://github.com/iliakai/Squares.
Use Unity **6000.3.26f1** with the Windows build module. Read `docs/current-state.md`,
`docs/game-design.md`, and the relevant specification before changing behavior.

## Specification-driven development (SDD)
1. Define/change a specification and acceptance criteria in `docs/specs/`.
2. Implement the smallest complete playable iteration.
3. Run the simulation checks, Unity import/compile, and a Windows build as appropriate.
4. Update `docs/current-state.md` with evidence, limitations, and the next task.
5. Commit and push completed changes to origin/main (the user explicitly requested this).

## Product
2D single-player arena roguelite; every visual shape is rectangular/square. Fast movement,
dash, automatic weapons, branching module builds, synergies, escalating waves, and persistent
unlocks. Keep simulation independent of Unity. This is preparation for multiplayer,
not a claim that networking is implemented. Do not add external art dependencies.

## "Ещё час" / autonomous playtest sessions
Only start the timed workflow when the user explicitly asks for it. Record a start time
and deadline with a real clock. Spend at least the requested duration playing/testing,
observing, improving, and verifying. Finish the iteration in progress after the deadline,
build and push, then invite the user to play. Distinguish manual input, automated gameplay,
simulation tests, and visual inspection honestly; never call automated tests manual play.
Do not shorten a requested hour or claim a playtest happened without evidence.
Record observations and outcomes in `docs/playtests/`.

## Environment recovery
Some sessions fail launching sandboxed commands with `setup refresh had errors`.
`apply_patch` may still work. Retry necessary shell commands with the escalation mechanism;
do not work outside this project or change global Git safety configuration. If Git ownership
differs, use per-command `git -c safe.directory=P:/GameDevelopment/Squares ...`.
Keep credentials, Library, Temp, Logs, Builds, and Saves out of Git.
When asked "продолжай", restore context from these documents and proceed without asking
the user to repeat the requirements. No additional agents are required.
