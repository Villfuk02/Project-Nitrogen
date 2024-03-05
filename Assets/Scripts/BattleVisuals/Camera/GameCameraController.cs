using UnityEngine;
using UnityEngine.EventSystems;
using Utils;

namespace BattleVisuals.Camera
{
    public class GameCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] UnityEngine.Camera cam;
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
        [Header("Runtime variables")]
        [SerializeField] float rotation;
        [SerializeField] float rotationVelocity;
        [SerializeField] float rotationTarget;
        [SerializeField] Vector3 camSpacePos;
        [SerializeField] Vector3 camSpacePosTarget;
        [SerializeField] float pitchAngle;

        void Start()
        {
            distBounds = (Vector2)WorldUtils.WORLD_SIZE * 0.5f;
        }

        void Update()
        {
            HandleInputs();
            Clamp();
            Interpolate();
            Apply();
        }

        void Apply()
        {
            float rotationRad = rotation * Mathf.Deg2Rad;
            transform.localPosition = new Vector3(camSpacePos.x, transform.localPosition.y, camSpacePos.z)
                                      - new Vector3(Mathf.Sin(rotationRad), 0, Mathf.Cos(rotationRad)) * camHeight / Mathf.Tan(pitchAngle / 180 * Mathf.PI);
            transform.localRotation = Quaternion.Euler(pitchAngle, rotation, 0);
            cam.orthographicSize = camSpacePos.y;
        }

        void Interpolate()
        {
            camSpacePos = Vector3.Lerp(camSpacePos, camSpacePosTarget, Time.deltaTime * interpolationSpeed);
            pitchAngle = Mathf.Lerp(minAngle, maxAngle, (camSpacePos.y - minZoom) / (maxZoom - minZoom));
            rotationVelocity *= Mathf.Pow(rotInertia, Time.deltaTime);
            rotationVelocity += (rotationTarget - rotation) * rotAcceleration;
            rotation += rotationVelocity * Time.deltaTime;
        }

        void Clamp()
        {
            camSpacePosTarget = new(
                Mathf.Clamp(camSpacePosTarget.x, -distBounds.x, distBounds.x),
                Mathf.Clamp(camSpacePosTarget.y, minZoom, maxZoom),
                Mathf.Clamp(camSpacePosTarget.z, -distBounds.y, distBounds.y));
        }

        void HandleInputs()
        {
            if (Input.GetKeyUp(KeyCode.Q))
                rotationTarget += 90;
            if (Input.GetKeyUp(KeyCode.E))
                rotationTarget -= 90;

            float realMove = moveSpeed * Time.deltaTime * camSpacePos.y;
            Vector2Int inputDirection = Vector2Int.zero;
            if (Input.GetKey(KeyCode.W))
                inputDirection += Vector2Int.up;
            if (Input.GetKey(KeyCode.S))
                inputDirection += Vector2Int.down;
            if (Input.GetKey(KeyCode.A))
                inputDirection += Vector2Int.left;
            if (Input.GetKey(KeyCode.D))
                inputDirection += Vector2Int.right;
            camSpacePosTarget += realMove * (Quaternion.Euler(0, rotation, 0) * new Vector3(inputDirection.x, 0, inputDirection.y));

            if (!EventSystem.current.IsPointerOverGameObject())
                camSpacePosTarget += Input.mouseScrollDelta.y * zoomSpeed * Vector3.up;
        }
    }
}
