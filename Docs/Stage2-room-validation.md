# Stage 2 room changes — 4 November 2026

Ice now uses five alternating ground-level switchbacks, eight slalom obstacles and a final chicane. The elevated crate/pillar routes are removed and the progression key is back at floor level. Wall contact discards momentum into the wall while retaining sliding along it.

Moving platforms, timed platforms, the enemy and gravity hazards no longer require completing entrance hints. Reading an open hint still pauses these mechanics. Moving platform colliders synchronize before the controller's support queries.

The requested Fire reference is `c670e87141bd94f849926090ed1d4f6c725b7b27`. All 123 objects and their components under `Hot Room - Staggered Lava Course` already matched that revision exactly. That course was preserved. No Fire layout restoration was necessary for this checkout.

Validated in Unity 6000.3.20f1 Play mode using temporary editor checks:

- 687 player-sized capsule samples along the Ice entry/key/exit route: clear, with continuous flat floor. Collectible key collision excluded because the key is collected before passing its position.
- Moving platform displacement before any hint was read.
- Timed platform collapse removed collision after its 1.6-second timer; collision restored after four seconds.
- Enemy present with valid navigation at scene start, before hints.
- Enemy followed a path toward the player in the maze, moving 5.15 metres during the three-second chase check.
- Enemy catch returned the player to the saved Door .002 checkpoint, approximately `(49.33, -3.57, 65.93)`.

The route clearance check is not a full manual playthrough or a difficulty assessment. A human playthrough remains useful for tuning the new sliding course. Existing standalone builds must be rebuilt to include these source and scene changes.

## Difficulty expansion — 6 November 2026

- High Gravity: closed five shortcut openings, retaining three alternate loops. The shortest entrance-to-key path increases from 19 to 23 cells; all 45 cells remain connected.
- Ice: added eight obstacles (four extra slalom fingers and four entry/exit islands). Extended the slalom fingers into the lanes so players must weave around their tips. The progression route remains entirely on the floor.
- Hot: replaced the old route with 27 main landings, repeated hairpin turns, smaller precision tops, a higher key summit and a seven-landing descent. Removed the obsolete shortcuts. Shifted the exit frame and final landing to keep the final jump clear of support columns.
- Preserved all moving/timed platform hierarchies, transforms and serialized component values; compared before and after rebuilding. No runtime movement or platform scripts changed.

Unity Play mode validation passed: 760 player-capsule floor-clearance samples in Ice, 589 in the maze (excluding collectible keys and operable doors), complete enemy NavMesh paths to all 45 maze cells, and collision-free sampled ballistic arcs for all 26 Hot-course jumps using the current controller's jump height, gravity, speed and capsule dimensions. Reviewed rendered overhead views of all three rooms. These geometric checks do not replace a full manual difficulty playthrough.

## Dialogue restoration and fourth-key completion — 9 November 2026

Restored all four room hint arrays verbatim from `e568138^`, before the layout-tool dialogue edits. Removed hint-writing code from the Ice and gravity layout tools so rebuilding geometry preserves authored dialogue.

The fourth distinct room key now starts completion immediately even when the exit table exists. The completion heading reads `GAME ENDED`; the existing four-second shutdown countdown remains.

Unity 6000.3.20f1 Play-mode regression passed: early exit interaction remains locked, duplicate claims are rejected, the first three keys do not end the game, and an actual E-input pickup of the fourth key starts completion, removes the key, displays the screen, disables controls, and clears the interaction prompt. Play mode stopped after the countdown (4.6 seconds observed). The regression component was moved out of the Editor folder and guarded with UNITY_EDITOR so Unity can attach it during testing without including it in player builds. This is a targeted regression, not a full room playthrough. Existing standalone builds require rebuilding.

## More visible low-gravity movement — 9 November 2026

Expanded moving ledges from 3 to 7: Low 02, 07, 09, 12, 14, 18 and 24. Horizontal amplitude is now 1.8 m each side (3.6 m total sweep), previously 0.55 m (1.1 m sweep). A six-second cycle makes the wider movement readable. Rest stops, the summit, tall pillars and collapsing crates remain stationary. Saved both scene components and layout-tool configuration; dialogue is unchanged.

Unity Play-mode sampling over 7.5 seconds measured 3.60 m travel on all seven ledges, checked their colliders remain within the room's horizontal bounds, and confirmed downward landing rays hit the moving colliders. All 25 jumps have center-to-center ballistic reach windows when sampling 600 launch phases with the current player speed (5 m/s), low-gravity acceleration (14.4 m/s²), and jump height (4.05 m). This range calculation includes target motion during flight, but is not a full collision-path or manual difficulty playthrough.
