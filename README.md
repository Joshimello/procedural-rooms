# Procedural Rooms

A simple unity procedural room generation system that uses binary space partitioning

## Requirements

- Unity 2018.4 LTS or newer
- Unity 6 recommended (currently tested version)

## Quick Start

### Clone and Open in Unity

1. Clone the repository:
   ```bash
   git clone https://github.com/Joshimello/procedural-rooms.git
   ```
2. Open Unity Hub and click "Add project from disk"
3. Navigate to the cloned `procedural-rooms` folder and select it
4. Open the project with Unity 2018.4 LTS or newer

### Using the Generator

1. Create a new scene or open an existing one
2. Add the `House.cs` component to an empty GameObject
3. Assign furniture prefabs (optional)
4. Adjust parameters in the Inspector
5. The house generates automatically in Edit Mode

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
