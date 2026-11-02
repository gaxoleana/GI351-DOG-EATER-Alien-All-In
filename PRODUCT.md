# Product

<!-- impeccable:product-schema 1 -->

## Platform

Unity gameplay prototype; the final game's release platform is undecided.

## Stack

Unity 6000.5.10f1, C#, URP 2D, the Input System and an editable uGUI Canvas.
The browser demo remains a gameplay reference.

## Users

Players who want a playful strategy and defense game with automated alien behavior and a few meaningful interventions. This audience is inferred from the GDD.

## Product Purpose

Alien All In is a grid-based idle strategy defense game. Players arrange a team of aliens, start the run, and support their autonomous attacks and activities while enemies interfere. In the current Main prototype, a run ends when time expires, all three fighters are eliminated, or Duck's rescue deadline expires; completing a Process station is a reward and does not end the run early.

## Positioning

The game combines automated alien behavior with tactical placement, activity processing, and a duck the player can pick up and redirect. This combination is stated in the GDD; its final balance and exact rules remain open.

## Operating Context

`Assets/Scenes/Main.unity` is the first enabled Build Settings scene and contains
one continuous arcade session: a 10x10 maximum `BoardGrid`, one world unit per
cell, and a visible 5x5 opening board that expands to 7x7 and 10x10 during play.
`RunLoopController` provides Placement, Running, Paused, RunComplete and
Defeated states, a prototype timer, and reset hooks. Seal, Cat and Pufferfish
are the three combat members; Duck is an invulnerable station worker with a
rescue deadline. `TeamPlacementController` supports
Placement; `MainRunGameplaySystems` uses the same RunLoop clock for Process,
Duck automation, the three fighter combat roles and Ultimate actions, three
Enemy archetypes, field Items, Coins, shared Chips, ATK upgrades and Score. GDD gaps and
temporary prototype values are documented in `GAMEPLAY.md`.

The confirmed core session lasts five minutes. The Casino is present from the
beginning and persists through the run; Darts and Jump Rope remain eligible for
the 25% station roll every 60 seconds. Static pixel-art prefabs distinguish
Casino, Darts and Jump Rope, and the melee Roaster, ranged Toaster and Pan Shield
enemies.
The session ends with a final score and Play Again. Main saves all HUD and attack
VFX objects in its scene. Clicking a character opens a paused upgrade popup with
a small camera zoom. Fighter upgrades add 0.1 ATK and double their Coin price
per rank. Defeated enemies award 1-10 shared Chips. Duck has no HP/ATK and can be
kidnapped; Chips pay its rescue ransom before the countdown expires. Seal uses
the reference grid-cell attack masks, Cat a direct laser, and Pufferfish inflates, launches and bounces continuously,
reflecting from walls, allies and stations. Seal and Cat normal attacks use a 2-second base interval.

The latest gameplay HUD places Stations/Enemy on the left, Items on the right,
four character cards at the bottom, and Score/Grid Scale/Timer above a blue-framed
board viewport. Clicking a dead card revives that member for 10 Coins while the
run is unfinished. Revival restores full upgraded HP and places the member in
the nearest usable empty cell; these restoration details are prototype rules.
The wallet is visible at the top below Score/Grid Scale/Timer. Exchange above the right-hand
Items panel opens the two-way 1:1 Chips/Coins exchange.

`Assets/Scenes/Gameplay.unity` remains the playable duck + seal reference with
fixed casino and jump-rope activities, enemies, supplies, Switch relocation,
and win/lose/reset. See `GAMEPLAY.md` for both scenes, controls, Inspector fields,
and prototype scope.

## Capabilities and Constraints

- The GDD names three combat aliens: seal, pufferfish, and cat. A duck supports objectives and collects items.
- The intended loop is setup, start, autonomous activities and combat, player intervention, resource use, and a timer or team-elimination outcome.
- The GDD assigns Casino and Jump Rope to Seal and Darts to Pufferfish. In Main's current Casino prototype, only Duck can work the machine. Cat defends. The fourth activity is undecided.
- The confirmed prototype opens at 5x5 and expands to 7x7, then 10x10 in one continuous session. Future final sizes and balance remain adjustable.
- Enemy HP is stated as 10. Alien HP, attack cadence, damage, objective duration, enemy pathing, charge rates, shop timing/prices, upgrade and Chip effects, interruption rules, and score tick details are not specified and must be labeled as prototype assumptions.
- Casino x2/x4/x8 uses check/cross states: check earns Chips each tick, cross loses Chips until clicked. Process completion summarizes actual net without another payout. Rates are prototype settings.
- The final game platform, full market/chip rules, and which items are included in the first build are undecided.
- The GDD names Casino, Darts and Jump Rope but leaves the fourth activity and station type selection open. Main implements only the three named activities and uses a temporary uniform choice; no-cell Process spawns are skipped until the next roll.

## Brand Commitments

- Product name: Alien All In.
- The tone should be irreverent and never overly serious.
- Strategy should be engaging without becoming too easy or too difficult.
- The GDD calls for juicy satisfaction and visible feedback from progress and attacks.
- Casino and arcade activity imagery is part of the concept.

## Evidence on Hand

- User-provided GDD: [Alien All In Game Design](https://docs.google.com/document/d/1cEkx_ig7Z5lBK1WUKFDgsZKwTtKT6dMUzEAeKJm8BAQ/edit?tab=t.0).
- Playable reference: https://seal-the-deal-arcade-1030.toonender.chatgpt.site/
- The Unity scene uses simple geometric sprite placeholders, not final art.

## Product Principles

- Placement should change how the run unfolds.
- Aliens can make progress automatically, while the player still has useful moments to intervene.
- Objectives, combat, and rewards should produce clear feedback.
- Humor should coexist with real strategic choices.

The permanent Casino uses one confirmed stake per progress cycle. Bet and
multiplier lock on confirmation. At 100% it stops until the player clicks the
machine and confirms a new stake. Mid-round rebetting is blocked. Clicking a
broken machine fixes it while preserving the committed round. There is no
automatic repeat round. Only Duck works stations. All unfinished non-Casino
stations take priority: Duck clears them before returning to resume an interrupted
Casino round, keeping its stake, multiplier and progress. There is no extra
station requirement when none are pending.
