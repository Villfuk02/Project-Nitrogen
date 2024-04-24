using UnityEngine;

namespace BattleSimulation.Attackers
{
    public class Instantiate : MonoBehaviour
    {
        public GameObject prefab;
        public Transform parent;
        public Vector3 position;
        public bool relativeToTransform;

        public void DoInstantiate()
        {
            Vector3 pos = position;
            if (relativeToTransform)
                pos += transform.position;
            if (parent == null)
                Instantiate(prefab, pos, Quaternion.identity);
            else
                Instantiate(prefab, position, Quaternion.identity, parent);
        }
    }
}
