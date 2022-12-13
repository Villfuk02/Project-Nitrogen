using UnityEngine;

namespace Assets.Scripts.World
{
    public class Tile : MonoBehaviour
    {
        public Vector2Int pos;
        public enum Obstacle { None, Path, Short, Tall, Crystals, Minerals }
        public Obstacle obstacle;
        public bool slant;
        public bool occupied; // change to building ref
    }
}