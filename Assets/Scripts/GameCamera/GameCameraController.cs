using Assets.Scripts.Utils;
using UnityEngine;

namespace Assets.Scripts.GameCamera
{
    public class GameCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Camera cam;
        [Header("Settings")]
        [SerializeField] Vector2 distBounds;
        [SerializeField] float moveSpeed;
        [SerializeField] float zoomSpeed;
        [SerializeField] float minZoom;
        [SerializeField] float maxZoom;
        [SerializeField] float minAngle;
        [SerializeField] float maxAngle;
        [SerializeField] float camHeight;
        [SerializeField] float interpolationSpeed;
        [SerializeField] float rotAcceleration;
        [SerializeField] float rotInertia;
        [Header("Runtime")]
        [SerializeField] float rotation;
        [SerializeField] float rotationVel;
        [SerializeField] float rotationTarget;
        [SerializeField] Vector3 camSpacePos;
        [SerializeField] Vector3 camSpacePosTarget;
        [SerializeField] float angle;

        void Start()
        {
            distBounds = (Vector2)WorldUtils.WORLD_SIZE * 0.5f;
        }

        void Update()
        {
            // inputs
            if (Input.GetKeyUp(KeyCode.Q))
                rotationTarget += 90;
            if (Input.GetKeyUp(KeyCode.E))
                rotationTarget -= 90;
            float rotationRad = rotation / 180 * Mathf.PI;
            float realMove = moveSpeed * Time.deltaTime * camSpacePos.y;
            if (Input.GetKey(KeyCode.W))
                camSpacePosTarget += realMove * new Vector3(Mathf.Sin(rotationRad), 0, Mathf.Cos(rotationRad));
            if (Input.GetKey(KeyCode.A))
                camSpacePosTarget += realMove * new Vector3(-Mathf.Cos(rotationRad), 0, Mathf.Sin(rotationRad));
            if (Input.GetKey(KeyCode.S))
                camSpacePosTarget += realMove * new Vector3(-Mathf.Sin(rotationRad), 0, -Mathf.Cos(rotationRad));
            if (Input.GetKey(KeyCode.D))
                camSpacePosTarget += realMove * new Vector3(Mathf.Cos(rotationRad), 0, -Mathf.Sin(rotationRad));
            camSpacePosTarget += Input.mouseScrollDelta.y * zoomSpeed * Vector3.up;

            // limits
            camSpacePosTarget = new(
                Mathf.Clamp(camSpacePosTarget.x, -distBounds.x, distBounds.x),
                Mathf.Clamp(camSpacePosTarget.y, minZoom, maxZoom),
                Mathf.Clamp(camSpacePosTarget.z, -distBounds.y, distBounds.y));

            // update
            camSpacePos = Vector3.Lerp(camSpacePos, camSpacePosTarget, Time.deltaTime * interpolationSpeed);
            angle = Mathf.Lerp(minAngle, maxAngle, (camSpacePos.y - minZoom) / (maxZoom - minZoom));
            rotationVel *= Mathf.Pow(rotInertia, Time.deltaTime);
            rotationVel += (rotationTarget - rotation) * rotAcceleration;
            rotation += rotationVel * Time.deltaTime;

            // apply
            rotationRad = rotation / 180 * Mathf.PI;
            transform.localPosition = new Vector3(camSpacePos.x, transform.localPosition.y, camSpacePos.z)
                - new Vector3(Mathf.Sin(rotationRad), 0, Mathf.Cos(rotationRad)) * camHeight / Mathf.Tan(angle / 180 * Mathf.PI);
            transform.localRotation = Quaternion.Euler(angle, rotation, 0);
            cam.orthographicSize = camSpacePos.y;
        }
    }
}
