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

Alien All In is a grid-based idle strategy defense game. Players arrange a team of aliens, start the run, and support their autonomous attacks and activities while enemies interfere. Success means completing the stage's activities before the alien team is eliminated.

## Positioning

The game combines automated alien behavior with tactical placement, activity processing, and a duck the player can pick up and redirect. This combination is stated in the GDD; its final balance and exact rules remain open.

## Operating Context

The current request is an editable Unity gameplay scene with a 10x10 board,
one world unit per cell, a duck, reliable cell occupancy and a playable core loop.
`Assets/Scenes/Gameplay.unity` implements a duck + seal stage with fixed casino
and jump-rope activities, enemies, supplies, Switch relocation and win/lose/reset.
See `GAMEPLAY.md` for controls, Inspector fields, verification and scope.

## Capabilities and Constraints

- The GDD names three combat aliens: seal, pufferfish, and cat. A duck supports objectives and collects items.
- The intended loop is setup, start, autonomous activities and combat, player intervention, and stage victory or defeat.
- The named objectives are casino for duck, darts for pufferfish, and jump rope for seal. The GDD says a stage chooses two of four activities, but names only three; the fourth is undecided.
- The GDD's first page says 3x3 while later detail pages and the UI say 10x10. The user confirmed 10x10 for this prototype; the final game's board size remains open.
- Enemy HP is stated as 10. Alien HP, attack cadence, damage, objective duration, enemy pathing, shop prices, and other balance values are not specified and must be labeled as prototype assumptions.
- The gambling theme is present, but a betting mechanic is not yet specified.
- The final game platform, full market/chip rules, and which items are included in the first build are undecided.

## Brand Commitments

- Product name: Alien All In.
- The tone should be irreverent and never overly serious.
- Strategy should be engaging without becoming too easy or too difficult.
- The GDD calls for juicy satisfaction and visible feedback from progress and attacks.
- Casino and arcade activity imagery is part of the concept.

## Evidence on Hand

- User-provided GDD: `Alien All In.pdf` (attached to the Codex chat).
- Playable reference: https://seal-the-deal-arcade-1030.toonender.chatgpt.site/
- The Unity scene uses simple geometric sprite placeholders, not final art.

## Product Principles

- Placement should change how the run unfolds.
- Aliens can make progress automatically, while the player still has useful moments to intervene.
- Objectives, combat, and rewards should produce clear feedback.
- Humor should coexist with real strategic choices.
