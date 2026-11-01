# Play the Unity core gameplay scene

## Stage 1 entry scene

`Assets/Scenes/Main.unity` is the first enabled Build Settings scene. It contains
the Stage 1 board shell: a 10x10 `BoardGrid`, with a 5x5 area open at 0% run
progress and locked cells visible in a darker color. `RunLoopController` starts
and pauses the run, shows a basic timer/status panel, and restarts the stage. It
is connected to the four-member Seal, Cat, Pufferfish, and Duck roster. The
team is editable in the RunLoop Inspector and appears under `Stage 1/Team`.
Process machines, combat, and resources are not part of this scene yet.

`StageGridProgression` is the single Main-scene source for the prototype run
duration (300 seconds) and grid milestones. The GDD leaves the final duration
and expansion milestones open. The opening area is 5x5. Existing prototype
thresholds hold that area through 25%, open 7x7 at 50%, and the full 10x10 board
at 75%. The run loop reads progress from its one
elapsed-time clock. It exposes optional authored player start cells and the
currently open cells as potential Process spawn candidates. Candidates do not
spawn machines, choose their type, or filter occupied cells.
The Stage start cells and RunLoop survivor roster contain the same four unique
open cells/members. The `TeamPlacementController` uses `BoardGrid` occupancy and
does not add colliders to each tile. During Placement, select a member from the
roster panel (or press 1-4), then click an empty open cell to move it. Locked,
outside, and teammate-occupied cells are rejected. Space only starts once all
members are alive, inside the open grid, and in distinct cells. All four members,
including Duck, count toward a team wipe for this prototype. `RegisterSurvivor`
is deduplicated and Restart keeps the existing roster instead of registering it
again. Process completion is not a run-end condition.

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

`Assets/Scenes/Gameplay.unity` remains the playable duck + seal reference scene
described below; it was kept separate from the Stage 1 entry scene.

Open `Assets/Scenes/Gameplay.unity` in Unity **6000.5.10f1** and press Play.
The board, all 100 cells, duck, seal, two activity stations, supply, HUD and
references are saved scene objects. They are visible and editable before Play.
The only dynamically instantiated objects are enemies, from `Enemy.prefab`.
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

This is a duck + seal gameplay slice with simple Unity sprite assets, not full
demo parity. Cat, pufferfish, random stages, the chip shop, upgrades, paired
Switch swaps, cinematic camera effects and final art/audio are not implemented.
All tuning values other than enemy 10 HP are prototype choices. Casino/jump rope
are fixed for repeatable testing. Trampoline boosts one attack; Switch relocation
costs 30 damage worth of charge. No real-money betting is implemented.

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
