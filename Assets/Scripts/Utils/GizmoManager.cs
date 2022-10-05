using System.Collections.Generic;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.Utils
{
    public class GizmoManager : MonoBehaviour
    {
        public bool draw;
        public bool onlySelected;
        readonly Dictionary<object, List<GizmoObject>> objects = new();

        private void Awake()
        {
            objects.Clear();
        }

        void Draw()
        {
            lock (this)
            {
                foreach ((var _, var list) in objects)
                {
                    foreach (var gizmoObject in list)
                    {
                        if (Gizmos.color != gizmoObject.color)
                            Gizmos.color = gizmoObject.color;
                        gizmoObject.Draw();
                    }
                }
            }
        }

        public void Add(object duration, GizmoObject obj)
        {
            lock (this)
            {
                if (objects.ContainsKey(duration))
                    objects[duration].Add(obj);
                else
                    objects[duration] = new() { obj };
            }
        }
        public void Add(object duration, IEnumerable<GizmoObject> obj)
        {
            lock (this)
            {
                if (objects.ContainsKey(duration))
                    objects[duration].AddRange(obj);
                else
                    objects[duration] = new(obj);
            }
        }

        public void Expire(object duration)
        {
            lock (this)
            {
                if (objects.ContainsKey(duration))
                    objects[duration].Clear();
            }
        }

        private void OnDrawGizmos()
        {
            if (draw && !onlySelected)
            {
                Draw();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (draw && onlySelected)
            {
                Draw();
            }
        }

        public abstract class GizmoObject
        {
            public readonly Color color;
            public GizmoObject(Color color)
            {
                this.color = color;
            }
            public abstract void Draw();
        }

        public class Line : GizmoObject
        {
            readonly Vector3 from;
            readonly Vector3 to;
            public Line(Color color, Vector3 from, Vector3 to) : base(color)
            {
                this.from = from;
                this.to = to;
            }
            public override void Draw()
            {
                Gizmos.DrawLine(from, to);
            }
        }

        public class Cube : GizmoObject
        {
            readonly Vector3 pos;
            readonly Vector3 size;
            public Cube(Color color, Vector3 pos, Vector3 size) : base(color)
            {
                this.pos = pos;
                this.size = size;
            }
            public Cube(Color color, Vector3 pos, float size) : base(color)
            {
                this.pos = pos;
                this.size = Vector3.one * size;
            }
            public override void Draw()
            {
                Gizmos.DrawWireCube(pos, size);
            }
        }
        public class Sphere : GizmoObject
        {
            readonly Vector3 pos;
            readonly float radius;
            public Sphere(Color color, Vector3 pos, float radius) : base(color)
            {
                this.pos = pos;
                this.radius = radius;
            }
            public override void Draw()
            {
                Gizmos.DrawWireSphere(pos, radius);
            }
        }
        public class Mesh : GizmoObject
        {
            readonly UnityEngine.Mesh mesh;
            readonly Vector3 pos;
            readonly Vector3 scale;
            readonly Quaternion rotation;

            public Mesh(Color color, UnityEngine.Mesh mesh, Vector3 pos) : base(color)
            {
                this.mesh = mesh;
                this.pos = pos;
                scale = Vector3.one;
                rotation = Quaternion.identity;
            }
            public Mesh(Color color, UnityEngine.Mesh mesh, Vector3 pos, Vector3 scale, Quaternion rotation) : base(color)
            {
                this.mesh = mesh;
                this.pos = pos;
                this.scale = scale;
                this.rotation = rotation;
            }

            public override void Draw()
            {
                Gizmos.DrawWireMesh(mesh, pos, rotation, scale);
            }
        }
    }
}
