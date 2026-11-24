using UnityEngine;

namespace ContentContent.Core.Grid.Unity
{
    /// <summary>
    /// Scene-placed MonoBehaviour defining grid dimensions, cell size, plane orientation,
    /// and Scene-view wireframe Gizmo visualization.
    /// </summary>
    [AddComponentMenu("ContentContent/Core/Grid")]
    [HelpURL("https://johnnyjaywu.github.io/ContentContent/#/core/quickstart")]
    public class GridBehaviour : MonoBehaviour
    {
        [Tooltip("Number of grid columns along X.")]
        [SerializeField, Min(1)] private int _width = 10;

        [Tooltip("Number of grid rows along Y (XY) or Z (XZ).")]
        [SerializeField, Min(1)] private int _height = 10;

        [Tooltip("World-space width and height of each cell.")]
        [SerializeField, Min(0.01f)] private float _cellSize = 1f;

        [Tooltip("2D orientation (XY) or 3D ground orientation (XZ).")]
        [SerializeField] private GridPlane _plane = GridPlane.XZ;

        [Tooltip("Wireframe gizmo color drawn in the Scene view when selected.")]
        [SerializeField] private Color _gizmoColor = new Color(0.2f, 0.85f, 1f, 0.5f);

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;
        public GridPlane Plane => _plane;

        public UnityGridTransform GridTransform => new UnityGridTransform(transform.position, _cellSize, _plane);

        private void OnValidate()
        {
            _width = Mathf.Max(1, _width);
            _height = Mathf.Max(1, _height);
            _cellSize = Mathf.Max(0.01f, _cellSize);
        }

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
