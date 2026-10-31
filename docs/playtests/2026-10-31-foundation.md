# Foundation verification — 2026-10-31

Type: automated gameplay and visual inspection. Not a timed hour or manual play.

## Evidence

- Seeded deterministic simulation: 108,097 assertions passed.
- Three 400-second pilots with fully purchased research defeated two bosses each.
- Fresh Striker Windows player at 1600×900 and 1280×720: 240 seconds, 575 kills,
  level 21, one boss, 20 upgrade choices, 80 banked flux, zero logged exceptions/errors.
- Pause checked in the running player. Fingerprint identical across screen sizes.
- Visually inspected five screens: menu, first draft, pause, gameplay at 45 seconds,
  and extraction results at four minutes. No clipping found at 1280×720.
- Save checks use `Temp/ProfileChecks`; smoke never writes to the real user profile.

## Observations and fixes

- A fully ranked build originally had only two legal fallback cards. Added the uncapped
  Flux Capacitor upgrade so three unique choices are always available.
- Prevented XP loss at the pickup cap by merging value into an existing pickup.
- Empty expeditions award zero currency, preventing instant-restart currency farming.
- A new dash no longer shortens an existing damage invulnerability grace period.
- Improved pause/settings button contrast after inspecting the actual player.
- Windows hides the swapchain for a hidden process; captures were black. Added explicit
  visible preview support and stopped claiming hidden runs validate rendered pixels.

## Remaining play questions

- Does the first minute provide enough danger, or do arc/orbit hybrid builds snowball too early?
- Do players notice charged overload and remember to dash without the pilot's knowledge?
- Is a 180-second first boss an engaging goal? Is extraction vs continued risk meaningful?
- Are reactor unlock prices sensible relative to typical first-run human rewards?

Use actual human feedback and a separately requested timed iteration to answer these.
