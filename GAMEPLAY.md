# Play the Unity core gameplay scene

## Main arcade session

Open `Assets/Scenes/Main.unity` in Unity **6000.5.10f1** and press Play. Main is
the first build scene. One session lasts five minutes: a 5x5 opening board grows
to 7x7 after 150 seconds and 10x10 after 225 seconds. Each cell is one world unit.
The permanent Casino is present when Main loads and stays on the board across
progress cycles and Restart. Other stations start empty; every 60 seconds a 25%
roll may create Darts or Jump Rope. Stations and enemies use distinct pixel-art
prefabs.

All HUD objects are **saved in Main**, including the header, roster, Items,
station status, character popup, result screen and O-key tools. Runtime code
binds the references and updates values; it creates no HUD objects. Edit the
`Main Core Gameplay UI` Canvas under the UI controller. Enable its inactive
`Character Popup` child to preview and edit it before Play; startup hides it.
The inactive `Debug Tools (O)` Canvas is saved beside it. `Attack VFX` under the
gameplay systems contains the Cat laser. `Board Feedback` contains editable hover
and drag outlines plus the Seal tile highlight colors. Text, colors,
widths, anchors and layouts are normal Inspector properties. MainSceneLayout
on Main Camera controls reserved HUD regions and selection zoom (.88 default).

- **Space** starts and pauses/resumes. **R** restarts. Header buttons offer Start,
  Pause and Options; Options contains Resume and Restart. Coins/Chips are shown
  directly below Score/Grid Scale/Timer; Exchange is above the right-hand Items panel.
- The bottom status bar contains all four characters. Click a living character
  to inspect it; click a dead character to revive it for **10 Coins** during an
  unfinished run. Revival restores full upgraded HP, preserves purchased ATK,
  clears Ultimate/attack state and resumes Idle animation. It uses the nearest
  unlocked empty cell to the death position, rejecting occupied/reserved/station
  cells. If no cell is available, it spends nothing. These restoration/location
  rules are prototype assumptions; the 10-Coin price comes from the UI reference.
- Click a character on the board to open its popup. Play pauses and
  the camera zooms in slightly. **X**, outside click or **Escape** closes it.
  Closing resumes only if opening the popup caused the pause.
- Drag a character on the board onto an open cell or matching
  station. Placement snaps; during play it walks there without spending charge.
  Seal works Jump Rope, Pufferfish works Darts, Duck works Casino and other
  stations, and Cat defends. Occupied/outside/incompatible cells are rejected.

## Coins, Chips and ATK upgrades

Working at stations pays Coins (+1 per prototype work tick). A fighter's popup
has one Upgrade button: **+0.1 ATK**, initially **10 Coins**, then **20, 40, 80...**
independently for each fighter. HP and attack cadence do not change. Edit the
initial price and increment on MainRunGameplaySystems. Duck has no upgrade,
HP or ATK display. Defeated enemies award **1-10 shared Chips**, immediately
collected once per enemy. Coins/Chips are separate from Score. The species Chip
inventories and Chip Shop have been removed. Items and Ultimate gauges remain.

## Casino machine

Permanent Casino: choose x2/x4/x8 and pay the initial stake. Only Duck works it.
Check earns Chips; cross loses Chips until the player clicks the machine to
restore check for free. This replaces the old 20-Coin malfunction repair rule.
Prototype defaults: tick every 1 second (gain and loss both bet x multiplier), and
cross roll every 3 staffed seconds at 5%/10%/20% for x2/x4/x8. Cross persists
across Process cycles until clicked. Rates and intervals are Inspector settings.
Chip balance cannot fall below zero. Rebet spends another stake and resets Progress.
Each completed cycle summarizes actual net change, including initial/rebet stake;
there is no random final payout, duplicate award or automatic next stake charge.
Normal work bounces with a check thought bubble; cross uses a red cross bubble.
Actual green signed Chip ticks float above-right, fading in/up/out without a panel.
Final net counts up with K/M/B abbreviations and multiplier; a positive summary
flies onto the HUD Chips line. Pause freezes gameplay and visuals. Restart clears
Casino state and effects. All rates remain prototype balance.
## Duck rescue

Duck cannot take HP damage. An enemy reaching and hitting it captures it instead.
Its popup shows **Free the Duck**, a **5-Chip** ransom and a **15-second** active
play deadline. Paying or killing its captor frees Duck; expiration ends the
session. A three-second grace period prevents immediate recapture. These values
are editable on MainRunGameplaySystems. Captured Duck cannot be moved. The
popup pauses the rescue clock with gameplay. Losing all three fighters also
ends the session; Duck is excluded from the fighter HP defeat count. Restart
clears rescue state, Coins, Chips, upgrades, temporary spawned objects and debug
overrides. It resets the permanent Casino but leaves it on the board.

## Attacks

Seal highlights its affected grid cells before the slam (.16-second telegraph).
Normal range is a 3x3 square (9 cells including center);
enhanced range follows 3/5/5/5/3 (21 cells). Cells beyond the active board are
clipped. Damage uses the same cell masks, including enemies moving within a cell. Cat draws a brief straight laser to its hit target.
Seal uses tile highlights; Cat uses a saved LineRenderer. Both freeze while paused. Pufferfish adds no VFX:
**stationary -> inflate -> launch -> continuous bouncing**. It initially aims at
a nearby enemy, or uses a diagonal direction when no target is available.
Enemy, ally, Casino/other station and active-board wall contacts reflect movement;
allies and stations take no collision damage. It keeps bouncing beyond two hits.
Manual movement and Processing can interrupt it; Restart clears its bounce state.
Seal and Cat normal attacks use a 2-second base interval. Duck still has no attack;
Pufferfish keeps its separate inflation/contact attack behavior. Its base speed is 1.12 cells/second (44% below the previous 2). While bouncing it slowly sways +/-22 degrees at 0.25 cycles/second. Basketball Sound.mp3 supplies a mono PCM impact trimmed from original 0.60 to 1.00 seconds, removing the initial silence (PufferBounceImpact.wav). The clip preloads and plays from the Pufferfish position with 3D linear attenuation, 40% volume and no Doppler; no collision starts no sound. Pause freezes sway and pauses sound; Restart restores rotation and clears sound. Speed, sway, volume, clip offset and segment length are Inspector settings.

## O-key debug tools

**O** opens/closes the saved debug Canvas in Editor/Development Builds; Escape
and Close also close it. It works in Placement and Pause and blocks board input
underneath. Scroll for all controls: spawn specific/random enemies or stations;
spawn five; kill for rewards or clear without rewards; expand/reduce or set
5x5/7x7/10x10/Auto; add/remove time; grant Coins/Chips/Items/Score; heal/revive;
fill Ultimates; freeze the clock; disable automatic enemy/station spawning.

Shrinking keeps allies in distinct active cells and removes off-board objects.
Clear/off-board removal grants no kill Score or Chips. Timer edits change time
remaining without skipping elapsed gameplay or spawn clocks. Restart resets
overrides. To expose O in a release build, enable `Enable Debug In Release Build`
on MainCoreGameplayUI.

The timing, work rewards, ransom, combat and upgrade balance above are prototype
tuning. These user-directed mechanics supersede the older HP-Duck, random-float
Pufferfish and three-upgrade/Chip-Shop rules. The explicit Editor menu
`Alien All In > Author Main scene HUD and attack visuals` rebuilds and saves the
named HUD/VFX children. It is never invoked automatically by Play or import.
Normal Inspector editing does not require rebuilding.

## Legacy reference scene

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


## Reference HUD and pointer feedback

The main HUD follows the latest reference inside its 992x558 game rectangle:
centered Score/Grid Scale/Timer, Start/Pause/Options at the top right,
Stations/Enemy on the left, three vertical Items on the right, and four compact
character cards at the bottom. A two-pixel blue frame outlines the board viewport.
All regions are saved scene objects; `ReferenceImageLayoutAuthoring` can reapply
this layout without rebuilding gameplay. The full HUD authoring command also
applies it. Coins/Chips stay visible at the top below Score/Grid Scale/Timer. The Exchange
button above Items opens the two-way 1:1 shop and pauses the run while open.
Keyboard O retains the debug tools. Hover a board character for
an outline. Drag a character to see the destination cell highlighted green when
valid and red when blocked; leaving the board or ending the drag clears it.
Seal's Inspector gizmo also uses the normal grid-cell mask in Main.
