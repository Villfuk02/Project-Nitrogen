using BattleVisuals.Selection.Highlightable;
using Game.Damage;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Utils;

namespace BattleSimulation.Attackers
{
    public class Attacker : MonoBehaviour, IHighlightable
    {
        [Header("References")]
        [SerializeField] Rigidbody rb;
        [SerializeField] UnityEvent<Damage> onDamage;
        [SerializeField] UnityEvent<IDamageSource> onDeath;
        [SerializeField] Image highlight;
        [SerializeField] Animator highlightAnim;
        [Header("Constants")]
        public const float SMALL_TARGET_HEIGHT = 0.3f;
        public const float LARGE_TARGET_HEIGHT = 0.6f;
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
        public int deadTime;

        void Awake()
        {
            health = maxHealth;
        }

        void FixedUpdate()
        {
            if (deadTime > 0)
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

        public void TakeDamage(Damage damage)
        {
            onDamage.Invoke(damage);
            if (damage.amount <= 0)
                return;

            health -= damage.amount;
            if (health <= 0)
                Die(damage.source);
        }

        public void Die(IDamageSource source)
        {
            deadTime = 1;
            rb.detectCollisions = false;

            onDeath.Invoke(source);
        }

        public void SetHighlightColor(Color color)
        {
            highlight.color = color;
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
