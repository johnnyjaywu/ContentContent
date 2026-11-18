using UnityEngine;

namespace GameSystem.Grid.Unity
{
    /// <summary>
    /// Scene-placed MonoBehaviour defining grid dimensions, cell size, plane orientation,
    /// and Scene-view wireframe Gizmo visualization.
    /// </summary>
    public class GridBehaviour : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _width = 10;
        [SerializeField, Min(1)] private int _height = 10;
        [SerializeField, Min(0.01f)] private float _cellSize = 1f;
        [SerializeField] private GridPlane _plane = GridPlane.XZ;
        [SerializeField] private Color _gizmoColor = new Color(0.2f, 0.85f, 1f, 0.5f);

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;
        public GridPlane Plane => _plane;

        public UnityGridTransform GridTransform => new UnityGridTransform(transform.position, _cellSize, _plane);

        public Grid2D<TCell> CreateGrid<TCell>(System.Func<GridCoord, TCell> initialFactory = null)
        {
            return new Grid2D<TCell>(_width, _height, initialFactory);
        }

        public Vector3 GridToWorldCenter(GridCoord coord) => GridTransform.GridToWorldCenter(coord);
        public Vector3 GridToWorldCorner(GridCoord coord) => GridTransform.GridToWorldCorner(coord);
        public GridCoord WorldToGrid(Vector3 worldPos) => GridTransform.WorldToGrid(worldPos);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _gizmoColor;
            UnityGridTransform gt = GridTransform;

            for (int x = 0; x <= _width; x++)
            {
                Vector3 start = gt.GridToWorldCorner(new GridCoord(x, 0));
                Vector3 end = gt.GridToWorldCorner(new GridCoord(x, _height));
                Gizmos.DrawLine(start, end);
            }

            for (int y = 0; y <= _height; y++)
            {
                Vector3 start = gt.GridToWorldCorner(new GridCoord(0, y));
                Vector3 end = gt.GridToWorldCorner(new GridCoord(_width, y));
                Gizmos.DrawLine(start, end);
            }
        }
    }
}
