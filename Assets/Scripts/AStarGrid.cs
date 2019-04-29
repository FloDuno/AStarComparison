using UnityEngine;

namespace AStarGameObject
{
    public class AStarGrid : MonoBehaviour
    {
        public static AStarGrid Instance;
        public Transform AStarTerrain;

        public Vector2Int GridSize;

        [HideInInspector]
        public Vector3[,] GridCells;

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
            GridCells = ScanTerrain(startScanPoint, realExtents * 2);
        }

        /// <summary>
        /// Raycast every point of the grid to get the height at this point
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private Vector3[,] ScanTerrain(Vector3 startPoint, Vector3 size)
        {
            // Do one more to make the grid extends to the ledge of the terrain
            var gridCells = new Vector3[GridSize.x + 1, GridSize.y + 1];
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

        public Vector3 GetNearestCell(Vector3 point)
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
            var lastCell = GridCells[GridCells.GetUpperBound(0),
                                     GridCells.GetUpperBound(1)];
            var shorterDistance =
                    Vector3.SqrMagnitude(lastCell - GridCells[0, 0]);
            var closestColumn = 0;
            for (var i = 0; i < GridSize.x + 1; i++)
            {
                var distance = Vector3.SqrMagnitude(GridCells[i, 0] - point);
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
                var distance =
                        Vector3.SqrMagnitude(
                            GridCells[closestColumn, j] - point);
                Debug.Log(distance);
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
            var toReturn = GridCells[closestColumn, closestRow];
            return toReturn;
        }
    }
}