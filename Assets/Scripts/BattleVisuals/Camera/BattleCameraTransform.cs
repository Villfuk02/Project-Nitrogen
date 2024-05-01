using UnityEngine;
using Utils;

namespace BattleVisuals.Camera
{
    public class BattleCameraTransform : MonoBehaviour
    {
        [Header("References")]
        public new UnityEngine.Camera camera;
        [Header("Runtime variables")]
        public float rotation;
        public Vector3 camSpacePos;
        public float pitchAngle;
        public float cameraHeight;

        public void UpdateCameraTransform()
        {
            float rotationRad = rotation * Mathf.Deg2Rad;
            camera.transform.localPosition = new Vector3(camSpacePos.x, cameraHeight, camSpacePos.z)
                                             - new Vector3(Mathf.Sin(rotationRad), 0, Mathf.Cos(rotationRad)) * cameraHeight / Mathf.Tan(pitchAngle * Mathf.Deg2Rad);
            camera.transform.localRotation = Quaternion.Euler(pitchAngle, rotation, 0);
            camera.orthographicSize = camSpacePos.y;
        }
    }
}