# Play the Unity core gameplay scene

## Stage 1 entry scene

`Assets/Scenes/Main.unity` is the first enabled Build Settings scene. It contains
the Stage 1 board shell: a 10x10 `BoardGrid`, with a 5x5 area open at 0% run
progress and locked cells visible in a darker color. `RunLoopController` starts
and pauses the run; `MainCoreGameplayUI` displays the timer/status and restarts the stage. It
is connected to the four-member Seal, Cat, Pufferfish, and Duck roster. The
team is editable in the RunLoop Inspector and appears under `Stage 1/Team`.
All four are active run members in Main; Cat/Pufferfish receive combat behavior
and Duck participates in team defeat checks.

`StageGridProgression` is the single Main-scene source for the prototype run
duration (300 seconds) and grid milestones. The GDD leaves the final duration
and expansion milestones open. The opening area is 5x5. Existing prototype
thresholds hold that area through 25%, open 7x7 at 50%, and the full 10x10 board
at 75%. The run loop reads progress from its one
elapsed-time clock. It exposes optional authored player start cells and the
currently open cells as potential Process spawn candidates.
The Stage start cells and RunLoop survivor roster contain the same four unique
open cells/members. The `TeamPlacementController` uses `BoardGrid` occupancy and
does not add colliders to each tile. During Placement, select a member card (or
press 1-4), then click an empty open cell to move it. Locked,
outside, and teammate-occupied cells are rejected. Space only starts once all
members are alive, inside the open grid, and in distinct cells.
`RegisterSurvivor` is deduplicated and Restart keeps the existing roster instead
of registering it again. Process completion is not a run-end condition.

The shared `GridActor` stores species, health/alive state, current grid cell, and
the run-start position. Restart restores the saved formation and calls
`ResetActor`, which returns each Animator to Idle. Each species has Idle, Attack,
and Ultimate clips at 8 FPS. Idle loops; Attack and Ultimate return to Idle after
one play. Duck's Attack clip is retained on its animation driver as a supplemental
asset, but the Duck controller has no Attack state and `PlayAttack` rejects Duck.
Processing currently remains on Idle until a Process animation is specified.
The sprite sheets use Point filtering, 362 PPU (one 362-pixel frame per one-unit
cell), no mipmaps, and uncompressed textures. The source files currently live
under `Assets/AlienAllIn/Art/Sprites/` in this project.

Space starts from Placement and pauses/resumes while running; R restarts. Pause
and terminal states set Unity's scaled game time to zero. Restart restores the
saved starting positions, HP/state/Idle animation and any optional activities or
pickups wired to the controller, then returns the grid to its opening area. The
final-frame tie rule checks team elimination before timeout, matching the
existing Gameplay prototype's outcome order. Main's shared GridActor HP is 30
per member as a prototype value; the GDD does not set Alien HP.

## Main vertical slice

`MainRunGameplaySystems` extends the existing Stage and Run Loop. The Run Loop
is the only owner of run time and outcomes; gameplay systems advance only with
`GameplayDeltaTime`. Pause freezes movement, spawn clocks, projectiles, attacks,
charge, Process, Cash, Score and shop restock. Restart removes runtime stations,
pickups, enemies and projectiles, clears inventories/Cash/Score, resets shop
stock, restores the four saved positions, HP/upgrades and Idle animations, then
returns Stage progression to its starting area. RunComplete comes only from the
timer. Defeated checks all four active members, including Duck. If the last
member dies on the timeout frame, Defeated wins because the existing Run Loop
resolves a team wipe before its timed result.

Main starts in Placement with all four members on distinct cells in the open
5x5 area. Select a card and click an open cell to arrange the roster. During a
run, fighter cards can be dragged to an open cell only with a full personal
Ultimate gauge; the move consumes that gauge. Duck may be dragged only while
Panic, Hiding, Working, or recently hit. A Duck drop next to an Alien and behind
it relative to the nearest threat marks it Hiding; this geometric reading of
“behind” is a prototype rule. Space starts/pauses/resumes; R restarts.

The field starts with no Process station and no carried Items. Every 60 run
seconds, the Inspector-defined 25% roll may create Casino, Darts, or Jump Rope
in a currently open valid cell. The three activities are selected uniformly at
random as a temporary rule; the GDD names no fourth activity and does not define
selection. A station cannot overlap an Alien, Item or another station. An
Enemy on its cell is crushed. If no valid cell exists, the roll is skipped until
the next interval, a temporary choice because the GDD leaves this case open.
There is no separate station-count cap beyond available cells.

The existing `ActivityStation` handles Process. Prototype durations are Casino
36s, Darts 32s and Jump Rope 40s at owner rate. Duck can work any station at
1.10x rate and receives another +15% while Overdrive is active. Each 1 second of
active work pays +1 Cash; every 2 seconds adds +5 Score; completion adds +100
Score, increments Processed once and removes the station. These tick timings are
prototype values. Enemy hits preserve progress and pause work for 2 seconds.
Stations have no HP. The GDD has not set these interruption and tick rules.

Duck prioritizes field Items over available Process and enters Panic while an
Enemy targets it. It automatically walks to Items and stations; the player can
drag it behind a teammate to hide. Items spawn after 8 seconds and every 28
seconds, with a four-item field cap; these are adjustable prototype values.
Duck's gauge gains +0.15 per pickup, +0.025 per second while processing and
+0.04 per second in Panic. A full gauge automatically starts a 7-second
Overdrive with 1.35x movement speed. During Overdrive, Duck ignores Panic and
continues Process through hits while still taking damage. Duck has no Attack
gameplay and its Attack clip is never called.

Seal performs AOE normal attacks with knockback; Cat hits one Enemy and stuns
it; Pufferfish inflates, floats randomly, bounces from the board, Enemies and
stations and teammates, and deals reduced contact damage with a repeat-hit cooldown.
Pufferfish can be picked up only while floating and with a full Ultimate gauge.
Fighter gauges charge from damage dealt; one full gauge is 30 damage. Matching Items
require a full gauge, consume both Item and gauge, then activate that fighter's
Ultimate. Seal's expanding ripple loses damage with distance; Cat emits eight
rotating laser spokes; Pufferfish's Ultimate increases float speed and contact
damage. Ultimate Switch requires two full fighter gauges, swaps their positions
and activates both Ultimates. Timing, range, cadence and damage are Inspector
prototypes.

The shared Enemy prefab is configured at runtime as one of three archetypes,
each with 10 HP. Roaster (melee) approaches the team, retreats from its nearest
threat at 20% HP or less, then takes a prototype 0.75s bite to restore +5 HP once.
Toaster (ranged) keeps distance from fighters except Duck and fires target-following
projectiles. Pan advances more slowly, reduces incoming damage by 20%, and uses a
short-range fry AOE that also interrupts nearby Process stations. Prototype spawn
settings are 7s initial delay, 4.5s interval and eight live Enemies; per-type
weights, speed, range, damage and cooldown are editable. Pan's always-on damage
reduction, Toaster's preferred range, and Roaster's eat/retreat timing are
temporary interpretations of the GDD.

Cash and Score are separate. Cash upgrades Damage, cast speed and HP. The Chip
Shop sells Seal, Cat and Pufferfish Chips; bought Chips enter inventory and
must be dragged onto the matching fighter. Upgrade/Chip prices and effects,
stock capacity and 90s restock are prototype values. The basic UI shows time,
run state, team HP/gauges, Process and Enemy HP, Item/Chip inventory, Cash and
Score. GDD score values are Hit +10, Processing +5 per prototype tick, Processed
+100, normal Kill +50 and Ultimate Kill +100. Ultimate Kill is exclusive to
normal Kill in this prototype; the GDD leaves additive versus replacement open.
No Kill Count is shown. The GDD's fourth Process type remains unspecified and
is intentionally absent. Charge, drop, duration, economy and interruption
details that the GDD leaves open remain Inspector prototypes for review.

`Assets/Scenes/Gameplay.unity` remains the playable duck + seal reference scene
described below; it was kept separate from the Stage 1 entry scene.

Open `Assets/Scenes/Gameplay.unity` in Unity **6000.5.10f1** and press Play.
The board, all 100 cells, duck, seal, two activity stations, supply, HUD and
references are saved scene objects. They are visible and editable before Play.
In this reference scene, the only dynamically instantiated objects are enemies,
from `Enemy.prefab`. The shared combat and spawn helpers retain its existing
behavior; Main uses them under the separate RunLoop clock.
The board uses sprites without text labels. Activity progress and selected-unit
health are shown in the sidebar; cell coordinates and occupancy remain in the Inspector.

## Controls

- Click a team member (or its DUCK/SEAL button), then an empty cell.
- Before starting, this places the character at that cell's center.
- **Space / START SHIFT:** begin. Space then pauses/resumes the running shift.
- During a shift, the duck walks to a requested cell and then resumes automation.
  It cannot be redirected while fleeing or taking a hit; hiding is allowed.
- The seal earns Switch by dealing damage. At 100%, select it and click an
  empty cell to reposition and release an immediate area attack.
- **T / TRAMPOLINE:** consume a duck-collected supply to empower the next seal
  slam, increasing its area and damage with slower recovery. No free reuse.
- **R / RESTART:** reset the entire stage to the authored starting positions.
- **Escape:** deselect.

## What this proves

The duck automatically collects the trampoline, then walks to the casino and
generates cash as it works. Enemies spawn around the edges and pursue the team.
The seal automatically slams nearby enemies and pushes survivors back. The duck
flees nearby enemies, using the seal as shelter, and resumes work when safe.
The seal also completes jump rope while stationed on its matching activity.
Both activities reaching 100% wins; losing both characters loses. Activity work
pauses if an enemy is within one world unit of the station. Pause freezes the
simulation; restart restores positions, health, supplies, enemies and progress.

The default arrangement is intentionally forgiving and can win without input.
Move the seal away from jump rope before starting to experiment with placement;
it will need to return before that objective can finish. A full Switch is needed
to move the seal during combat.

## Inspect and edit

| Hierarchy / asset | Useful fields |
|---|---|
| Board / Cell A1 ... J10 | Coordinate, surface color, live Occupants list |
| Team / Duck | Starting Transform, health, move speed; live Current Cell, State |
| Team / Seal | Starting Transform, health, attack range/damage/cadence; Switch |
| Activities / CASINO, JUMP ROPE | Position, required species, duration, live progress |
| Pickups / Trampoline supply | Position and quantity |
| Gameplay | Spawn delay/interval, enemy limit, fear/safety radii, knockback |
| `Assets/AlienAllIn/Prefabs/Enemy.prefab` | Enemy HP (10), speed, damage and cadence |
| HUD | Normal Canvas, Text and Button objects |
| Team / Art | Individual sprite shapes, positions, sizes and colors |

Changes made outside Play mode persist when you save the scene. Play-mode edits
are temporary, following normal Unity behavior. Move character and station roots,
not their Art children, to change placement. Snap roots to cell centers.

The fixed board spans **(-5,-5) to (5,5)** in XY. A1's center is (-4.5,-4.5);
J10's center is (4.5,4.5). Every logical cell is **1 x 1 world unit**; the drawn
.96-unit squares leave a small visible gutter. Keep the board scale at one and
cell size/dimensions at their authored defaults for this stage.

`BoardGrid.TryWorldToCell` uses `InverseTransformPoint` and `FloorToInt`. It
supports a translated/rotated board, rejects outside positions and uses half-open
cell bounds. `BoardGrid.RefreshOccupancy` rebuilds each cell's occupant list from
actual actor centers, including enemies. A duck can cross occupied cells.
This avoids needing 100 colliders, rigidbodies and trigger enter/exit events.
Combat uses explicit world-space distance. Select Board or Seal in Scene view
to see grid/range gizmos. Occupancy and inspector values refresh during play.

## Scope and prototype assumptions

`Gameplay.unity` remains a separate Duck + Seal reference scene with its own
prototype loop and objectives. Main's broader systems are described above and
do not overwrite that reference scene. Both use simple geometric/pixel-art
placeholders rather than final art, and no real-money betting is implemented.

## Verification

Unity Test Runner includes EditMode tests for the authored scene, boundary and
transformed-grid lookup, occupancy, placement rejection, collection, a complete
winning run with enemy kills, fleeing, pause/reset, loss, consumables, Switch and
species matching. PlayMode tests load the saved scene and exercise actual HUD
listeners, runtime simulation through victory/restart, duck movement and pause.
The simulation is fast-forwarded using the same `Advance` method called by Update.

Run from the Unity Test Runner, or launch the installed editor with
`-batchmode -projectPath <project> -runTests -testPlatform EditMode`
and then `-testPlatform PlayMode`, specifying `-testResults <path>` each time.

`Alien All In > Rebuild Gameplay scene` is an optional authoring tool. It **replaces
Gameplay.unity**, so save a copy before using it. It is never invoked on import
or Play. Normal scene editing does not require rebuilding.
