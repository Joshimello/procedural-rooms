using System;
using System.Collections.Generic;
using UnityEngine;

public static class RoomFurniturePlacer
{
    public struct Config
    {
        public float wallThickness;
        public float interiorWallThickness;
        public float floorThickness;
        public float wallInset;
        public float itemYOffset;
        public float chairRadius;

        public GameObject livingRoomTablePrefab;
        public GameObject livingRoomChairPrefab;

        public GameObject bedroomBedPrefab;
        public GameObject bedroomClosetPrefab;

        public GameObject toiletBathtubPrefab;
        public GameObject toiletCarpetPrefab;
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

    public static void Populate(List<RoomLayoutBuilder.RoomSpec> rooms, int seed, Transform roomsRoot, Vector3 origin, Config config)
    {
        var rng = new System.Random(seed ^ rooms.Count);

        for (int i = 0; i < rooms.Count; i++)
        {
            RoomLayoutBuilder.RoomSpec room = rooms[i];

            GameObject roomGO = new GameObject($"Room_{i}");
            roomGO.transform.SetParent(roomsRoot, false);
            roomGO.transform.position = origin + new Vector3(room.center.x, 0f, room.center.y);

            RoomType type = (RoomType)rng.Next(0, Enum.GetValues(typeof(RoomType)).Length);
            switch (type)
            {
                case RoomType.LivingRoom:
                    PlaceLivingRoom(room, roomGO.transform, origin, config);
                    break;
                case RoomType.Bedroom:
                    PlaceBedroom(room, roomGO.transform, origin, config, rng);
                    break;
                case RoomType.Toilet:
                    PlaceToilet(room, roomGO.transform, origin, config, rng);
                    break;
            }
        }
    }

    private static void PlaceLivingRoom(RoomLayoutBuilder.RoomSpec room, Transform parent, Vector3 origin, Config config)
    {
        if (config.livingRoomTablePrefab != null)
        {
            float baseInset = Mathf.Max(config.wallThickness, config.interiorWallThickness) * 0.5f + config.wallInset;
            float maxInset = Mathf.Min(room.bounds.width, room.bounds.height) * 0.5f;
            float inset = Mathf.Min(baseInset, maxInset);
            float clampedX = Mathf.Clamp(room.center.x, room.bounds.xMin + inset, room.bounds.xMax - inset);
            float clampedZ = Mathf.Clamp(room.center.y, room.bounds.yMin + inset, room.bounds.yMax - inset);
            Vector3 tablePos = new Vector3(clampedX + origin.x, origin.y + config.floorThickness + config.itemYOffset, clampedZ + origin.z);
            GameObject tableInstance = InstantiatePrefab(config.livingRoomTablePrefab, tablePos, Quaternion.identity, parent);
            PostAdjustToFloor(tableInstance, origin, config);
            CenterInstanceToRoomCenter(tableInstance, room, origin);
            ClampInstanceToRoomBounds(tableInstance, room, origin, config);
        }

        if (config.livingRoomChairPrefab != null)
        {
            float baseInset = Mathf.Max(config.wallThickness, config.interiorWallThickness) * 0.5f + config.wallInset;
            float maxInset = Mathf.Min(room.bounds.width, room.bounds.height) * 0.5f;
            float inset = Mathf.Min(baseInset, maxInset);
            float maxRadius = Mathf.Min((room.bounds.width * 0.5f) - inset, (room.bounds.height * 0.5f) - inset);
            if (maxRadius <= 0.05f)
            {
                return;
            }

            float radius = Mathf.Min(config.chairRadius, maxRadius);
            float clampedX = Mathf.Clamp(room.center.x, room.bounds.xMin + inset, room.bounds.xMax - inset);
            float clampedZ = Mathf.Clamp(room.center.y, room.bounds.yMin + inset, room.bounds.yMax - inset);
            Vector3 center = new Vector3(clampedX + origin.x, origin.y + config.floorThickness + config.itemYOffset, clampedZ + origin.z);

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
                GameObject chairInstance = InstantiatePrefab(config.livingRoomChairPrefab, pos, rot, parent);
                PostAdjustToFloor(chairInstance, origin, config);
                ClampInstanceToRoomBounds(chairInstance, room, origin, config);
            }
        }
    }

    private static void PlaceBedroom(RoomLayoutBuilder.RoomSpec room, Transform parent, Vector3 origin, Config config, System.Random rng)
    {
        WallSide bedSide = GetRandomWall(rng);
        WallSide closetSide = GetDifferentWall(bedSide, rng);

        PlaceWallItem(config.bedroomBedPrefab, room, bedSide, parent, origin, config);
        PlaceWallItem(config.bedroomClosetPrefab, room, closetSide, parent, origin, config);
    }

    private static void PlaceToilet(RoomLayoutBuilder.RoomSpec room, Transform parent, Vector3 origin, Config config, System.Random rng)
    {
        WallSide tubSide = GetRandomWall(rng);
        WallSide carpetSide = GetDifferentWall(tubSide, rng);

        PlaceWallItem(config.toiletBathtubPrefab, room, tubSide, parent, origin, config);
        PlaceWallItem(config.toiletCarpetPrefab, room, carpetSide, parent, origin, config);
    }

    private static void PlaceWallItem(GameObject prefab, RoomLayoutBuilder.RoomSpec room, WallSide side, Transform parent, Vector3 origin, Config config)
    {
        if (prefab == null)
        {
            return;
        }

        GetWallPlacement(room, side, origin, config, out Vector3 pos, out Quaternion rot);
        GameObject instance = InstantiatePrefab(prefab, pos, rot, parent);
        PostAdjustToFloor(instance, origin, config);
        AlignToWallWithBounds(instance, room, side, rot, origin, config);
        ClampInstanceToRoomBounds(instance, room, origin, config);
    }

    private static void GetWallPlacement(RoomLayoutBuilder.RoomSpec room, WallSide side, Vector3 origin, Config config, out Vector3 position, out Quaternion rotation)
    {
        float baseInset = Mathf.Max(config.wallThickness, config.interiorWallThickness) * 0.5f + config.wallInset;
        float maxInset = Mathf.Min(room.bounds.width, room.bounds.height) * 0.5f;
        float inset = Mathf.Min(baseInset, maxInset);
        float y = origin.y + config.floorThickness + config.itemYOffset;

        float clampedX = Mathf.Clamp(room.center.x, room.bounds.xMin + inset, room.bounds.xMax - inset);
        float clampedZ = Mathf.Clamp(room.center.y, room.bounds.yMin + inset, room.bounds.yMax - inset);

        switch (side)
        {
            case WallSide.North:
                position = new Vector3(clampedX + origin.x, y, room.bounds.yMax + origin.z - inset);
                rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
                break;
            case WallSide.South:
                position = new Vector3(clampedX + origin.x, y, room.bounds.yMin + origin.z + inset);
                rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                break;
            case WallSide.East:
                position = new Vector3(room.bounds.xMax + origin.x - inset, y, clampedZ + origin.z);
                rotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
                break;
            case WallSide.West:
                position = new Vector3(room.bounds.xMin + origin.x + inset, y, clampedZ + origin.z);
                rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
                break;
            default:
                position = new Vector3(clampedX + origin.x, y, clampedZ + origin.z);
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
        GameObject instance = UnityEngine.Object.Instantiate(prefab, position, rotation, parent);
        instance.name = prefab.name;
        return instance;
    }

    private static void PostAdjustToFloor(GameObject instance, Vector3 origin, Config config)
    {
        if (instance == null)
        {
            return;
        }

        if (!TryGetBounds(instance, out Bounds bounds))
        {
            return;
        }

        float y = origin.y + config.floorThickness + config.itemYOffset;
        float delta = y - bounds.min.y;
        if (Mathf.Abs(delta) > 0.0001f)
        {
            instance.transform.position += new Vector3(0f, delta, 0f);
        }
    }

    private static void PostAdjustToWall(GameObject instance, Quaternion rotation)
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

    private static void AlignToWallWithBounds(GameObject instance, RoomLayoutBuilder.RoomSpec room, WallSide side, Quaternion rotation, Vector3 origin, Config config)
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

        float baseInset = Mathf.Max(config.wallThickness, config.interiorWallThickness) * 0.5f + config.wallInset;
        Vector3 offset = bounds.center - instance.transform.position;
        Vector3 pos = instance.transform.position;

        switch (side)
        {
            case WallSide.North:
                pos.z = (room.bounds.yMax + origin.z - baseInset - bounds.extents.z) - offset.z;
                break;
            case WallSide.South:
                pos.z = (room.bounds.yMin + origin.z + baseInset + bounds.extents.z) - offset.z;
                break;
            case WallSide.East:
                pos.x = (room.bounds.xMax + origin.x - baseInset - bounds.extents.x) - offset.x;
                break;
            case WallSide.West:
                pos.x = (room.bounds.xMin + origin.x + baseInset + bounds.extents.x) - offset.x;
                break;
        }

        instance.transform.position = pos;
    }

    private static void CenterInstanceToRoomCenter(GameObject instance, RoomLayoutBuilder.RoomSpec room, Vector3 origin)
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
        Vector3 roomWorldCenter = new Vector3(room.center.x, bounds.center.y, room.center.y) + origin;
        instance.transform.position = roomWorldCenter - offset;
    }

    private static void ClampInstanceToRoomBounds(GameObject instance, RoomLayoutBuilder.RoomSpec room, Vector3 origin, Config config)
    {
        if (instance == null)
        {
            return;
        }

        if (!TryGetBounds(instance, out Bounds bounds))
        {
            return;
        }

        float baseInset = Mathf.Max(config.wallThickness, config.interiorWallThickness) * 0.5f + config.wallInset;

        float minX = room.bounds.xMin + origin.x + baseInset;
        float maxX = room.bounds.xMax + origin.x - baseInset;
        float minZ = room.bounds.yMin + origin.z + baseInset;
        float maxZ = room.bounds.yMax + origin.z - baseInset;

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
}
