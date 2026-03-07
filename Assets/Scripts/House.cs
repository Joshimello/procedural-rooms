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
            interiorFilter.sharedMesh = HouseMeshBuilder.BuildInteriorWalls(
                layout.interiorWalls,
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

    private void BuildRoomPrefabs(List<RoomLayoutBuilder.RoomSpec> rooms, int seed)
    {
        Transform roomsRoot = GetOrCreateChild(RoomsName);
        ClearChildren(roomsRoot);

        RoomFurniturePlacer.Populate(rooms, seed, roomsRoot, transform.position, new RoomFurniturePlacer.Config
        {
            wallThickness = wallThickness,
            interiorWallThickness = interiorWallThickness,
            floorThickness = floorThickness,
            wallInset = wallInset,
            itemYOffset = itemYOffset,
            chairRadius = chairRadius,
            livingRoomTablePrefab = livingRoomTablePrefab,
            livingRoomChairPrefab = livingRoomChairPrefab,
            bedroomBedPrefab = bedroomBedPrefab,
            bedroomClosetPrefab = bedroomClosetPrefab,
            toiletBathtubPrefab = toiletBathtubPrefab,
            toiletCarpetPrefab = toiletCarpetPrefab,
        });
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

}
