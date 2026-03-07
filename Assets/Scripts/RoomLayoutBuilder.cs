using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a BSP layout for a rectangular house footprint.
/// Outputs room rectangles and interior wall segments along split lines.
/// Coordinate space: Vector2 uses (x, z).
/// </summary>
public static class RoomLayoutBuilder
{
    [Serializable]
    public struct RoomSpec
    {
        public Vector2 center;
        public float width;
        public float length;
        public Rect bounds;
    }

    public sealed class LayoutResult
    {
        public readonly List<RoomSpec> rooms;
        public readonly List<HouseMeshBuilder.WallSegment> interiorWalls;

        public LayoutResult(List<RoomSpec> rooms, List<HouseMeshBuilder.WallSegment> interiorWalls)
        {
            this.rooms = rooms;
            this.interiorWalls = interiorWalls;
        }
    }

    public static LayoutResult GenerateBspLayout(
        float houseWidth,
        float houseLength,
        int iterations,
        Vector2 minRoomSize,
        Vector2 maxRoomSize,
        int seed,
        float interiorWallThickness)
    {
        houseWidth = Mathf.Max(1f, houseWidth);
        houseLength = Mathf.Max(1f, houseLength);
        iterations = Mathf.Max(0, iterations);

        minRoomSize = new Vector2(Mathf.Max(1f, minRoomSize.x), Mathf.Max(1f, minRoomSize.y));
        maxRoomSize = new Vector2(Mathf.Max(minRoomSize.x, maxRoomSize.x), Mathf.Max(minRoomSize.y, maxRoomSize.y));
        maxRoomSize = new Vector2(Mathf.Min(maxRoomSize.x, houseWidth), Mathf.Min(maxRoomSize.y, houseLength));
        minRoomSize = new Vector2(Mathf.Min(minRoomSize.x, maxRoomSize.x), Mathf.Min(minRoomSize.y, maxRoomSize.y));

        var rng = new System.Random(seed);

        var partitions = new List<Rect>
        {
            new Rect(-houseWidth * 0.5f, -houseLength * 0.5f, houseWidth, houseLength)
        };

        var interiorWalls = new List<HouseMeshBuilder.WallSegment>();

        for (int i = 0; i < iterations; i++)
        {
            int splitIndex = FindSplittablePartition(partitions, minRoomSize, rng);
            if (splitIndex < 0)
            {
                break;
            }

            Rect rect = partitions[splitIndex];
            bool splitVertical = ChooseSplitDirection(rect, rng);

            if (splitVertical)
            {
                float minSplit = minRoomSize.x;
                float maxSplit = rect.width - minRoomSize.x;
                if (maxSplit <= minSplit)
                {
                    continue;
                }

                float split = Mathf.Lerp(minSplit, maxSplit, (float)rng.NextDouble());
                float splitX = rect.xMin + split;

                Rect left = new Rect(rect.xMin, rect.yMin, split, rect.height);
                Rect right = new Rect(splitX, rect.yMin, rect.width - split, rect.height);

                partitions[splitIndex] = left;
                partitions.Add(right);

                interiorWalls.Add(new HouseMeshBuilder.WallSegment
                {
                    start = new Vector2(splitX, rect.yMin),
                    end = new Vector2(splitX, rect.yMax),
                    thickness = interiorWallThickness
                });
            }
            else
            {
                float minSplit = minRoomSize.y;
                float maxSplit = rect.height - minRoomSize.y;
                if (maxSplit <= minSplit)
                {
                    continue;
                }

                float split = Mathf.Lerp(minSplit, maxSplit, (float)rng.NextDouble());
                float splitZ = rect.yMin + split;

                Rect bottom = new Rect(rect.xMin, rect.yMin, rect.width, split);
                Rect top = new Rect(rect.xMin, splitZ, rect.width, rect.height - split);

                partitions[splitIndex] = bottom;
                partitions.Add(top);

                interiorWalls.Add(new HouseMeshBuilder.WallSegment
                {
                    start = new Vector2(rect.xMin, splitZ),
                    end = new Vector2(rect.xMax, splitZ),
                    thickness = interiorWallThickness
                });
            }
        }

        var rooms = new List<RoomSpec>();
        for (int i = 0; i < partitions.Count; i++)
        {
            Rect rect = partitions[i];

            float roomWMax = Mathf.Min(maxRoomSize.x, rect.width);
            float roomLMax = Mathf.Min(maxRoomSize.y, rect.height);
            float roomWMin = Mathf.Min(minRoomSize.x, roomWMax);
            float roomLMin = Mathf.Min(minRoomSize.y, roomLMax);

            if (roomWMax <= 0f || roomLMax <= 0f)
            {
                continue;
            }

            float roomW = Mathf.Lerp(roomWMin, roomWMax, (float)rng.NextDouble());
            float roomL = Mathf.Lerp(roomLMin, roomLMax, (float)rng.NextDouble());

            float centerXMin = rect.xMin + roomW * 0.5f;
            float centerXMax = rect.xMax - roomW * 0.5f;
            float centerZMin = rect.yMin + roomL * 0.5f;
            float centerZMax = rect.yMax - roomL * 0.5f;

            float centerX = centerXMax >= centerXMin
                ? Mathf.Lerp(centerXMin, centerXMax, (float)rng.NextDouble())
                : rect.center.x;

            float centerZ = centerZMax >= centerZMin
                ? Mathf.Lerp(centerZMin, centerZMax, (float)rng.NextDouble())
                : rect.center.y;

            Rect roomBounds = new Rect(
                centerX - roomW * 0.5f,
                centerZ - roomL * 0.5f,
                roomW,
                roomL
            );

            rooms.Add(new RoomSpec
            {
                center = new Vector2(centerX, centerZ),
                width = roomW,
                length = roomL,
                bounds = roomBounds
            });
        }

        return new LayoutResult(rooms, interiorWalls);
    }

    private static bool ChooseSplitDirection(Rect rect, System.Random rng)
    {
        // TODO#1
        // Calculate the ratio of width to height
        // If the rect is wide (ratio >= 1.25), split vertically (return true)
        // If the rect is tall (ratio <= 0.8), split horizontally (return false)
        // Otherwise, split randomly
        throw new System.NotImplementedException();
    }

    private static int FindSplittablePartition(List<Rect> partitions, Vector2 minRoomSize, System.Random rng)
    {
        // TODO#2
        // Create an empty list of candidate indices
        // Loop through all partitions:
        //   A partition can be split if its width >= minRoomSize.x * 2 OR height >= minRoomSize.y * 2
        //   If so, add its index to candidates
        // If no candidates, return -1 (signals the BSP loop to stop)
        // Otherwise, return a random index from candidates
        throw new System.NotImplementedException();
    }
}
