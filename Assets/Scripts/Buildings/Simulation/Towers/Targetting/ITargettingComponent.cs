
using Attackers.Simulation;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Buildings.Simulation.Towers.Targetting
{
    public interface ITargettingParent
    {
        void TargetFound(Attacker target);
        void TargetLost(Attacker target);
    }
    public interface ITargettingChild
    {
        void InitParent(ITargettingParent parent);
        bool IsInBounds(Vector3 pos);
    }
    internal abstract class CompositeTargettingComponent : ITargettingChild, ITargettingParent
    {
        protected ITargettingParent parent;
        protected Dictionary<Attacker, int> inRange = new();


        public virtual void InitParent(ITargettingParent parent)
        {
            this.parent = parent;
        }

        public abstract bool IsInBounds(Vector3 pos);

        public abstract void TargetFound(Attacker target);

        public abstract void TargetLost(Attacker target);
    }
    internal class UnionTargettingComponent : CompositeTargettingComponent
    {
        readonly ITargettingChild[] parts;
        public UnionTargettingComponent(params ITargettingChild[] parts)
        {
            this.parts = parts;
        }

        public override void InitParent(ITargettingParent parent)
        {
            base.InitParent(parent);
            foreach (var part in parts)
            {
                part.InitParent(this);
            }
        }
        public override void TargetFound(Attacker target)
        {
            if (inRange.ContainsKey(target))
            {
                inRange[target]++;
            }
            else
            {
                inRange.Add(target, 1);
                parent.TargetFound(target);
            }
        }

        public override void TargetLost(Attacker target)
        {
            int count = --inRange[target];
            if (count == 0)
            {
                inRange.Remove(target);
                parent.TargetLost(target);
            }
        }
        public override bool IsInBounds(Vector3 pos) => parts.Any(p => p.IsInBounds(pos));
    }
    internal class IntersectionTargettingComponent : CompositeTargettingComponent
    {
        readonly ITargettingChild[] parts;
        public IntersectionTargettingComponent(params ITargettingChild[] parts)
        {
            this.parts = parts;
        }

        public override void InitParent(ITargettingParent parent)
        {
            base.InitParent(parent);
            foreach (var part in parts)
            {
                part.InitParent(this);
            }
        }

        public override void TargetFound(Attacker target)
        {
            if (inRange.ContainsKey(target))
                inRange[target]++;
            else
                inRange.Add(target, 1);

            if (inRange[target] == parts.Length)
                parent.TargetFound(target);
        }

        public override void TargetLost(Attacker target)
        {
            int count = --inRange[target];
            if (count < parts.Length)
                parent.TargetLost(target);
            if (count == 0)
                inRange.Remove(target);
        }
        public override bool IsInBounds(Vector3 pos) => parts.All(p => p.IsInBounds(pos));
    }
    internal class SubtractionTargettingComponent : CompositeTargettingComponent
    {
        readonly ITargettingChild mainPart;
        readonly ITargettingChild[] subtractParts;
        public SubtractionTargettingComponent(ITargettingChild mainPart, params ITargettingChild[] subtractParts)
        {
            this.mainPart = mainPart;
            this.subtractParts = subtractParts;
        }

        public override void InitParent(ITargettingParent parent)
        {
            base.InitParent(parent);
            mainPart.InitParent(new MainInterface(this));
            foreach (var part in subtractParts)
            {
                part.InitParent(this);
            }
        }

        class MainInterface : ITargettingParent
        {
            readonly SubtractionTargettingComponent o;
            public MainInterface(SubtractionTargettingComponent o) => this.o = o;
            public void TargetFound(Attacker target) => o.TargetLost(target);
            public void TargetLost(Attacker target) => o.TargetFound(target);
        }

        public override void TargetFound(Attacker target)
        {
            if (inRange.ContainsKey(target))
            {
                int count = --inRange[target];
                if (count == 0)
                {
                    inRange.Remove(target);
                    parent.TargetLost(target);
                }
            }
            else
            {
                inRange.Add(target, -1);
            }
        }

        public override void TargetLost(Attacker target)
        {
            if (inRange.ContainsKey(target))
            {
                int count = ++inRange[target];
                if (count == 0)
                    inRange.Remove(target);
            }
            else
            {
                inRange.Add(target, 1);
                parent.TargetFound(target);
            }
        }
        public override bool IsInBounds(Vector3 pos) => mainPart.IsInBounds(pos) && !subtractParts.Any(p => p.IsInBounds(pos));
    }
}
