using BattleVisuals.Selection.Highlightable;
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
        public static GameCommand<(Attacker target, Damage damage)> hit = new();
        public static GameCommand<(Attacker target, Damage damage)> damage = new();
        public static GameCommand<(Attacker target, Damage cause)> die = new();
        static Attacker()
        {
            damage.Register(DamageHandler, 0);
            die.Register(DeathHandler, 0);
        }
        [Header("References")]
        [SerializeField] Rigidbody rb;
        [SerializeField] UnityEvent<Damage> onDamage;
        [SerializeField] UnityEvent<Damage> onDeath;
        public UnityEvent onRemoved;
        [SerializeField] Image highlight;
        [SerializeField] Animator highlightAnim;
        [Header("Constants")]
        public const float SMALL_TARGET_HEIGHT = 0.15f;
        public const float LARGE_TARGET_HEIGHT = 0.3f;
        [Header("Stats")]
        public float speed;
        public int maxHealth;
        [Header("Runtime References")]
        public Transform target;
        [Header("Runtime values")]
        [SerializeField] float pathSegmentProgress;
        [SerializeField] Vector2Int pathSegmentTarget;
        [SerializeField] Vector2Int lastTarget;
        [SerializeField] uint pathSplitIndex;
        [SerializeField] int segmentsToCenter;
        public int health;
        [SerializeField] int deadTime;
        public bool IsDead => deadTime > 0;

        void Awake()
        {
            health = maxHealth;
        }

        void FixedUpdate()
        {
            if (IsDead)
            {
                deadTime++;
                if (deadTime > 200)
                    Destroy(gameObject);
                return;
            }

            pathSegmentProgress += Time.fixedDeltaTime * speed;

            while (pathSegmentProgress >= 1)
            {
                pathSegmentProgress--;
                segmentsToCenter--;
                lastTarget = pathSegmentTarget;
                uint paths = (uint)World.WorldData.World.data.tiles[pathSegmentTarget].pathNext.Count;
                if (paths == 0)
                {
                    onRemoved.Invoke();
                    Destroy(gameObject);
                    return;
                }
                uint chosen = pathSplitIndex % paths;
                pathSplitIndex /= paths;
                pathSegmentTarget = World.WorldData.World.data.tiles[pathSegmentTarget].pathNext[(int)chosen].pos;
            }

            UpdateWorldPosition();
        }

        void UpdateWorldPosition()
        {
            Vector2 pos = Vector2.Lerp(lastTarget, pathSegmentTarget, pathSegmentProgress);
            float height = World.WorldData.World.data.tiles.GetHeightAt(pos) ?? World.WorldData.World.data.tiles.GetHeightAt(pathSegmentTarget)!.Value;
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
            transform.localPosition = Vector3.up * 200;
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
                throw new InvalidOperationException("dead attackers cannot be damaged!");
            if (dmg.amount < 0)
                throw new ArgumentException("damage cannot be negative!");

            dmg.amount = Mathf.Floor(dmg.amount);
            if (dmg.amount <= 0)
                return;

            onDamage.Invoke(dmg);

            health -= (int)dmg.amount;
            if (health <= 0)
            {
                (Attacker, Damage) param = (this, dmg);
                die.Invoke(param);
            }
        }

        void Die(Damage cause)
        {
            if (IsDead)
                throw new InvalidOperationException("dead attackers cannot die!");

            deadTime = 1;
            rb.detectCollisions = false;

            onDeath.Invoke(cause);
            onRemoved.Invoke();
        }

        void OnDrawGizmosSelected()
        {
            if (World.WorldData.World.instance is null || !World.WorldData.World.instance.Ready)
                return;
            float height = World.WorldData.World.data.tiles.GetHeightAt(pathSegmentTarget) ?? 0;
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
