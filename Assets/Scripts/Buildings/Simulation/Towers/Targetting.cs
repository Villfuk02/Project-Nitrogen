using Assets.Scripts.Attackers.Simulation;
using Assets.Scripts.Utils;
using System.Collections.Generic;
using UnityEngine;
using static Assets.Scripts.World.WorldData.WorldData;

namespace Assets.Scripts.Buildings.Simulation.Towers
{
    public class Targetting : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] TargettingCollider[] parts;
        static LayerMask visibilityMask;
        [Header("Settings")]
        [SerializeField] bool checkLineOfSight;
        [Header("Runtime values")]
        public Attacker? target;

        private void Awake()
        {
            visibilityMask = LayerMask.GetMask(LayerNames.COARSE_TERRAIN, LayerNames.COARSE_BLOCKER);
            foreach (var part in parts)
            {
                part.targetting = this;
            }
        }
        private void FixedUpdate()
        {
            if (target == null)
                Retarget();
            else if (checkLineOfSight && !HasLineOfSight(target.target.position))
                TargetLost();
        }

        public void TargetFound()
        {
            if (target == null)
                Retarget();
        }
        public void TargetLost()
        {
            target = null;
            Retarget();
        }
        private void Retarget()
        {
            if (parts.Length == 0)
                return;
            HashSet<Attacker> valid = new(parts[0].inRange);
            for (int i = 1; i < parts.Length; i++)
            {
                valid.IntersectWith(parts[i].inRange);
            }
            if (valid.Count > 0)
            {
                float min = float.PositiveInfinity;
                foreach (Attacker attacker in valid)
                {
                    if (attacker == null || !HasLineOfSight(attacker.target.position))
                        continue;
                    float dist = attacker.GetDistanceToCenter();
                    if (dist < min)
                    {
                        min = dist;
                        target = attacker;
                    }
                }
            }
        }
        private bool HasLineOfSight(Vector3 pos)
        {
            Vector3 dir = pos - transform.position;
            return !Physics.Raycast(transform.position, dir, out RaycastHit _, dir.magnitude, visibilityMask);
        }

        private void OnDrawGizmosSelected()
        {
            if (parts.Length == 0)
                return;
            HashSet<Attacker> valid = new(parts[0].inRange);
            HashSet<Attacker> all = new(parts[0].inRange);
            for (int i = 1; i < parts.Length; i++)
            {
                valid.IntersectWith(parts[i].inRange);
                all.UnionWith(parts[i].inRange);
            }
            foreach (Attacker attacker in all)
            {
                if (attacker == null || !HasLineOfSight(attacker.target.position))
                    continue;
                if (attacker == target)
                    Gizmos.color = Color.red;
                else if (valid.Contains(attacker))
                    Gizmos.color = Color.yellow;
                else
                    continue;
                Gizmos.DrawLine(transform.position, attacker.target.position);
            }
        }

        private void OnDrawGizmos()
        {
            DisplayRangeGizmos(4);
        }

        private void DisplayRangeGizmos(float range)
        {
            int subdivisions = 360;
            Vector3?[] hits = new Vector3?[subdivisions];
            for (int i = 0; i < subdivisions; i++)
            {
                float angle = 2 * Mathf.PI / subdivisions * i;
                Vector3 offset = new(Mathf.Sin(angle) * range, 10, Mathf.Cos(angle) * range);
                if (Physics.Raycast(transform.position + offset, Vector3.down, out RaycastHit hit, 20, LayerMask.GetMask(LayerNames.COARSE_TERRAIN)))
                    hits[i] = hit.point;
                else
                    hits[i] = null;
            }
            Gizmos.color = Color.green;
            for (int i = 0; i < subdivisions; i++)
            {
                int j = (i + 1) % subdivisions;
                if (hits[i] != null && hits[j] != null)
                    Gizmos.DrawLine(hits[i].Value, hits[j].Value);
            }

            if (WORLD_DATA != null && WORLD_DATA.pathStarts != null)
            {
                foreach (var tile in WORLD_DATA.tiles)
                {
                    foreach (var next in tile.pathNext)
                    {
                        DisplayPathReach(tile.pos, next.pos);
                    }
                }
                for (int i = 0; i < WORLD_DATA.pathStarts.Length; i++)
                {
                    DisplayPathReach(WORLD_DATA.pathStarts[i], WORLD_DATA.firstPathNodes[i]);
                }
            }
        }

        private void DisplayPathReach(Vector2Int from, Vector2Int to)
        {
            for (float f = 0; f < 1; f += 0.2f)
            {
                Vector2 interpolated = Vector2.Lerp(from, to, f);
                float height = WORLD_DATA.tiles.GetHeightAt(interpolated) ?? WORLD_DATA.tiles.GetHeightAt(to) ?? -3;
                Vector3 pos = WorldUtils.TileToWorldPos(interpolated.x, interpolated.y, height);
                if (new Vector2(pos.x - transform.position.x, pos.z - transform.position.z).sqrMagnitude <= 16)
                {
                    Vector3 smallPos = pos + Vector3.up * Attacker.SMALL_TARGET_HEIGHT;
                    Vector3 largePos = pos + Vector3.up * Attacker.LARGE_TARGET_HEIGHT;
                    if (HasLineOfSight(smallPos))
                        Gizmos.color = Color.green;
                    else if (HasLineOfSight(largePos))
                        Gizmos.color = Color.yellow;
                    else
                        Gizmos.color = Color.red;
                    Gizmos.DrawCube(smallPos, new(0.2f, 0, 0.2f));
                }
            }
        }
    }
}

