# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Unity procedural room generation system using Binary Space Partitioning (BSP). Requires Unity 2018.4 LTS or newer (Unity 6 recommended).

## Development

This is a Unity project — there are no CLI build/test commands. All development happens inside the Unity Editor:

- Open the project in Unity Hub via "Add project from disk"
- The `House` component auto-rebuilds in Edit Mode whenever Inspector parameters change (`[ExecuteAlways]` + `OnValidate`)
- To test: add `House.cs` to a GameObject in any scene and adjust parameters in the Inspector

## Architecture

All scripts are in `Assets/Scripts/`. The system has a clear separation of concerns:

### Data Flow
```
House.cs (MonoBehaviour)
  → RoomLayoutBuilder.GenerateBspLayout()   → LayoutResult (rooms + wall segments)
  → HouseMeshBuilder.BuildHouseShell()      → outer shell Mesh
  → HouseMeshBuilder.BuildInteriorWalls()   → interior walls Mesh
  → House.BuildRoomPrefabs()                → instantiates furniture prefabs
```

### Script Roles

- **`House.cs`** — The only MonoBehaviour. Orchestrates the full rebuild pipeline, manages child GameObjects (`HouseShell`, `InteriorWalls`, `Rooms`), handles furniture placement logic (wall alignment, bounds clamping, floor adjustment), and exposes all Inspector parameters.

- **`RoomLayoutBuilder.cs`** — Pure static utility. Runs BSP on the house footprint: recursively splits a `Rect` into partitions, outputs `List<RoomSpec>` (room bounds/centers) and `List<WallSegment>` (interior divider segments). Coordinate space: `Vector2` maps to `(x, z)`.

- **`HouseMeshBuilder.cs`** — Pure static utility. Builds procedural meshes by composing axis-aligned boxes (`AppendBox`). Handles both the house shell (floor slab + 4 outer walls) and interior wall segments (rotated boxes along each divider).

- **`RoomMeshBuilder.cs`** — Pure static utility. Builds meshes for individual rooms or batches of rooms. Largely mirrors `HouseMeshBuilder`'s box-building approach for per-room geometry.

### Room Types & Furniture

`House.cs` randomly assigns each BSP partition one of three `RoomType` values: `LivingRoom`, `Bedroom`, `Toilet`. Furniture prefabs are assigned via Inspector fields and placed using wall-inset calculations with bounds-clamping to avoid clipping.

### Key Design Decisions

- All mesh geometry is built into reused `Mesh` objects (cleared and repopulated on each rebuild) — no mesh asset creation.
- `[ExecuteAlways]` + `EditorApplication.delayCall` debouncing prevents multiple rebuilds per `OnValidate` call in Edit Mode.
- `RoomLayoutBuilder` and `HouseMeshBuilder` are `static` classes with no Unity lifecycle — safe to call from anywhere.
- Interior wall `WallSegment` types are duplicated between `RoomLayoutBuilder` and `HouseMeshBuilder`; `House.cs` manually converts between them.

## Assets

- `Assets/Furnitures/Prefabs/` — furniture prefabs (bed, closet, chair, table, bathtub, carpet)
- `Assets/Furnitures/Meshes/` — FBX source meshes
- `Assets/Furnitures/Materials/` — shared `Color.mat` material using a single texture atlas
- `Assets/Scenes/SampleScene.unity` — the main scene
