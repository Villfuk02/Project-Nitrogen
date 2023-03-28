using UnityEngine;

namespace Assets.Scripts.World
{
    public class Tile : MonoBehaviour
    {
        [Header("References")]
        public Transform slantedParts;
        [SerializeField] Animator selectionAnimator;
        [Header("Properties")]
        public Vector2Int pos;
        public enum Obstacle { None, Path, Short, Tall, Crystals, Minerals }
        public Obstacle obstacle;
        public bool slant;
        [Header("Runtime variables")]
        public bool occupied; // change to building ref
        [SerializeField] bool hovered;


        public void Hover()
        {
            hovered = true;
            selectionAnimator.SetTrigger("Hover");
        }
        public void Unhover()
        {
            hovered = false;
            selectionAnimator.SetTrigger("Unhover");
        }
    }
}