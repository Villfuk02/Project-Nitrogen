using UnityEngine;

namespace Utils
{
    public class LayerMasks : MonoBehaviour
    {
        const string DEFAULT_NAME = "Default";
        const string TRANSPARENT_FX_NAME = "TransparentFX";
        const string IGNORE_RAYCAST_NAME = "IgnoreRaycast";
        const string WATER_NAME = "Water";
        const string UI_NAME = "UI";
        const string COARSE_TERRAIN_NAME = "CoarseTerrain";
        const string SELECTION_NAME = "Selection";
        const string ATTACKER_NAME = "Attacker";
        const string TARGETING_NAME = "Targeting";
        const string ATTACKER_TARGET_NAME = "AttackerTarget";
        const string COARSE_OBSTACLE_NAME = "CoarseObstacle";
        const string PROJECTILE_NAME = "Projectile";
        const string SELECTION_ATTACKER_NAME = "SelectionAttacker";

        public static LayerMask coarseTerrain;
        public static LayerMask coarseTerrainAndObstacles;
        public static LayerMask selection;
        public static LayerMask selectionWithoutAttackers;
        public static LayerMask attackerTargets;

        void Awake()
        {
            coarseTerrain = LayerMask.GetMask(COARSE_TERRAIN_NAME);
            coarseTerrainAndObstacles = LayerMask.GetMask(COARSE_TERRAIN_NAME, COARSE_OBSTACLE_NAME);
            selection = LayerMask.GetMask(SELECTION_NAME, SELECTION_ATTACKER_NAME);
            selectionWithoutAttackers = LayerMask.GetMask(SELECTION_NAME);
            attackerTargets = LayerMask.GetMask(ATTACKER_TARGET_NAME);
        }
    }
}