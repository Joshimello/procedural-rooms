using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds mesh geometry for a rectangular house shell (floor + 4 outer walls with thickness)
/// and interior divider walls (line segments with thickness).
/// Coordinate space: Vector2 uses (x, z).
/// </summary>
public static class HouseMeshBuilder
{
    public struct WallSegment
    {
        public Vector2 start;
        public Vector2 end;
        public float thickness;
    }

    public static Mesh BuildHouseShell(
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
            target = new Mesh { name = "HouseShellMesh" };
        }

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        width = Mathf.Max(1f, width);
        length = Mathf.Max(1f, length);
        wallHeight = Mathf.Max(0.1f, wallHeight);
        wallThickness = Mathf.Max(0.01f, wallThickness);
        floorThickness = Mathf.Max(0.01f, floorThickness);

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

    public static Mesh BuildInteriorWalls(
        IReadOnlyList<WallSegment> wallSegments,
        float wallHeight,
        float floorThickness,
        Mesh target = null)
    {
        if (target == null)
        {
            target = new Mesh { name = "InteriorWallsMesh" };
        }

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        if (wallSegments == null || wallSegments.Count == 0)
        {
            target.Clear();
            return target;
        }

        wallHeight = Mathf.Max(0.1f, wallHeight);
        floorThickness = Mathf.Max(0.01f, floorThickness);

        for (int i = 0; i < wallSegments.Count; i++)
        {
            WallSegment segment = wallSegments[i];

            Vector2 start = segment.start;
            Vector2 end = segment.end;
            float thickness = Mathf.Max(0.01f, segment.thickness);

            Vector2 dir = (end - start);
            float length = dir.magnitude;
            if (length <= Mathf.Epsilon)
            {
                continue;
            }

            Vector2 axis = dir / length;
            Vector2 center2D = (start + end) * 0.5f;

            Vector3 wallCenter = new Vector3(center2D.x, floorThickness + wallHeight * 0.5f, center2D.y);
            Vector3 wallSize = new Vector3(thickness, wallHeight, length);

            Quaternion rotation = Quaternion.LookRotation(new Vector3(axis.x, 0f, axis.y), Vector3.up);
            AppendBox(wallSize, wallCenter, rotation, vertices, normals, uvs, triangles);
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
        AppendBox(size, center, Quaternion.identity, vertices, normals, uvs, triangles);
    }

    private static void AppendBox(
        Vector3 size,
        Vector3 center,
        Quaternion rotation,
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> triangles)
    {
        // TODO#4
        // Compute half-extents from size
        // Compute 8 corner points (p0–p7) by combining ±half.x, ±half.y, ±half.z,
        //   each rotated by 'rotation' and offset by 'center'
        //   p0 = (-x, -y, -z), p1 = (+x, -y, -z), p2 = (+x, -y, +z), p3 = (-x, -y, +z)  [bottom]
        //   p4 = (-x, +y, -z), p5 = (+x, +y, -z), p6 = (+x, +y, +z), p7 = (-x, +y, +z)  [top]
        // Call AddFace 6 times, one per side, using the correct 4 corners and rotated normal:
        //   Front (+Z): p3, p2, p6, p7
        //   Back  (-Z): p1, p0, p4, p5
        //   Left  (-X): p0, p3, p7, p4
        //   Right (+X): p2, p1, p5, p6
        //   Top   (+Y): p4, p7, p6, p5
        //   Bottom(-Y): p0, p1, p2, p3
        throw new System.NotImplementedException();
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
        // TODO#3
        // Record the current vertex count as 'index' (so triangle indices are relative to this face)
        // Add the 4 corner vertices (v0, v1, v2, v3) to the vertices list
        // Add the same normal 4 times (one per vertex)
        // Add UVs: (0,0), (1,0), (1,1), (0,1) — bottom-left going counter-clockwise
        // Add 2 triangles (6 indices): triangle 1 = (0,1,2), triangle 2 = (0,2,3)
        throw new System.NotImplementedException();
    }
}
