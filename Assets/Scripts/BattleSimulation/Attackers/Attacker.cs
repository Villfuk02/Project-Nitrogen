using System;
using BattleVisuals.Selection.Highlightable;
using Game.AttackerStats;
using Game.Damage;
using Game.Shared;
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
        public static readonly ModifiableCommand<(Attacker target, float amount)> HEAL = new();
        public static readonly ModifiableCommand<(Attacker target, Damage cause)> DIE = new();

        public delegate void SpawnAttackersRelative(Attacker attacker, AttackerStats stats, int count, float offsetRadius);

        public static SpawnAttackersRelative spawnAttackersRelative;

        static Attacker()
        {
            DAMAGE.RegisterHandler(DamageHandler);
            HEAL.RegisterHandler(HealHandler);
            DIE.RegisterHandler(DeathHandler);
        }

        [Header("References")]
        [SerializeField] Rigidbody rb;
        [SerializeField] Image highlight;
        [SerializeField] Animator highlightAnim;
        public Transform target;
        public Transform visualTarget;
        [Header("Settings")]
        [SerializeField] UnityEvent<Damage> onDamage;
        [SerializeField] UnityEvent<Damage> onDeath;
        public UnityEvent onRemoved;
        public UnityEvent<Attacker> onReachedHub;
        [Header("Runtime variables")]
        public AttackerStats originalStats;
        public AttackerStats stats;
        public float pathSegmentProgress;
        public Vector2Int pathSegmentTarget;
        public Vector2Int lastTarget;
        public uint pathSplitIndex;
        public Vector2Int firstNode;
        public Vector2Int startPosition;
        public uint startPathSplitIndex;
        public int segmentsToHub;
        public int health;
        bool removed_;
        public bool IsDead { get; private set; }

        public virtual void FixedUpdate()
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
            segmentsToHub--;
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

        public void Init(AttackerStats stats, Vector2Int start, Vector2Int firstNode, uint pathSplitIndex)
        {
            originalStats = stats;
            this.stats = originalStats.Clone();
            health = stats.maxHealth;

            lastTarget = startPosition = start;
            pathSegmentTarget = this.firstNode = firstNode;
            pathSegmentProgress = 0;
            this.pathSplitIndex = startPathSplitIndex = pathSplitIndex;
            segmentsToHub = World.WorldData.World.data.tiles[firstNode].dist + 1;
            UpdateWorldPosition();
        }

        public void AddPathProgress(float progress, uint newPathSplitIndex)
        {
            pathSegmentProgress += progress;
            while (pathSegmentProgress >= 1)
                if (!TryAdvanceSegment())
                    return;
            pathSplitIndex = newPathSplitIndex;
            UpdateWorldPosition();
        }

        public float GetDistanceToHub()
        {
            return segmentsToHub - pathSegmentProgress;
        }

        static bool DamageHandler(ref (Attacker target, Damage damage) param) => param.target.TakeDamage(ref param.damage);

        static bool HealHandler(ref (Attacker target, float amount) param) => param.target.Heal(ref param.amount);

        static bool DeathHandler(ref (Attacker target, Damage cause) param)
        {
            param.target.Die(param.cause);
            return true;
        }

        bool TakeDamage(ref Damage dmg)
        {
            if (IsDead)
                throw new InvalidOperationException("Dead attackers cannot be damaged");
            if (dmg.amount < 0)
                throw new ArgumentException("Damage cannot be negative");

            dmg.amount = Mathf.Min(Mathf.Floor(dmg.amount), health);
            if (dmg.amount == 0)
                return false;

            onDamage.Invoke(dmg);

            health -= (int)dmg.amount;
            if (health <= 0)
            {
                (Attacker, Damage) param = (this, dmg);
                DIE.Invoke(param);
            }

            return true;
        }

        bool Heal(ref float amount)
        {
            if (IsDead)
                throw new InvalidOperationException("Dead attackers cannot be healed");
            if (amount < 0)
                throw new ArgumentException("Healing cannot be negative");

            amount = Mathf.Min(Mathf.Floor(amount), stats.maxHealth - health);
            health += (int)amount;
            return amount > 0;
        }

        void Die(Damage cause)
        {
            if (IsDead)
                throw new InvalidOperationException("Dead attackers cannot die");

            IsDead = true;
            Destroy(gameObject, 5f);
            rb.detectCollisions = false;

            onDeath.Invoke(cause);
            SoundController.PlaySound(SoundController.Sound.AttackerDie, 0.5f, 1, 0.2f, transform.position, SoundController.Priority.Low);
            if (removed_)
                return;
            removed_ = true;
            onRemoved.Invoke();
        }

        void OnDrawGizmosSelected()
        {
            if (World.WorldData.World.instance == null || !World.WorldData.World.Ready)
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

        public bool TryHit(Damage damage, out int damageDealt)
        {
            damageDealt = 0;
            if (IsDead)
                return false;
            (Attacker a, Damage dmg) hitParam = (this, damage);
            if (!HIT.InvokeRef(ref hitParam))
                return false;
            if (hitParam.dmg.amount == 0)
                return true;
            if (DAMAGE.InvokeRef(ref hitParam))
                damageDealt = (int)hitParam.dmg.amount;
            return true;
        }
    }
}