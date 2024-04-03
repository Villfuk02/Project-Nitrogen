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
        [SerializeField] float rotThreshold;
        [Header("Runtime variables")]
        [SerializeField] float rotation;
        [SerializeField] float rotationVelocity;
        [SerializeField] float rotationTarget;
        [SerializeField] Vector3 camSpacePos;
        [SerializeField] Vector3 camSpacePosTarget;
        [SerializeField] float pitchAngle;
        [SerializeField] Vector3 cursorWorldPos;
        [SerializeField] Vector3 lastZoomCursorPos;
        [SerializeField] Vector3 lastZoomWorldPos;
        [SerializeField] bool dragging;
        [SerializeField] bool rotDragging;
        [SerializeField] float rotDragStartPos;

        void Start()
        {
            distBounds = (Vector2)WorldUtils.WORLD_SIZE * 0.5f;
            camSpacePos.y = maxZoom;
            camSpacePosTarget.y = maxZoom;
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
            HandleDragging();
            HandleKeyboardMovement();
            HandleRotation();
            HandleZoom();
        }

        void HandleDragging()
        {
            var currentCursorWorldPos = ScreenToWorldPos(Input.mousePosition);
            if (dragging && Input.GetMouseButton(1))
                camSpacePosTarget -= 10 * (currentCursorWorldPos - cursorWorldPos);
            else
                dragging = Input.GetMouseButtonDown(1) && !EventSystem.current.IsPointerOverGameObject();

            cursorWorldPos = currentCursorWorldPos;
        }

        void HandleKeyboardMovement()
        {
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
        }

        void HandleRotation()
        {
            var currentCursorXPos = Input.mousePosition.x;
            if (rotDragging && Input.GetMouseButton(2))
            {
                var offset = (currentCursorXPos - rotDragStartPos) / Screen.width;
                if (offset > rotThreshold)
                {
                    rotDragStartPos = currentCursorXPos;
                    rotationTarget += 90;
                }
                else if (offset < -rotThreshold)
                {
                    rotDragStartPos = currentCursorXPos;
                    rotationTarget -= 90;
                }
            }
            else
            {
                rotDragging = Input.GetMouseButtonDown(2) && !EventSystem.current.IsPointerOverGameObject();
                rotDragStartPos = currentCursorXPos;
            }

            if (Input.GetKeyUp(KeyCode.Q))
                rotationTarget += 90;
            if (Input.GetKeyUp(KeyCode.E))
                rotationTarget -= 90;
        }

        void HandleZoom()
        {
            var zoomWorldPos = ScreenToWorldPos(lastZoomCursorPos);
            camSpacePosTarget -= 3 * Mathf.Abs(camSpacePos.y - camSpacePosTarget.y) * (zoomWorldPos - lastZoomWorldPos);
            lastZoomWorldPos = zoomWorldPos;
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                camSpacePosTarget += Input.mouseScrollDelta.y * zoomSpeed * Vector3.up;
                if (Input.mouseScrollDelta.y > 0.01f)
                {
                    lastZoomWorldPos = ScreenToWorldPos(Input.mousePosition);
                    lastZoomCursorPos = Input.mousePosition;
                }
            }
        }

        Vector3 ScreenToWorldPos(Vector3 screenPos)
        {
            var ray = cam.ScreenPointToRay(screenPos);
            return ray.origin - ray.direction * ray.origin.y / ray.direction.y;
        }
    }
}
