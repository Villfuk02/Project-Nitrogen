using BattleSimulation.Attackers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

namespace BattleSimulation.Targeting
{
    public interface ITargetingParent
    {
        void TargetFound(Attacker target);
        void TargetLost(Attacker target);
    }
    public interface ITargetingChild
    {
        void SetParent(ITargetingParent targetingParent);
        bool IsInBounds(Vector3 pos);
    }
    internal abstract class CompositeTargetingComponent : ITargetingChild, ITargetingParent
    {
        protected ITargetingParent parent;
        protected Dictionary<Attacker, int> inRange = new();


        public virtual void SetParent(ITargetingParent targetingParent)
        {
            parent = targetingParent;
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

        public override void SetParent(ITargetingParent targetingParent)
        {
            base.SetParent(targetingParent);
            foreach (var part in parts_)
            {
                part.SetParent(this);
            }
        }
        public override void TargetFound(Attacker target)
        {
            if (!inRange.ContainsKey(target))
            {
                inRange.Add(target, 1);
                parent.TargetFound(target);
            }
            else
            {
                inRange[target]++;
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

        public override void SetParent(ITargetingParent targetingParent)
        {
            base.SetParent(targetingParent);
            foreach (var part in parts_)
            {
                part.SetParent(this);
            }
        }

        public override void TargetFound(Attacker target)
        {
            inRange.Increment(target);

            if (inRange[target] == parts_.Length)
                parent.TargetFound(target);
        }

        public override void TargetLost(Attacker target)
        {
            int count = --inRange[target];
            if (count == parts_.Length - 1)
                parent.TargetLost(target);
            if (count == 0)
                inRange.Remove(target);
        }
        public override bool IsInBounds(Vector3 pos) => parts_.All(p => p.IsInBounds(pos));
    }

    internal class DifferenceTargetingComponent : ITargetingChild, ITargetingParent
    {
        ITargetingParent parent_;
        readonly ITargetingChild component1_;
        readonly ITargetingChild component2_;
        readonly HashSet<Attacker> inRange_ = new();

        public DifferenceTargetingComponent(ITargetingChild component1, ITargetingChild component2)
        {
            component1_ = component1;
            component2_ = component2;
        }

        public void SetParent(ITargetingParent targetingParent)
        {
            parent_ = targetingParent;
            component1_.SetParent(this);
            component2_.SetParent(this);
        }

        public void TargetFound(Attacker target) => Flip(target);
        public void TargetLost(Attacker target) => Flip(target);

        void Flip(Attacker target)
        {
            if (!inRange_.Contains(target))
            {
                inRange_.Add(target);
                parent_.TargetFound(target);
            }
            else
            {
                inRange_.Remove(target);
                parent_.TargetLost(target);
            }
        }

        public bool IsInBounds(Vector3 pos) => component1_.IsInBounds(pos) ^ component2_.IsInBounds(pos);
    }
}
