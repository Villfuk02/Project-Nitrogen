using BattleVisuals.Selection.Highlightable;
using Game.AttackerStats;
using Game.Damage;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Utils;

namespace BattleSimulation.Attackers
{
    public class Attacker : MonoBehaviour, IHighlightable
    {
        public const float SMALL_TARGET_HEIGHT = 0.15f;
        public const float LARGE_TARGET_HEIGHT = 0.3f;
        public static readonly ModifiableCommand<(Attacker target, Damage damage)> HIT = new();
        public static readonly ModifiableCommand<(Attacker target, Damage damage)> DAMAGE = new();
        public static readonly ModifiableCommand<(Attacker target, Damage cause)> DIE = new();
        static Attacker()
        {
            DAMAGE.RegisterHandler(DamageHandler);
            DIE.RegisterHandler(DeathHandler);
        }
        [Header("References")]
        [SerializeField] Rigidbody rb;
        [SerializeField] Image highlight;
        [SerializeField] Animator highlightAnim;
        public Transform target;
        [Header("Settings")]
        [SerializeField] UnityEvent<Damage> onDamage;
        [SerializeField] UnityEvent<Damage> onDeath;
        public UnityEvent onRemoved;
        public UnityEvent<Attacker> onReachedHub;
        [Header("Runtime variables")]
        public AttackerStats originalStats;
        public AttackerStats stats;
        [SerializeField] float pathSegmentProgress;
        [SerializeField] Vector2Int pathSegmentTarget;
        [SerializeField] Vector2Int lastTarget;
        [SerializeField] uint pathSplitIndex;
        [SerializeField] int segmentsToCenter;
        public int health;
        bool removed_;
        public bool IsDead { get; private set; }

        void Awake()
        {
            stats = originalStats.Clone();
            health = stats.maxHealth;
        }

        void FixedUpdate()
        {
            if (IsDead)
                return;

            pathSegmentProgress += Time.fixedDeltaTime * stats.speed;

            while (pathSegmentProgress >= 1)
                if (!TryAdvanceSegment())
                    return;

            UpdateWorldPosition();
        }

        bool TryAdvanceSegment()
        {
            pathSegmentProgress--;
            segmentsToCenter--;
            lastTarget = pathSegmentTarget;
            uint ways = (uint)World.WorldData.World.data.tiles[pathSegmentTarget].pathNext.Count;
            if (ways == 0)
            {
                ReachedHub();
                return false;
            }

            uint chosen = pathSplitIndex % ways;
            pathSplitIndex /= ways;
            pathSegmentTarget = World.WorldData.World.data.tiles[pathSegmentTarget].pathNext[(int)chosen].pos;
            return true;
        }

        void ReachedHub()
        {
            if (!IsDead)
                onReachedHub.Invoke(this);

            if (!removed_)
            {
                removed_ = true;
                onRemoved.Invoke();
            }

            IsDead = true;
            Destroy(gameObject);
        }

        void UpdateWorldPosition()
        {
            Vector2 pos = Vector2.Lerp(lastTarget, pathSegmentTarget, pathSegmentProgress);
            float height = World.WorldData.World.data.tiles.GetHeightAt(pos);
            Vector3 worldPos = WorldUtils.TilePosToWorldPos(pos.x, pos.y, height);
            rb.MovePosition(worldPos);
            transform.position = worldPos;
        }

        public void InitPath(Vector2Int start, Vector2Int firstNode, uint index)
        {
            lastTarget = start;
            pathSegmentTarget = firstNode;
            pathSegmentProgress = 0;
            pathSplitIndex = index;
            segmentsToCenter = World.WorldData.World.data.tiles[firstNode].dist + 1;
            UpdateWorldPosition();
        }

        public float GetDistanceToCenter()
        {
            return segmentsToCenter - pathSegmentProgress;
        }

        static bool DamageHandler(ref (Attacker target, Damage damage) param)
        {
            param.target.TakeDamage(param.damage);
            return true;
        }

        static bool DeathHandler(ref (Attacker target, Damage cause) param)
        {
            param.target.Die(param.cause);
            return true;
        }

        void TakeDamage(Damage dmg)
        {
            if (IsDead)
                throw new InvalidOperationException("Dead attackers cannot be damaged");
            if (dmg.amount < 0)
                throw new ArgumentException("Damage cannot be negative");

            dmg.amount = Mathf.Floor(dmg.amount);
            if (dmg.amount == 0)
                return;

            onDamage.Invoke(dmg);

            health -= (int)dmg.amount;
            if (health <= 0)
            {
                (Attacker, Damage) param = (this, dmg);
                DIE.Invoke(param);
            }
        }

        void Die(Damage cause)
        {
            if (IsDead)
                throw new InvalidOperationException("Dead attackers cannot die");

            IsDead = true;
            Destroy(gameObject, 5f);
            rb.detectCollisions = false;

            onDeath.Invoke(cause);
            if (removed_)
                return;
            removed_ = true;
            onRemoved.Invoke();
        }

        void OnDrawGizmosSelected()
        {
            if (World.WorldData.World.instance == null || !World.WorldData.World.instance.Ready)
                return;
            float height = World.WorldData.World.data.tiles.GetHeightAt(pathSegmentTarget);
            Vector3 segTarget = WorldUtils.TilePosToWorldPos(pathSegmentTarget.x, pathSegmentTarget.y, height);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(segTarget, Vector3.one * 0.15f);
            Gizmos.DrawLine(transform.position, segTarget);
        }

        public void Highlight(Color color)
        {
            highlight.color = color;
            highlightAnim.SetTrigger(IHighlightable.HIGHLIGHT_TRIGGER);
        }

        public void Unhighlight()
        {
            highlightAnim.SetTrigger(IHighlightable.UNHIGHLIGHT_TRIGGER);
        }
    }
}
