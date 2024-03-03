using UnityEngine;

namespace Utils
{
    public class QuadTree<T>
    {
        public delegate T CalculateValue(Vector2Int pos, int depth);
        public readonly Vector2Int pos;
        public readonly int depth;
        public readonly QuadTree<T> parent;
        public T value;
        public DiagonalDirs<QuadTree<T>>? children;

        public QuadTree(Vector2Int pos, int depth, T value, QuadTree<T> parent)
        {
            this.pos = pos;
            this.depth = depth;
            this.value = value;
            this.parent = parent;
        }

        public QuadTree(Vector2Int pos, int depth, CalculateValue valueProvider, QuadTree<T> parent) : this(pos, depth, default(T), parent)
        {
            value = valueProvider(pos, depth);
        }

        public void InitializeChildren(CalculateValue valueProvider)
        {
            children = WorldUtils.DIAGONAL_DIRS.Map(offset => new QuadTree<T>(pos * 2 + (offset + Vector2Int.one) / 2, depth + 1, valueProvider, this));
        }
    }
}
