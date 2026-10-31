# Alien All In — Topic 1: Project survey

Survey date: 2026-10-31 (Asia/Bangkok). Baseline commit: `ec68b5e`.
Status: repository survey complete; no playable gameplay implemented or validated in this topic.

## Instructions and evidence

- Workspace: the root of this Unity repository.
- No repository `AGENTS.md` was found in the file inventory. Follow the user-supplied AGENTS instructions: communicate candidly, flag conflicting evidence, and do not invent uncertain details.
- Read the user-provided six-page `Alien All In.pdf` with pypdf. This was text extraction, not visual verification. Screen reference attachments are not available in this chat's accessible input; do not claim to have compared their appearance.
- `PRODUCT.md` describes the retired HTML prototype. Its statement that the final board size is open is superseded by the user's explicit 10×10 confirmation. Do not use the HTML prototype as Unity architecture or final design specification.

## Confirmed environment

| Area | Evidence and conclusion |
| --- | --- |
| Editor | `ProjectSettings/ProjectVersion.txt`: 6000.5.10f1, revision 3bd4f66ad299. Live CLI also reported this Editor ready for this exact project. |
| Render pipeline | URP 2D template assets; Quality Settings reference a pipeline asset. **Version conflict:** manifest requests 17.6.0; lockfile resolves URP/Core/Shader Graph 17.5.0 as built-in packages. Actual registered Editor package version still needs verification; do not silently change dependencies. |
| Input | Input System 1.20.0 in manifest and lock; `activeInputHandler: 1` enables new Input System. |
| Input asset | `Assets/Settings/InputSystem_Actions.inputactions`: Player and UI maps, keyboard/mouse, gamepad, touch, joystick and XR bindings. No first-party gameplay consumer at survey baseline. Device support is not proven merely by bindings. |
| UI | uGUI 2.5.0 installed. No first-party Canvas, UIDocument, UXML/USS or gameplay UI found at baseline. Use a small uGUI presentation layer for the proposed slice unless later requirements change this. |
| Tests | Unity Test Framework 1.7.0 installed. No first-party test suites or test assemblies found. No tests/builds run for this documentation-only topic. |
| Tooling | Unity CLI 1.0.0-beta.15; `com.unity.pipeline` 0.9.1-exp.1. Read-only CLI status with local Editor access returned one Editor, state ready. Restricted commands reported unreachable, so that result alone does not prove Editor failure. The existing separate AB Unity MCP connector had previously found no instances. |
| Networking | No gameplay networking implementation found. Multiplayer Center being installed is not evidence of multiplayer gameplay. |

## Scenes, files and architecture at baseline

- `Assets/Scenes/SampleScene.unity` is the only enabled Build Settings scene. Its named root objects are Main Camera and Global Light 2D; no gameplay startup flow exists in the saved scene.
- `Assets/Settings/Scenes/URP2DSceneTemplate.unity` is an additional template scene, not an enabled game scene.
- Original first-party folders: `Assets/Scenes` and `Assets/Settings`. No existing gameplay scripts, prefabs, assembly definitions or established game architecture were found before this task.
- The live Editor previously displayed an Untitled scene. Do not replace or save the user's current scene to install the slice. Create a separate authored scene later and preserve unsaved work.
- No code convention can be inferred from existing first-party scripts because there were none. Proposed convention for new code: `AlienAllIn` namespace, explicit references, serialized configuration, plain C# rules separated from Unity lifecycle and UI, no global manager framework.

## Design facts confirmed by user and GDD text

- Board is **10×10**. The GDD cover says 3×3, but page 2 and the user's explicit decision supersede it.
- Combat species: Seal/Bouncer, Pufferfish/Puff Daddy, Cat/Meow-Tron. Duck/Quack Ops collects drops and performs objectives.
- Named activities: Duck Casino, Pufferfish Darts, Seal Jump Rope. GDD says two activities per stage selected from four, but names only three. Reserve extensible activity definitions; do not invent the fourth.
- Loop: placement/assignment → Start Shift → automatic work and combat → enemies disrupt → player supports → complete all objectives or lose the team.
- Enemy HP 10 is stated on page 3. Alien HP 40 and duck HP 24 are reported by the user from screen references, not visually verified here. Damage, cast times, ranges, spawn cadence and activity duration remain provisional.
- Items are consumable pickups, with starting inventory zero. They are not freely reusable cooldown powers. Duck must collect Needle, Laser and Trampoline drops.
- Duck flees enemies and shelters behind friends, can cross reserved tiles, and cannot be grabbed while fleeing or being attacked. Safe idle/working/hiding states permit grabbing.
- Seal base attack is local AoE; Trampoline expands AoE with slower cast and knockback. Pufferfish base behavior is a straight-line out-and-back dash; Needle emits eight directions and temporarily removes it. Cat has targeted base attacks; Laser attacks a grid line. Exact damage geometry/timing needs clarification.
- Switch is earned by dealing damage and enables special activations, including swapping allies. Full combo/cutscene implementation is outside the proposed week-one core slice, pending scope confirmation in Topic 2.
- Shop, Chips, cash upgrades, complete content, final art and final balance are outside the requested week-one implementation.

## Open questions and constraints

1. Original screen-reference images need to be provided again for visual/HP/DMG/CAST comparison. GDD extraction alone does not verify their layout.
2. Which entities count toward elimination: all four including duck, or only the three combat aliens? GDD says all aliens on the field; expose this decision explicitly in the slice specification.
3. How exactly do enemies interfere: pause progress, regress it, attack workers, or damage activities? No precise rate/rule is defined. A pause-near-enemy rule would be a documented provisional choice.
4. Final attack ranges, cast times, activity durations, enemy movement/target priority, spawn cadence and drop distribution are unspecified.
5. The fourth activity, Switch thresholds, exact Laser direction, and final target platform remain unresolved. Do not introduce replacement content as fact.
6. Reconcile manifest URP 17.6.0 vs resolved built-in 17.5.0 through registered Editor package evidence before any package migration.

## Working tree preservation and delivery boundary

Pre-existing changes observed before implementation: URP global settings, package manifest/lock from MCP setup, EditorBuildSettings, ProjectAuditorSettings, ShaderGraphSettings, untracked `.vsconfig` and `PRODUCT.md`. These are not part of the Topic 1 commit.

Before the user requested sequential topic delivery, two unintegrated draft runtime files were started under `Assets/AlienAllIn/Runtime`: `ShiftConfig.cs` and `ShiftSimulation.cs`. They have no scene entry point, UI or completed runtime validation. They remain local work in progress and are deliberately excluded from this documentation commit. Their placeholder rules are not approved GDD decisions; review them against the Topic 2 contract before completing implementation.

Next delivery: Topic 2, agree and document the smallest vertical slice and system ownership; Topic 3, the five-day sequence; then implement features one topic at a time with scoped commits and pushes.

## Files inspected

`ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/EditorBuildSettings.asset`, `ProjectSettings/QualitySettings.asset`, `ProjectSettings/GraphicsSettings.asset`, `Assets/Scenes/SampleScene.unity`, `Assets/Settings/InputSystem_Actions.inputactions`, `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`, `.gitignore`, `PRODUCT.md`, and the local GDD PDF.
