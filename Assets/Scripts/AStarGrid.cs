using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace AStarGameObject
{
    using System;

    public class AStarGrid : MonoBehaviour
    {
        public class Cell
        {
            public Vector2Int Index;
            public Vector3 Position;

            public Cell(Vector2Int index, Vector3 position)
            {
                Index = index;
                Position = position;
            }

            public int ComputeScore(List<Cell> path, Cell goal)
            {
                var startDistance = path.Count;
                var startPoint = path.First().Index;
                var manhattanDistance =
                        Mathf.Abs(startPoint.x - goal.Index.x) +
                        Mathf.Abs(startPoint.y - goal.Index.y);
                return startDistance + manhattanDistance;
            }
        }

        public static AStarGrid Instance;
        public Transform AStarTerrain;

        public Vector2Int GridSize;

        private Cell[,] gridCells;

        private Vector3 realExtents;

        private void OnValidate()
        {
            if (GridSize.x <= 0)
            {
                GridSize.x = 1;
            }

            if (GridSize.y <= 0)
            {
                GridSize.y = 1;
            }
        }

        private void OnEnable()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            var terrainBounds = AStarTerrain
                                .GetComponent<MeshFilter>()
                                .mesh.bounds;
            // Get x and z real scales
            realExtents = new Vector3(
                terrainBounds.extents.x * AStarTerrain.lossyScale.x,
                terrainBounds.extents.y * AStarTerrain.lossyScale.y,
                terrainBounds.extents.z * AStarTerrain.lossyScale.z);
            // Get left up corner when seen from above
            var startScanPoint = new Vector3(
                AStarTerrain.position.x - realExtents.x,
                AStarTerrain.position.y + realExtents.y,
                AStarTerrain.position.z + realExtents.z);
            gridCells = ScanTerrain(startScanPoint, realExtents * 2);
            var path = GetPathIDs(Vector3.zero, new Vector3(-17, 0, 10));
            foreach (var point in path)
            {
                Debug.Log(point);
            }
        }

        /// <summary>
        /// Raycast every point of the grid to get the height at this point
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private Cell[,] ScanTerrain(Vector3 startPoint, Vector3 size)
        {
            // Do one more to make the grid extends to the ledge of the terrain
            var newCells = new Cell[GridSize.x + 1, GridSize.y + 1];
            var step = new Vector2(size.x / GridSize.x, size.z / GridSize.y);
            for (var i = 0; i <= GridSize.x; i++)
            {
                // First point of the i column
                var scanPos = new Vector3(
                    startPoint.x + step.x * i,
                    startPoint.y,
                    startPoint.z);
                // j = row
                for (var j = 0; j <= GridSize.y; j++)
                {
                    RaycastHit hit;
                    var groundRaycast = Physics.Raycast(
                        scanPos,
                        Vector3.down,
                        out hit,
                        size.y + 1);
                    if (!groundRaycast)
                    {
                        Debug.LogError(
                            $"No terrain found under point {scanPos}");
                        newCells[i, j] = null;
                    }
                    else
                    {
                        var cellPos = new Vector3(
                            scanPos.x,
                            hit.point.y,
                            scanPos.z);
                        var cellIndex = new Vector2Int(i, j);
                        newCells[i, j] = new Cell(cellIndex, cellPos);
                    }

                    // Change Row
                    scanPos -= new Vector3(0, 0, step.y);
                }
            }

            return newCells;
        }

        public Vector2Int GetNearestCellID(Vector3 point)
        {
            //Make sure the point is in the grid
            point.x = Mathf.Clamp(
                point.x,
                AStarTerrain.position.x - realExtents.x,
                AStarTerrain.position.x + realExtents.x);
            point.y = Mathf.Clamp(
                point.y,
                AStarTerrain.position.y - realExtents.y,
                AStarTerrain.position.y + realExtents.y);
            point.z = Mathf.Clamp(
                point.z,
                AStarTerrain.position.z - realExtents.z,
                AStarTerrain.position.z + realExtents.z);
            var lastCell = gridCells[gridCells.GetUpperBound(0),
                                     gridCells.GetUpperBound(1)];
            var shorterDistance = Vector3.SqrMagnitude(
                lastCell.Position - gridCells[0, 0].Position);
            var closestColumn = 0;
            for (var i = 0; i < GridSize.x + 1; i++)
            {
                var distance =
                        Vector3.SqrMagnitude(gridCells[i, 0].Position - point);
                if (distance < shorterDistance)
                {
                    shorterDistance = distance;
                    closestColumn = i;
                }
                else
                {
                    break;
                }
            }

            var closestRow = 0;
            for (var j = 0; j < GridSize.y; j++)
            {
                var distance = Vector3.SqrMagnitude(
                    gridCells[closestColumn, j].Position - point);
                if (distance <= shorterDistance)
                {
                    shorterDistance = distance;
                    closestRow = j;
                }
                else
                {
                    break;
                }
            }

            return new Vector2Int {x = closestColumn, y = closestRow};
        }

        public Cell GetNearestCell(Vector3 point)
        {
            var cellID = GetNearestCellID(point);
            return gridCells[cellID.x, cellID.y];
        }

        /// <summary>
        /// Get all cells needed to reach our goal
        ///
        /// Main source : https://www.raywenderlich.com/3016-introduction-to-a-pathfinding
        /// </summary>
        /// <param name="start"></param>
        /// <param name="goal"></param>
        /// <returns></returns>
        private List<Cell> GetPathIDs(Vector3 start, Vector3 goal)
        {
            var startCell = GetNearestCell(start);
            var goalCell = GetNearestCell(goal);
            var openList = new Dictionary<Cell, int>();
            var path = new List<Cell>();
            openList.Add(startCell, 0);
            while (openList.Count > 0)
            {
                openList = openList
                           .OrderBy(x => x.Value)
                           .ToDictionary(pair => pair.Key, pair => pair.Value);
                var bestCell = openList.First();
                path.Add(bestCell.Key);
                openList.Remove(bestCell.Key);

                var neighborCells = GetAdjacentCells(bestCell.Key);
                foreach (var cell in neighborCells)
                {
                    if (cell == goalCell)
                        return path;
                    if (openList.ContainsKey(cell))
                    {
                        openList[cell] = cell.ComputeScore(path, goalCell);
                    }
                    else
                    {
                        openList.Add(cell, cell.ComputeScore(path, goalCell));
                    }
                }
            }

            return path;
        }

        private Cell[] GetAdjacentCells(Cell center)
        {
            var neighbors = new List<Cell>();
            var centerId = center.Index;
            if (centerId.x == 0)
            {
                // Right Neighbor
                neighbors.Add(gridCells[centerId.x + 1, centerId.y]);
            }
            else if (centerId.x == GridSize.x)
            {
                // Left Neighbor
                neighbors.Add(gridCells[centerId.x - 1, centerId.y]);
            }
            else
            {
                neighbors.Add(gridCells[centerId.x + 1, centerId.y]);
                neighbors.Add(gridCells[centerId.x - 1, centerId.y]);
            }

            if (centerId.y == 0)
            {
                // Down Neighbor
                neighbors.Add(gridCells[centerId.x, centerId.y + 1]);
            }
            else if (center.Index.y == GridSize.y)
            {
                // Up Neighbor
                neighbors.Add(gridCells[centerId.x, centerId.y - 1]);
            }
            else
            {
                neighbors.Add(gridCells[centerId.x, centerId.y + 1]);
                neighbors.Add(gridCells[centerId.x, centerId.y - 1]);
            }

            return neighbors.ToArray();
        }
    }
}