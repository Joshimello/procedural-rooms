# Procedural Rooms

A simple unity procedural room generation system that uses binary space partitioning

## Requirements

- Unity 2018.4 LTS or newer
- Unity 6 recommended (currently tested version)

## Quick Start

1. Add the `House.cs` component to an empty GameObject
2. Assign furniture prefabs (optional)
3. Adjust parameters in the Inspector
4. The house generates automatically in Edit Mode

## Parameters

### House Dimensions

- **Width**: Overall house width (default: 12m)
- **Length**: Overall house length (default: 12m)
- **Wall Height**: Height of all walls (default: 3m)

### Room Generation

- **BSP Iterations**: Number of subdivision passes (default: 4)
- **Min Room Size**: Minimum room dimensions (default: 3x3m)
- **Max Room Size**: Maximum room dimensions (default: 8x8m)

### Customization

- **Seed**: Random seed for reproducible layouts
- **Randomize Seed**: Generate new random seed on each rebuild
- **Generate toggles**: Enable/disable floor, outer walls, or interior walls

## Project Structure

```
Assets/Scripts/
├── House.cs              # Main MonoBehaviour component
├── HouseMeshBuilder.cs   # Builds house shell and interior wall meshes
├── RoomLayoutBuilder.cs  # BSP algorithm for room layout generation
└── RoomMeshBuilder.cs    # Builds individual room meshes
```

## How It Works

1. **Layout Generation**: `RoomLayoutBuilder` uses BSP to split the house into rectangular partitions
2. **Mesh Building**: `HouseMeshBuilder` and `RoomMeshBuilder` generate procedural geometry
3. **Furniture Placement**: `House.cs` assigns room types and places furniture along walls
4. **Edit Mode Updates**: Changes to parameters automatically trigger rebuilds

## License

MIT
