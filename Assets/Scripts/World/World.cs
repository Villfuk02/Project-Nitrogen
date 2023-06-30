
using UnityEngine;

namespace World
{
    public class World : MonoBehaviour
    {
        //[Header("References")]
        [Header("Runtime")]
        [SerializeField] Tile[,] tiles;
        public bool ready;
    }
}
