using BattleSimulation.Attackers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BattleSimulation.Targeting
{
    public interface ITargetingParent
    {
        void TargetFound(Attacker target);
        void TargetLost(Attacker target);
    }
    public interface ITargetingChild
    {
        void InitParent(ITargetingParent targetingParent);
        bool IsInBounds(Vector3 pos);
    }
    internal abstract class CompositeTargetingComponent : ITargetingChild, ITargetingParent
    {
        protected ITargetingParent parent;
        protected Dictionary<Attacker, int> inRange = new();


        public virtual void InitParent(ITargetingParent targetingParent)
        {
            this.parent = targetingParent;
        }

        public abstract bool IsInBounds(Vector3 pos);

        public abstract void TargetFound(Attacker target);

        public abstract void TargetLost(Attacker target);
    }
    internal class UnionTargetingComponent : CompositeTargetingComponent
    {
        readonly ITargetingChild[] parts_;
        public UnionTargetingComponent(params ITargetingChild[] parts)
        {
            parts_ = parts;
        }

        public override void InitParent(ITargetingParent targetingParent)
        {
            base.InitParent(targetingParent);
            foreach (var part in parts_)
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
        public override bool IsInBounds(Vector3 pos) => parts_.Any(p => p.IsInBounds(pos));
    }
    internal class IntersectionTargetingComponent : CompositeTargetingComponent
    {
        readonly ITargetingChild[] parts_;
        public IntersectionTargetingComponent(params ITargetingChild[] parts)
        {
            parts_ = parts;
        }

        public override void InitParent(ITargetingParent targetingParent)
        {
            base.InitParent(targetingParent);
            foreach (var part in parts_)
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

            if (inRange[target] == parts_.Length)
                parent.TargetFound(target);
        }

        public override void TargetLost(Attacker target)
        {
            int count = --inRange[target];
            if (count < parts_.Length)
                parent.TargetLost(target);
            if (count == 0)
                inRange.Remove(target);
        }
        public override bool IsInBounds(Vector3 pos) => parts_.All(p => p.IsInBounds(pos));
    }
    internal class SubtractionTargetingComponent : CompositeTargetingComponent
    {
        readonly ITargetingChild mainPart_;
        readonly ITargetingChild[] subtractParts_;
        public SubtractionTargetingComponent(ITargetingChild mainPart, params ITargetingChild[] subtractParts)
        {
            mainPart_ = mainPart;
            subtractParts_ = subtractParts;
        }

        public override void InitParent(ITargetingParent targetingParent)
        {
            base.InitParent(targetingParent);
            mainPart_.InitParent(new MainInterface(this));
            foreach (var part in subtractParts_)
            {
                part.InitParent(this);
            }
        }

        class MainInterface : ITargetingParent
        {
            readonly SubtractionTargetingComponent o;
            public MainInterface(SubtractionTargetingComponent o) => this.o = o;
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
        public override bool IsInBounds(Vector3 pos) => mainPart_.IsInBounds(pos) && !subtractParts_.Any(p => p.IsInBounds(pos));
    }
}
