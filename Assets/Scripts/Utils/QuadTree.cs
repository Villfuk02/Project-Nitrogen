using UnityEngine;

namespace Utils
{
    // TODO: generalize, in HighlightController use a derived class instead
    public class QuadTree<T>
    {
        public readonly Vector2Int pos;
        public readonly int scale;
        public readonly QuadTree<T> parent;
        public T value;
        public DiagonalDirs<QuadTree<T>>? children;

        public QuadTree(Vector2Int pos, int scale, T value, QuadTree<T> parent)
        {
            this.pos = pos;
            this.scale = scale;
            this.value = value;
            this.parent = parent;
        }

        public DiagonalDirs<Vector2Int> GetChildrenPositions => WorldUtils.DIAGONAL_DIRS.Map(o => pos + (Vector2Int.one + o) * scale / 4);

        public void SetChildrenValues(DiagonalDirs<T> values)
        {
            var positions = GetChildrenPositions;
            children = new(
                new(positions[0], scale / 2, values[0], this),
                new(positions[1], scale / 2, values[1], this),
                new(positions[2], scale / 2, values[2], this),
                new(positions[3], scale / 2, values[3], this)
                );
        }
    }
}
