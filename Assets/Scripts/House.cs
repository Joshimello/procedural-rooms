using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class House : MonoBehaviour
{
    private const string ShellName = "HouseShell";
    private const string InteriorName = "InteriorWalls";
    private const string RoomsName = "Rooms";

    [Header("House Dimensions")]
    [Min(1f)]
    [SerializeField] private float width = 12f;

    [Min(1f)]
    [SerializeField] private float length = 12f;

    [Min(0.1f)]
    [SerializeField] private float wallHeight = 3f;

    [Header("Thickness")]
    [Min(0.01f)]
    [SerializeField] private float wallThickness = 0.2f;

    [Min(0.01f)]
    [SerializeField] private float floorThickness = 0.2f;

    [Min(0.01f)]
    [SerializeField] private float interiorWallThickness = 0.15f;

    [Header("Room Layout (BSP)")]
    [Min(0)]
    [SerializeField] private int bspIterations = 4;

    [Min(1f)]
    [SerializeField] private Vector2 minRoomSize = new Vector2(3f, 3f);

    [Min(1f)]
    [SerializeField] private Vector2 maxRoomSize = new Vector2(8f, 8f);

    [SerializeField] private bool randomizeSeed = true;
    [SerializeField] private int seed = 12345;

    [Header("Mesh Toggles")]
    [SerializeField] private bool generateFloor = true;
    [SerializeField] private bool generateOuterWalls = true;
    [SerializeField] private bool generateInteriorWalls = true;

    [Header("Room Prefabs")]
    [SerializeField] private GameObject livingRoomTablePrefab;
    [SerializeField] private GameObject livingRoomChairPrefab;

    [SerializeField] private GameObject bedroomBedPrefab;
    [SerializeField] private GameObject bedroomClosetPrefab;

    [SerializeField] private GameObject toiletBathtubPrefab;
    [SerializeField] private GameObject toiletCarpetPrefab;

    [Header("Placement")]
    [Min(0f)]
    [SerializeField] private float wallInset = 0.2f;

    [Min(0f)]
    [SerializeField] private float itemYOffset = 0f;

    [Min(0.1f)]
    [SerializeField] private float chairRadius = 0.8f;

    private Mesh shellMesh;
    private Mesh interiorMesh;
    private bool rebuildQueued;

    private void Reset()
    {
        width = 12f;
        length = 12f;
        wallHeight = 3f;
        wallThickness = 0.2f;
        floorThickness = 0.2f;
        interiorWallThickness = 0.15f;

        bspIterations = 4;
        minRoomSize = new Vector2(3f, 3f);
        maxRoomSize = new Vector2(8f, 8f);

        randomizeSeed = true;
        seed = 12345;

        wallInset = 0.2f;
        itemYOffset = 0f;
        chairRadius = 0.8f;

        Rebuild();
    }

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        width = Mathf.Max(1f, width);
        length = Mathf.Max(1f, length);
        wallHeight = Mathf.Max(0.1f, wallHeight);
        wallThickness = Mathf.Max(0.01f, wallThickness);
        floorThickness = Mathf.Max(0.01f, floorThickness);
        interiorWallThickness = Mathf.Max(0.01f, interiorWallThickness);

        bspIterations = Mathf.Max(0, bspIterations);

        minRoomSize = new Vector2(Mathf.Max(1f, minRoomSize.x), Mathf.Max(1f, minRoomSize.y));
        maxRoomSize = new Vector2(Mathf.Max(minRoomSize.x, maxRoomSize.x), Mathf.Max(minRoomSize.y, maxRoomSize.y));
        maxRoomSize = new Vector2(Mathf.Min(maxRoomSize.x, width), Mathf.Min(maxRoomSize.y, length));
        minRoomSize = new Vector2(Mathf.Min(minRoomSize.x, maxRoomSize.x), Mathf.Min(minRoomSize.y, maxRoomSize.y));

        wallInset = Mathf.Max(0f, wallInset);
        chairRadius = Mathf.Max(0.1f, chairRadius);

        QueueRebuild();
    }

    private void QueueRebuild()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            Rebuild();
            return;
        }

        if (rebuildQueued)
        {
            return;
        }

        rebuildQueued = true;
        EditorApplication.delayCall += () =>
        {
            rebuildQueued = false;
            if (this == null)
            {
                return;
            }

            Rebuild();
        };
#else
        Rebuild();
#endif
    }

    private void Rebuild()
    {
        EnsureShellObjects(out MeshFilter shellFilter, out MeshRenderer shellRenderer);
        EnsureInteriorObjects(out MeshFilter interiorFilter, out MeshRenderer interiorRenderer);
        EnsureDefaultMaterial(shellRenderer);
        EnsureDefaultMaterial(interiorRenderer);

        if (shellMesh == null)
        {
            shellMesh = new Mesh { name = "HouseShellMesh" };
        }

        if (interiorMesh == null)
        {
            interiorMesh = new Mesh { name = "InteriorWallsMesh" };
        }

        shellFilter.sharedMesh = HouseMeshBuilder.BuildHouseShell(
            width,
            length,
            wallHeight,
            wallThickness,
            floorThickness,
            generateFloor,
            generateOuterWalls,
            shellMesh
        );

        int usedSeed = GetSeed();
        RoomLayoutBuilder.LayoutResult layout = RoomLayoutBuilder.GenerateBspLayout(
            width,
            length,
            bspIterations,
            minRoomSize,
            maxRoomSize,
            usedSeed,
            interiorWallThickness
        );

        if (generateInteriorWalls)
        {
            var wallSegments = ConvertWallSegments(layout.interiorWalls);
            interiorFilter.sharedMesh = HouseMeshBuilder.BuildInteriorWalls(
                wallSegments,
                wallHeight,
                floorThickness,
                interiorMesh
            );
        }
        else
        {
            interiorFilter.sharedMesh = null;
        }

        BuildRoomPrefabs(layout.rooms, usedSeed);
    }

    private int GetSeed()
    {
        return randomizeSeed ? UnityEngine.Random.Range(int.MinValue, int.MaxValue) : seed;
    }

    private void EnsureShellObjects(out MeshFilter filter, out MeshRenderer renderer)
    {
        Transform shellTransform = GetOrCreateChild(ShellName);
        filter = EnsureMeshComponents(shellTransform, out renderer);
    }

    private void EnsureInteriorObjects(out MeshFilter filter, out MeshRenderer renderer)
    {
        Transform interiorTransform = GetOrCreateChild(InteriorName);
        filter = EnsureMeshComponents(interiorTransform, out renderer);
    }

    private MeshFilter EnsureMeshComponents(Transform target, out MeshRenderer renderer)
    {
        MeshFilter filter = target.GetComponent<MeshFilter>();
        if (filter == null)
        {
            filter = target.gameObject.AddComponent<MeshFilter>();
        }

        renderer = target.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            renderer = target.gameObject.AddComponent<MeshRenderer>();
        }

        return filter;
    }

    private void EnsureDefaultMaterial(MeshRenderer renderer)
    {
        if (renderer == null || renderer.sharedMaterial != null)
        {
            return;
        }

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null)
        {
            litShader = Shader.Find("Standard");
        }

        if (litShader != null)
        {
            renderer.sharedMaterial = new Material(litShader);
        }
    }

    private Transform GetOrCreateChild(string name)
    {
        Transform child = transform.Find(name);
        if (child != null)
        {
            return child;
        }

        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        return go.transform;
    }

    private List<HouseMeshBuilder.WallSegment> ConvertWallSegments(List<RoomLayoutBuilder.WallSegment> walls)
    {
        var result = new List<HouseMeshBuilder.WallSegment>();
        for (int i = 0; i < walls.Count; i++)
        {
            RoomLayoutBuilder.WallSegment wall = walls[i];
            result.Add(new HouseMeshBuilder.WallSegment
            {
                start = wall.start,
                end = wall.end,
                thickness = wall.thickness
            });
        }

        return result;
    }

    private void BuildRoomPrefabs(List<RoomLayoutBuilder.RoomSpec> rooms, int seed)
    {
        Transform roomsRoot = GetOrCreateChild(RoomsName);
        ClearChildren(roomsRoot);

        var rng = new System.Random(seed ^ rooms.Count);

        for (int i = 0; i < rooms.Count; i++)
        {
            RoomLayoutBuilder.RoomSpec room = rooms[i];

            GameObject roomGO = new GameObject($"Room_{i}");
            roomGO.transform.SetParent(roomsRoot, false);
            roomGO.transform.position = transform.position + new Vector3(room.center.x, 0f, room.center.y);

            RoomType type = (RoomType)rng.Next(0, Enum.GetValues(typeof(RoomType)).Length);
            switch (type)
            {
                case RoomType.LivingRoom:
                    PlaceLivingRoom(room, roomGO.transform);
                    break;
                case RoomType.Bedroom:
                    PlaceBedroom(room, roomGO.transform, rng);
                    break;
                case RoomType.Toilet:
                    PlaceToilet(room, roomGO.transform, rng);
                    break;
            }
        }
    }

    private void PlaceLivingRoom(RoomLayoutBuilder.RoomSpec room, Transform parent)
    {
        if (livingRoomTablePrefab != null)
        {
            float baseInset = Mathf.Max(wallThickness, interiorWallThickness) * 0.5f + wallInset;
            float maxInset = Mathf.Min(room.bounds.width, room.bounds.height) * 0.5f;
            float inset = Mathf.Min(baseInset, maxInset);
            float clampedX = Mathf.Clamp(room.center.x, room.bounds.xMin + inset, room.bounds.xMax - inset);
            float clampedZ = Mathf.Clamp(room.center.y, room.bounds.yMin + inset, room.bounds.yMax - inset);
            Vector3 roomWorldOffset = transform.position;
            Vector3 tablePos = new Vector3(clampedX + roomWorldOffset.x, roomWorldOffset.y + floorThickness + itemYOffset, clampedZ + roomWorldOffset.z);
            GameObject tableInstance = InstantiatePrefab(livingRoomTablePrefab, tablePos, Quaternion.identity, parent);
            PostAdjustToFloor(tableInstance);
            CenterInstanceToRoomCenter(tableInstance, room);
            ClampInstanceToRoomBounds(tableInstance, room);
        }

        if (livingRoomChairPrefab != null)
        {
            float baseInset = Mathf.Max(wallThickness, interiorWallThickness) * 0.5f + wallInset;
            float maxInset = Mathf.Min(room.bounds.width, room.bounds.height) * 0.5f;
            float inset = Mathf.Min(baseInset, maxInset);
            float maxRadius = Mathf.Min((room.bounds.width * 0.5f) - inset, (room.bounds.height * 0.5f) - inset);
            if (maxRadius <= 0.05f)
            {
                return;
            }

            float radius = Mathf.Min(chairRadius, maxRadius);
            float clampedX = Mathf.Clamp(room.center.x, room.bounds.xMin + inset, room.bounds.xMax - inset);
            float clampedZ = Mathf.Clamp(room.center.y, room.bounds.yMin + inset, room.bounds.yMax - inset);
            Vector3 roomWorldOffset = transform.position;
            Vector3 center = new Vector3(clampedX + roomWorldOffset.x, roomWorldOffset.y + floorThickness + itemYOffset, clampedZ + roomWorldOffset.z);

            Vector3[] offsets =
            {
                new Vector3(radius, 0f, 0f),
                new Vector3(-radius, 0f, 0f),
                new Vector3(0f, 0f, radius),
                new Vector3(0f, 0f, -radius)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                Vector3 pos = center + offsets[i];
                Quaternion rot = Quaternion.LookRotation((center - pos).normalized, Vector3.up);
                GameObject chairInstance = InstantiatePrefab(livingRoomChairPrefab, pos, rot, parent);
                PostAdjustToFloor(chairInstance);
                ClampInstanceToRoomBounds(chairInstance, room);
            }
        }
    }

    private void PlaceBedroom(RoomLayoutBuilder.RoomSpec room, Transform parent, System.Random rng)
    {
        WallSide bedSide = GetRandomWall(rng);
        WallSide closetSide = GetDifferentWall(bedSide, rng);

        PlaceWallItem(bedroomBedPrefab, room, bedSide, parent);
        PlaceWallItem(bedroomClosetPrefab, room, closetSide, parent);
    }

    private void PlaceToilet(RoomLayoutBuilder.RoomSpec room, Transform parent, System.Random rng)
    {
        WallSide tubSide = GetRandomWall(rng);
        WallSide carpetSide = GetDifferentWall(tubSide, rng);

        PlaceWallItem(toiletBathtubPrefab, room, tubSide, parent);
        PlaceWallItem(toiletCarpetPrefab, room, carpetSide, parent);
    }

    private void PlaceWallItem(GameObject prefab, RoomLayoutBuilder.RoomSpec room, WallSide side, Transform parent)
    {
        if (prefab == null)
        {
            return;
        }

        Vector3 pos;
        Quaternion rot;
        GetWallPlacement(room, side, out pos, out rot);

        GameObject instance = InstantiatePrefab(prefab, pos, rot, parent);
        PostAdjustToFloor(instance);
        AlignToWallWithBounds(instance, room, side, rot);
        ClampInstanceToRoomBounds(instance, room);
    }

    private void GetWallPlacement(RoomLayoutBuilder.RoomSpec room, WallSide side, out Vector3 position, out Quaternion rotation)
    {
        float baseInset = Mathf.Max(wallThickness, interiorWallThickness) * 0.5f + wallInset;
        float maxInset = Mathf.Min(room.bounds.width, room.bounds.height) * 0.5f;
        float inset = Mathf.Min(baseInset, maxInset);
        float y = transform.position.y + floorThickness + itemYOffset;

        float clampedX = Mathf.Clamp(room.center.x, room.bounds.xMin + inset, room.bounds.xMax - inset);
        float clampedZ = Mathf.Clamp(room.center.y, room.bounds.yMin + inset, room.bounds.yMax - inset);
        Vector3 roomWorldOffset = transform.position;

        switch (side)
        {
            case WallSide.North:
                position = new Vector3(clampedX + roomWorldOffset.x, y, room.bounds.yMax + roomWorldOffset.z - inset);
                rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
                break;
            case WallSide.South:
                position = new Vector3(clampedX + roomWorldOffset.x, y, room.bounds.yMin + roomWorldOffset.z + inset);
                rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                break;
            case WallSide.East:
                position = new Vector3(room.bounds.xMax + roomWorldOffset.x - inset, y, clampedZ + roomWorldOffset.z);
                rotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
                break;
            case WallSide.West:
                position = new Vector3(room.bounds.xMin + roomWorldOffset.x + inset, y, clampedZ + roomWorldOffset.z);
                rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
                break;
            default:
                position = new Vector3(clampedX + roomWorldOffset.x, y, clampedZ + roomWorldOffset.z);
                rotation = Quaternion.identity;
                break;
        }
    }

    private static WallSide GetRandomWall(System.Random rng)
    {
        return (WallSide)rng.Next(0, 4);
    }

    private static WallSide GetDifferentWall(WallSide current, System.Random rng)
    {
        WallSide next = current;
        while (next == current)
        {
            next = (WallSide)rng.Next(0, 4);
        }

        return next;
    }

    private static GameObject InstantiatePrefab(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject instance = Instantiate(prefab, position, rotation, parent);
        instance.name = prefab.name;
        return instance;
    }

    private void PostAdjustToFloor(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (!TryGetBounds(instance, out Bounds bounds))
        {
            return;
        }

        float y = transform.position.y + floorThickness + itemYOffset;
        float delta = y - bounds.min.y;
        if (Mathf.Abs(delta) > 0.0001f)
        {
            instance.transform.position += new Vector3(0f, delta, 0f);
        }
    }

    private void PostAdjustToWall(GameObject instance, Quaternion rotation)
    {
        if (instance == null)
        {
            return;
        }

        if (!TryGetBounds(instance, out Bounds bounds))
        {
            return;
        }

        Vector3 forward = rotation * Vector3.forward;
        Vector3 absForward = new Vector3(Mathf.Abs(forward.x), Mathf.Abs(forward.y), Mathf.Abs(forward.z));
        float depth = Vector3.Dot(bounds.extents, absForward);

        if (depth > 0.0001f)
        {
            instance.transform.position += forward * depth;
        }
    }

    private void AlignToWallWithBounds(GameObject instance, RoomLayoutBuilder.RoomSpec room, WallSide side, Quaternion rotation)
    {
        if (instance == null)
        {
            return;
        }

        if (!TryGetBounds(instance, out Bounds bounds))
        {
            PostAdjustToWall(instance, rotation);
            return;
        }

        float baseInset = Mathf.Max(wallThickness, interiorWallThickness) * 0.5f + wallInset;
        Vector3 offset = bounds.center - instance.transform.position;
        Vector3 pos = instance.transform.position;
        Vector3 roomWorldOffset = transform.position;

        switch (side)
        {
            case WallSide.North:
                pos.z = (room.bounds.yMax + roomWorldOffset.z - baseInset - bounds.extents.z) - offset.z;
                break;
            case WallSide.South:
                pos.z = (room.bounds.yMin + roomWorldOffset.z + baseInset + bounds.extents.z) - offset.z;
                break;
            case WallSide.East:
                pos.x = (room.bounds.xMax + roomWorldOffset.x - baseInset - bounds.extents.x) - offset.x;
                break;
            case WallSide.West:
                pos.x = (room.bounds.xMin + roomWorldOffset.x + baseInset + bounds.extents.x) - offset.x;
                break;
        }

        instance.transform.position = pos;
    }

    private void CenterInstanceToRoomCenter(GameObject instance, RoomLayoutBuilder.RoomSpec room)
    {
        if (instance == null)
        {
            return;
        }

        if (!TryGetBounds(instance, out Bounds bounds))
        {
            return;
        }

        Vector3 offset = bounds.center - instance.transform.position;
        Vector3 roomWorldCenter = new Vector3(room.center.x, bounds.center.y, room.center.y) + transform.position;
        instance.transform.position = roomWorldCenter - offset;
    }

    private void ClampInstanceToRoomBounds(GameObject instance, RoomLayoutBuilder.RoomSpec room)
    {
        if (instance == null)
        {
            return;
        }

        if (!TryGetBounds(instance, out Bounds bounds))
        {
            return;
        }

        float baseInset = Mathf.Max(wallThickness, interiorWallThickness) * 0.5f + wallInset;
        Vector3 roomWorldOffset = transform.position;

        float minX = room.bounds.xMin + roomWorldOffset.x + baseInset;
        float maxX = room.bounds.xMax + roomWorldOffset.x - baseInset;
        float minZ = room.bounds.yMin + roomWorldOffset.z + baseInset;
        float maxZ = room.bounds.yMax + roomWorldOffset.z - baseInset;

        float extX = bounds.extents.x;
        float extZ = bounds.extents.z;

        float xMin = minX + extX;
        float xMax = maxX - extX;
        float zMin = minZ + extZ;
        float zMax = maxZ - extZ;

        if (xMax < xMin)
        {
            float mid = (minX + maxX) * 0.5f;
            xMin = mid;
            xMax = mid;
        }

        if (zMax < zMin)
        {
            float mid = (minZ + maxZ) * 0.5f;
            zMin = mid;
            zMax = mid;
        }

        Vector3 offset = bounds.center - instance.transform.position;
        Vector3 center = bounds.center;
        center.x = Mathf.Clamp(center.x, xMin, xMax);
        center.z = Mathf.Clamp(center.z, zMin, zMax);
        instance.transform.position = center - offset;
    }

    private static bool TryGetBounds(GameObject instance, out Bounds bounds)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (Application.isPlaying)
            {
#if UNITY_EDITOR
                DestroyImmediate(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
            else
            {
#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(child.gameObject);
#else
                DestroyImmediate(child.gameObject);
#endif
            }
        }
    }

    private enum RoomType
    {
        LivingRoom = 0,
        Bedroom = 1,
        Toilet = 2
    }

    private enum WallSide
    {
        North = 0,
        South = 1,
        East = 2,
        West = 3
    }
}
