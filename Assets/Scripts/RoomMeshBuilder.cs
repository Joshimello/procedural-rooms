using System.Collections.Generic;
using UnityEngine;

public static class RoomMeshBuilder
{
    public static Mesh BuildRoom(
        float width,
        float length,
        float wallHeight,
        float wallThickness,
        float floorThickness,
        bool generateFloor,
        bool generateWalls,
        Mesh target = null)
    {
        if (target == null)
        {
            target = new Mesh { name = "RoomMesh" };
        }

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        float halfW = width * 0.5f;
        float halfL = length * 0.5f;
        float halfT = wallThickness * 0.5f;

        if (generateFloor)
        {
            Vector3 floorSize = new Vector3(width, floorThickness, length);
            Vector3 floorCenter = new Vector3(0f, floorThickness * 0.5f, 0f);
            AppendBox(floorSize, floorCenter, vertices, normals, uvs, triangles);
        }

        if (generateWalls)
        {
            float wallCenterY = floorThickness + wallHeight * 0.5f;

            Vector3 frontSize = new Vector3(width + wallThickness * 2f, wallHeight, wallThickness);
            Vector3 frontCenter = new Vector3(0f, wallCenterY, halfL + halfT);
            AppendBox(frontSize, frontCenter, vertices, normals, uvs, triangles);

            Vector3 backSize = new Vector3(width + wallThickness * 2f, wallHeight, wallThickness);
            Vector3 backCenter = new Vector3(0f, wallCenterY, -halfL - halfT);
            AppendBox(backSize, backCenter, vertices, normals, uvs, triangles);

            Vector3 leftSize = new Vector3(wallThickness, wallHeight, length + wallThickness * 2f);
            Vector3 leftCenter = new Vector3(-halfW - halfT, wallCenterY, 0f);
            AppendBox(leftSize, leftCenter, vertices, normals, uvs, triangles);

            Vector3 rightSize = new Vector3(wallThickness, wallHeight, length + wallThickness * 2f);
            Vector3 rightCenter = new Vector3(halfW + halfT, wallCenterY, 0f);
            AppendBox(rightSize, rightCenter, vertices, normals, uvs, triangles);
        }

        target.Clear();
        target.SetVertices(vertices);
        target.SetNormals(normals);
        target.SetUVs(0, uvs);
        target.SetTriangles(triangles, 0);
        target.RecalculateBounds();

        return target;
    }

    public struct RoomSpec
    {
        public Vector2 center;
        public float width;
        public float length;
    }

    public static Mesh BuildRooms(
        IReadOnlyList<RoomSpec> rooms,
        float wallHeight,
        float wallThickness,
        float floorThickness,
        bool generateFloor,
        bool generateWalls,
        Mesh target = null)
    {
        if (target == null)
        {
            target = new Mesh { name = "RoomMesh" };
        }

        if (rooms == null || rooms.Count == 0)
        {
            target.Clear();
            return target;
        }

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        for (int i = 0; i < rooms.Count; i++)
        {
            RoomSpec room = rooms[i];

            float halfW = room.width * 0.5f;
            float halfL = room.length * 0.5f;
            float halfT = wallThickness * 0.5f;

            if (generateFloor)
            {
                Vector3 floorSize = new Vector3(room.width, floorThickness, room.length);
                Vector3 floorCenter = new Vector3(room.center.x, floorThickness * 0.5f, room.center.y);
                AppendBox(floorSize, floorCenter, vertices, normals, uvs, triangles);
            }

            if (generateWalls)
            {
                float wallCenterY = floorThickness + wallHeight * 0.5f;

                Vector3 frontSize = new Vector3(room.width + wallThickness * 2f, wallHeight, wallThickness);
                Vector3 frontCenter = new Vector3(room.center.x, wallCenterY, room.center.y + halfL + halfT);
                AppendBox(frontSize, frontCenter, vertices, normals, uvs, triangles);

                Vector3 backSize = new Vector3(room.width + wallThickness * 2f, wallHeight, wallThickness);
                Vector3 backCenter = new Vector3(room.center.x, wallCenterY, room.center.y - halfL - halfT);
                AppendBox(backSize, backCenter, vertices, normals, uvs, triangles);

                Vector3 leftSize = new Vector3(wallThickness, wallHeight, room.length + wallThickness * 2f);
                Vector3 leftCenter = new Vector3(room.center.x - halfW - halfT, wallCenterY, room.center.y);
                AppendBox(leftSize, leftCenter, vertices, normals, uvs, triangles);

                Vector3 rightSize = new Vector3(wallThickness, wallHeight, room.length + wallThickness * 2f);
                Vector3 rightCenter = new Vector3(room.center.x + halfW + halfT, wallCenterY, room.center.y);
                AppendBox(rightSize, rightCenter, vertices, normals, uvs, triangles);
            }
        }

        target.Clear();
        target.SetVertices(vertices);
        target.SetNormals(normals);
        target.SetUVs(0, uvs);
        target.SetTriangles(triangles, 0);
        target.RecalculateBounds();

        return target;
    }

    private static void AppendBox(
        Vector3 size,
        Vector3 center,
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> triangles)
    {
        Vector3 half = size * 0.5f;

        Vector3 p0 = center + new Vector3(-half.x, -half.y, -half.z);
        Vector3 p1 = center + new Vector3(half.x, -half.y, -half.z);
        Vector3 p2 = center + new Vector3(half.x, -half.y, half.z);
        Vector3 p3 = center + new Vector3(-half.x, -half.y, half.z);
        Vector3 p4 = center + new Vector3(-half.x, half.y, -half.z);
        Vector3 p5 = center + new Vector3(half.x, half.y, -half.z);
        Vector3 p6 = center + new Vector3(half.x, half.y, half.z);
        Vector3 p7 = center + new Vector3(-half.x, half.y, half.z);

        AddFace(p3, p2, p6, p7, Vector3.forward, vertices, normals, uvs, triangles); // Front
        AddFace(p1, p0, p4, p5, Vector3.back, vertices, normals, uvs, triangles);    // Back
        AddFace(p0, p3, p7, p4, Vector3.left, vertices, normals, uvs, triangles);    // Left
        AddFace(p2, p1, p5, p6, Vector3.right, vertices, normals, uvs, triangles);   // Right
        AddFace(p4, p7, p6, p5, Vector3.up, vertices, normals, uvs, triangles);      // Top
        AddFace(p0, p1, p2, p3, Vector3.down, vertices, normals, uvs, triangles);    // Bottom
    }

    private static void AddFace(
        Vector3 v0,
        Vector3 v1,
        Vector3 v2,
        Vector3 v3,
        Vector3 normal,
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> triangles)
    {
        int index = vertices.Count;

        vertices.Add(v0);
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);

        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);

        uvs.Add(new Vector2(0f, 0f));
        uvs.Add(new Vector2(1f, 0f));
        uvs.Add(new Vector2(1f, 1f));
        uvs.Add(new Vector2(0f, 1f));

        triangles.Add(index + 0);
        triangles.Add(index + 1);
        triangles.Add(index + 2);
        triangles.Add(index + 0);
        triangles.Add(index + 2);
        triangles.Add(index + 3);
    }
}
