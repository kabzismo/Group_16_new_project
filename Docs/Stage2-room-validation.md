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
