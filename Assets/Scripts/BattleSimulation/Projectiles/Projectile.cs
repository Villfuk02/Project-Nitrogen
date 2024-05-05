using BattleSimulation.Attackers;
using UnityEngine;
using Utils;

namespace BattleSimulation.Projectiles
{
    public abstract class Projectile : MonoBehaviour
    {
        public IProjectileSource source;
        protected abstract void HitAttacker(Attacker attacker);
        protected abstract void HitTerrain();

        public void Init(Vector3 position, IProjectileSource source)
        {
            this.source = source;
            transform.position = position;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<Attacker>(out var attacker))
                HitAttacker(attacker);
            else
                HitTerrain();
        }

        protected void CheckTerrainHit(float radius)
        {
            var newTilePos = WorldUtils.WorldPosToTilePos(transform.localPosition);
            bool underground = World.WorldData.World.data.tiles.GetHeightAt(newTilePos) >= newTilePos.z - radius;
            if (underground)
                HitTerrain();
        }
    }
}
