
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// Provides a simple, thread safe way to register gizmos to be drawn for some period of time.
    /// </summary>
    public class GizmoManager : MonoBehaviour
    {
        [SerializeField] bool draw;
        [SerializeField] bool onlyDrawWhenSelected;
        readonly object lock_ = new();
        // holds all the gizmos to keep drawing
        readonly Dictionary<object, List<GizmoObject>> objects_ = new();

        void Awake()
        {
            lock (lock_)
            {
                objects_.Clear();
            }
        }

        void Draw()
        {
            lock (lock_)
            {
                foreach (var (_, list) in objects_)
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
        /// <summary>
        /// Adds a gizmo to be drawn, until <see cref="Expire"/> with the same duration is called.
        /// </summary>
        public void Add(object duration, GizmoObject obj)
        {
            lock (lock_)
            {
                if (!objects_.ContainsKey(duration))
                    objects_.Add(duration, new());
                objects_[duration].Add(obj);
            }
        }
        /// <summary>
        /// Adds a multiple gizmos to be drawn, until <see cref="Expire"/> with the same duration is called.
        /// </summary>
        public void Add(object duration, IEnumerable<GizmoObject> obj)
        {
            lock (lock_)
            {
                if (!objects_.ContainsKey(duration))
                    objects_.Add(duration, new());
                objects_[duration].AddRange(obj);
            }
        }
        /// <summary>
        /// Stops drawing all gizmos with the given duration.
        /// </summary>
        public void Expire(object duration)
        {
            lock (lock_)
            {
                if (objects_.TryGetValue(duration, out var list))
                    list.Clear();
            }
        }

        void OnDrawGizmos()
        {
            if (draw && !onlyDrawWhenSelected)
            {
                Draw();
            }
        }

        void OnDrawGizmosSelected()
        {
            if (draw && onlyDrawWhenSelected)
            {
                Draw();
            }
        }

        public abstract class GizmoObject
        {
            public readonly Color color;

            protected GizmoObject(Color color)
            {
                this.color = color;
            }
            public abstract void Draw();
        }

        public class Line : GizmoObject
        {
            readonly Vector3 from_;
            readonly Vector3 to_;
            /// <summary>
            /// A line from 'from' to 'to'.
            /// </summary>
            public Line(Color color, Vector3 from, Vector3 to) : base(color)
            {
                from_ = from;
                to_ = to;
            }
            public override void Draw()
            {
                Gizmos.DrawLine(from_, to_);
            }
        }

        public class Cube : GizmoObject
        {
            readonly Vector3 pos_;
            readonly Vector3 size_;
            /// <summary>
            /// A wireframe cube centered at 'pos' with width, height and depth equal to the x, y and z components of 'size'.
            /// </summary>
            public Cube(Color color, Vector3 pos, Vector3 size) : base(color)
            {
                pos_ = pos;
                size_ = size;
            }
            /// <summary>
            /// A wireframe cube centered at 'pos' with width, height and depth all equal to 'size'.
            /// </summary>
            public Cube(Color color, Vector3 pos, float size) : base(color)
            {
                pos_ = pos;
                size_ = Vector3.one * size;
            }
            public override void Draw()
            {
                Gizmos.DrawWireCube(pos_, size_);
            }
        }
        public class Sphere : GizmoObject
        {
            readonly Vector3 pos_;
            readonly float radius_;
            /// <summary>
            /// A wireframe sphere centered at 'pos' with radius 'radius'.
            /// </summary>
            public Sphere(Color color, Vector3 pos, float radius) : base(color)
            {
                pos_ = pos;
                radius_ = radius;
            }
            public override void Draw()
            {
                Gizmos.DrawWireSphere(pos_, radius_);
            }
        }
        public class Mesh : GizmoObject
        {
            readonly UnityEngine.Mesh mesh_;
            readonly Vector3 pos_;
            readonly Vector3 scale_;
            readonly Quaternion rotation_;
            /// <summary>
            /// A wireframe mesh with origin at 'pos'.
            /// </summary>
            public Mesh(Color color, UnityEngine.Mesh mesh, Vector3 pos) : base(color)
            {
                mesh_ = mesh;
                pos_ = pos;
                scale_ = Vector3.one;
                rotation_ = Quaternion.identity;
            }
            /// <summary>
            /// A wireframe mesh with origin at 'pos', with given scale and rotation.
            /// </summary>
            public Mesh(Color color, UnityEngine.Mesh mesh, Vector3 pos, Vector3 scale, Quaternion rotation) : base(color)
            {
                mesh_ = mesh;
                pos_ = pos;
                scale_ = scale;
                rotation_ = rotation;
            }

            public override void Draw()
            {
                Gizmos.DrawWireMesh(mesh_, pos_, rotation_, scale_);
            }
        }
    }
}
