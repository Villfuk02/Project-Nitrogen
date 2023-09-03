using UnityEngine;

namespace Attackers.Visuals
{
    public class CanvasFaceCamera : MonoBehaviour
    {
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
