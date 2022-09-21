using System.Collections.Generic;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.Utils
{
    public class GizmoManager : MonoBehaviour
    {
        public bool draw;
        public bool onlySelected;
        readonly Dictionary<object, List<GizmoObject>> objects = new();

        void Draw()
        {
            foreach (var list in objects)
            {
                foreach (var gizmoObject in list.Value)
                {
                    if (Gizmos.color != gizmoObject.color)
                        Gizmos.color = gizmoObject.color;
                    gizmoObject.Draw();
                }
            }
        }

        public void Add(object duration, GizmoObject obj)
        {
            if (objects.ContainsKey(duration))
                objects[duration].Add(obj);
            else
                objects[duration] = new() { obj };
        }
        public void Add(object duration, List<GizmoObject> obj)
        {
            if (objects.ContainsKey(duration))
                objects[duration].AddRange(obj);
            else
                objects[duration] = obj;
        }
        public void Add(object duration, params GizmoObject[] obj)
        {
            if (objects.ContainsKey(duration))
                objects[duration].AddRange(obj);
            else
                objects[duration] = new(obj);
        }

        public void Expire(object duration)
        {
            if (objects.ContainsKey(duration))
                objects[duration].Clear();
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
            public Color color;
            public GizmoObject(Color color)
            {
                this.color = color;
            }
            public abstract void Draw();
        }

        public class Line : GizmoObject
        {
            Vector3 from;
            Vector3 to;
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
            Vector3 pos;
            Vector3 size;
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
    }
}
