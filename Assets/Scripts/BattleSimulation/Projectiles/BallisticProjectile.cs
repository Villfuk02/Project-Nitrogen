using BattleSimulation.Attackers;
using UnityEngine;
using UnityEngine.Events;
using Utils;

namespace BattleSimulation.Projectiles
{
    public class BallisticProjectile : Projectile
    {
        static LayerMask attackerMask_;
        [Header("Settings")]
        public UnityEvent onImpact;
        public float timeToLiveAfterImpact;
        [Header("Runtime values")]
        public float impactRadius;
        public Vector3 velocity;
        public bool hit;

        void Awake()
        {
            if (attackerMask_ == 0)
                attackerMask_ = LayerMask.GetMask(LayerNames.ATTACKER_TARGET);
        }

        public void Init(Vector3 position, IProjectileSource source, Vector3 target, float delay, float impactRadius)
        {
            Init(position, source);
            this.impactRadius = impactRadius;
            velocity = (target - position) / delay - Physics.gravity * (delay - 2 * TimeUtils.SECS_PER_TICK) / 2;
        }

        void FixedUpdate()
        {
            if (hit)
            {
                timeToLiveAfterImpact -= Time.fixedDeltaTime;
                if (timeToLiveAfterImpact <= 0)
                    Destroy(gameObject);
                return;
            }

            transform.Translate(velocity * Time.fixedDeltaTime);
            velocity += Physics.gravity * Time.fixedDeltaTime;
            CheckTerrainHit(0.4f);
        }

        protected override void HitAttacker(Attacker attacker) => Impact();

        protected override void HitTerrain() => Impact();

        void Impact()
        {
            if (hit)
                return;
            hit = true;

            var hits = Physics.SphereCastAll(transform.position, impactRadius, Vector3.up, 0.01f, attackerMask_);
            foreach (var hit in hits)
            {
                Attacker a = hit.rigidbody.GetComponent<Attacker>();
                source.TryHit(this, a);
            }

            onImpact.Invoke();
        }
    }
}