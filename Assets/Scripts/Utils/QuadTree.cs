using UnityEngine;

namespace Utils
{
    public class QuadTree<T>
    {
        public readonly Vector2 pos;
        public readonly float scale;
        public readonly QuadTree<T> parent;
        public T value;
        public DiagonalDirs<QuadTree<T>>? children;

        public QuadTree(Vector2 pos, float scale, T value, QuadTree<T> parent)
        {
            this.pos = pos;
            this.scale = scale;
            this.value = value;
            this.parent = parent;
        }

        public DiagonalDirs<Vector2> GetChildrenPositions => WorldUtils.DIAGONAL_DIRS.Map(o => pos + (Vector2)o * scale);

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

        public QuadTree<T> GetClosestTo(Vector2 point)
        {
            if (children == null)
                return this;

            var diff = point - pos;
            return diff switch
            {
                { x: <= 0, y: >= 0 } => children.Value.NW,
                { x: >= 0, y: >= 0 } => children.Value.NE,
                { x: >= 0, y: <= 0 } => children.Value.SE,
                _ => children.Value.SW
            };
        }
    }
}
