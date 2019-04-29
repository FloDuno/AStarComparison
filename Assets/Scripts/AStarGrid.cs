﻿using UnityEngine;

namespace AStarGameObject
{
    public class AStarGrid : MonoBehaviour
    {
        public Transform AStarTerrain;

        public float MaxTerrainHeight;

        public Vector2Int GridSize;

        [HideInInspector]
        public Vector3[,] GridCells;

        private void OnValidate()
        {
            if (MaxTerrainHeight <= 0)
            {
                MaxTerrainHeight = 0.1f;
            }

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
            var terrainBounds = AStarTerrain
                                .GetComponent<MeshFilter>()
                                .mesh.bounds;
            // Get x and z real scales
            var terrainDimension = new Vector2(
                terrainBounds.size.x * AStarTerrain.lossyScale.x,
                terrainBounds.size.z * AStarTerrain.lossyScale.z);
            // Get left up corner when seen from above
            var startScanPoint = new Vector3(
                AStarTerrain.position.x - terrainDimension.x / 2,
                MaxTerrainHeight,
                AStarTerrain.position.z + terrainDimension.y / 2);
            GridCells = ScanTerrain(startScanPoint, terrainDimension);
        }

        /// <summary>
        /// Raycast every point of the grid to get the height at this point
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private Vector3[,] ScanTerrain(Vector3 startPoint, Vector2 size)
        {
            var gridCells = new Vector3[GridSize.x, GridSize.y];
            var step = new Vector2(size.x / GridSize.x, size.y / GridSize.y);
            for (var i = 0; i < GridSize.x; i++)
            {
                // First point of the i column
                var scanPos = new Vector3(
                    startPoint.x + step.x * i,
                    startPoint.y,
                    startPoint.z);
                // j = row
                for (var j = 0; j < GridSize.y; j++)
                {
                    RaycastHit hit;
                    var groundRaycast = Physics.Raycast(
                        scanPos,
                        Vector3.down,
                        out hit,
                        MaxTerrainHeight + 1);
                    if (!groundRaycast)
                    {
                        Debug.LogError(
                            $"No terrain found under point {scanPos}");
                        gridCells[i, j] = Vector3.negativeInfinity;
                    }
                    else
                    {
                        gridCells[i, j] = new Vector3(
                            scanPos.x,
                            hit.point.y,
                            scanPos.z);
                    }

                    // Change Row
                    scanPos -= new Vector3(0, 0, step.y);
                }
            }

            return gridCells;
        }
    }
}