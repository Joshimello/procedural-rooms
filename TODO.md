# Student Exercises

Complete the four functions below in order. Each one builds on the previous.
Search for `TODO#1` through `TODO#4` in the code to jump directly to each exercise.

---

## TODO#1 — `ChooseSplitDirection` (`RoomLayoutBuilder.cs`)

**Concept:** How BSP decides which axis to split on — aspect ratio drives the decision.

Returns `true` for a vertical split (left/right), `false` for a horizontal split (top/bottom).

- Calculate the ratio of `rect.width / rect.height`
- If the rect is wide (ratio >= 1.25), split vertically → return `true`
- If the rect is tall (ratio <= 0.8), split horizontally → return `false`
- Otherwise, split randomly using `rng.NextDouble() > 0.5`

---

## TODO#2 — `FindSplittablePartition` (`RoomLayoutBuilder.cs`)

**Concept:** Filtering a list of rects to find candidates that are big enough to split, then picking one randomly.

Returns the index of a partition to split, or `-1` if none are large enough (which stops the BSP loop).

- Create an empty `List<int>` for candidate indices
- Loop through all partitions — a partition is splittable if `width >= minRoomSize.x * 2` OR `height >= minRoomSize.y * 2`
- If no candidates found, return `-1`
- Otherwise return `candidates[rng.Next(candidates.Count)]`

---

## TODO#3 — `AddFace` (`HouseMeshBuilder.cs`)

**Concept:** The atom of procedural mesh generation — defining one quad (4 vertices, 1 normal, 4 UVs, 2 triangles).

Every wall and floor in the project comes down to this function.

- Record `int index = vertices.Count` (triangle indices are relative to this face)
- Add the 4 corner vertices `v0, v1, v2, v3` to `vertices`
- Add `normal` 4 times to `normals` (one per vertex)
- Add UVs: `(0,0)`, `(1,0)`, `(1,1)`, `(0,1)`
- Add 6 triangle indices: `(0,1,2)` and `(0,2,3)` — all offset by `index`

---

## TODO#4 — `AppendBox` (`HouseMeshBuilder.cs`)

**Concept:** Composing 8 corners into 6 faces — the core building block every wall uses.

Builds directly on TODO#3. Compute the 8 corners of a box, then call `AddFace` once per side.

- Compute `Vector3 half = size * 0.5f`
- Compute 8 corners using `center + rotation * new Vector3(±half.x, ±half.y, ±half.z)`:
  - `p0` = (-x,-y,-z)  `p1` = (+x,-y,-z)  `p2` = (+x,-y,+z)  `p3` = (-x,-y,+z)  [bottom ring]
  - `p4` = (-x,+y,-z)  `p5` = (+x,+y,-z)  `p6` = (+x,+y,+z)  `p7` = (-x,+y,+z)  [top ring]
- Call `AddFace` 6 times:
  - Front (+Z): `p3, p2, p6, p7` — normal: `rotation * Vector3.forward`
  - Back  (-Z): `p1, p0, p4, p5` — normal: `rotation * Vector3.back`
  - Left  (-X): `p0, p3, p7, p4` — normal: `rotation * Vector3.left`
  - Right (+X): `p2, p1, p5, p6` — normal: `rotation * Vector3.right`
  - Top   (+Y): `p4, p7, p6, p5` — normal: `rotation * Vector3.up`
  - Bottom(-Y): `p0, p1, p2, p3` — normal: `rotation * Vector3.down`
