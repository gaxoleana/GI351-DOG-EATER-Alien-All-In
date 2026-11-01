# Play the Unity core gameplay scene

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
