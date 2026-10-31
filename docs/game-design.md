# SQUARES // OVERLOAD — design

## Pillars

- Immediate motion: move, dash through a gap, collect data, trigger overload.
- Readable squares: enemies communicate intent through size, hue, and square outlines.
- Decisions every 15–35 seconds: three upgrade cards with ranks and actual effects.
- Builds, not just stats: projectiles, satellites, and arcs create different spatial play.
- Pressure with release: a new sector every 30 seconds, an elite every minute, a boss
  every three minutes. A boss is a rewarding milestone, not a forced end to a build.
- Persistent breadth: unlock distinct starting reactors, then small capped research.

## Loop

Start with a reactor → dodge enemies and square projectiles → collect data → choose one
of three upgrades while time is frozen → exploit synergies and overload → defeat bosses
or die (or extract through the pause menu) → bank flux → buy unlocks/research → try another build.

### Combat

Striker starts with a kinetic weapon. Satellite starts with a stronger orbiting weapon;
Conductor starts with chain arcs. All can learn every weapon. Weapons acquire targets
automatically; positioning and dash timing are the player's active decisions.

Dash grants brief invulnerability and leaves a square trail. OVERLOAD charges through kills;
at full charge, Q triggers a radial square shock pulse, heals a little, and briefly accelerates
weapons. Multikills build a short-lived combo that improves charge gain.

Enemy roles: red chaser, orange charger (pauses/telegraphs before a straight rush), violet
sniper (keeps distance and fires), cyan splitter (spawns small chasers), large elite, boss.
Bosses launch aimed fans and summon escorts. Damage and spawn pressure scale with sectors.
The arena is bounded; edge marks prevent losing the play area.

### Modules

Kinetic: damage, rate, multishot, piercing, speed. Satellites: count, orbit damage/radius.
Arcs: damage and chain targets. Utility: hull, repair, magnet, movement, dash recharge,
critical chance, regeneration, XP. Each rank has a cap; exhausted builds can always choose
repair or an uncapped damage increase. Hybrid thresholds unlock visible synergies.

### Persistence

Flux awarded once per completed run from kills, time survived, and boss milestones. Ending
a zero-progress run awards zero flux. No award on restarting
the app mid-run. Menu purchases save immediately. Versioned local JSON, backup, and validation
protect normal restarts; this is local persistence, not an anti-cheat system.

## Scope boundaries

First foundation: one complete arena, menu, three reactors, weapon builds, bosses, death,
retry, persistent progression, generated audio, settings, reproducible checks, Windows build.
Next: balancing from actual play, run mutators, more boss patterns, controller support.
Networked play, module placement editor, cloud saves, localization, and achievements are
future work. The initial module layout is visual; freeform construction is not yet implemented.

## Iteration 002: readable choices

v0.2 replaces the prototype English interface with Russian descriptions and numerical
before/after previews computed from actual combat stats. TAB pauses and exposes the full
build and synergy requirements. One free reroll per run guarantees a new alternative.
Explosive bullets damage neighbors, cold bullets slow (half effect on bosses), and ram
deals one hit to each enemy crossed during a dash. Blast 2 + Frost 2 unlock Icebreak:
explosions deal 50% more damage to slowed neighbors.

Level five offers a permanent specialization followed by the ordinary skill draft:
Artillery: +60% projectile damage, −15% movement; Storm: unlock lightning, +40% arc and
+60% satellite damage, −25% projectile damage; Scout: +25% movement, +1.5 pickup radius,
−20% maximum hull. These have actual simulation effects, not just UI labels.
