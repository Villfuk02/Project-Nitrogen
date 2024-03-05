using UnityEngine;

namespace BattleVisuals
{
    public class CanvasFaceCamera : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Camera cam;
        void Start()
        {
            if (cam == null)
                cam = Camera.main;
        }

        void Update()
        {
            transform.rotation = cam.transform.rotation;
        }
    }
}
