using Game.Shared;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BattleVisuals.Camera
{
    public class BattleCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] BattleCameraTransform mainCamera;
        [SerializeField] BattleCameraTransform targetCamera;
        [Header("Settings")]
        [SerializeField] float cameraHeight;
        [SerializeField] Vector2 distBounds;
        [SerializeField] float minZoom;
        [SerializeField] float maxZoom;
        [SerializeField] float minAngle;
        [SerializeField] float maxAngle;
        [SerializeField] float moveSpeed;
        [SerializeField] float scrollZoomSpeed;
        [SerializeField] float keysZoomSpeed;
        [SerializeField] float interpolationSpeed;
        [SerializeField] float rotAcceleration;
        [SerializeField] float rotInertia;
        [SerializeField] float rotationDragThreshold;
        [Header("Runtime variables")]
        [SerializeField] bool dragging;
        [SerializeField] Vector3 dragWorldPos;
        [SerializeField] float rotationVelocity;
        [SerializeField] bool rotationDragging;
        [SerializeField] float rotationDragStartPos;

        void Start()
        {
            targetCamera.cameraHeight = cameraHeight;
            SetTargetCamSpacePosition(targetCamera.camSpacePos);

            mainCamera.camSpacePos = targetCamera.camSpacePos;
            mainCamera.pitchAngle = targetCamera.pitchAngle;
            mainCamera.rotation = targetCamera.rotation;
            mainCamera.cameraHeight = targetCamera.cameraHeight;
            mainCamera.UpdateCameraTransform();
        }

        void Update()
        {
            UpdateTargetCameraAspectRatio();
            HandleInputs();
            Interpolate();
        }

        void UpdateTargetCameraAspectRatio()
        {
            targetCamera.camera.aspect = mainCamera.camera.aspect;
        }

        void Interpolate()
        {
            mainCamera.camSpacePos = Vector3.Lerp(mainCamera.camSpacePos, targetCamera.camSpacePos, Time.deltaTime * interpolationSpeed);
            mainCamera.pitchAngle = Mathf.Lerp(minAngle, maxAngle, (mainCamera.camSpacePos.y - minZoom) / (maxZoom - minZoom));
            rotationVelocity *= Mathf.Pow(rotInertia, Time.deltaTime);
            rotationVelocity += (targetCamera.rotation - mainCamera.rotation) * rotAcceleration;
            mainCamera.rotation += rotationVelocity * Time.deltaTime;

            mainCamera.UpdateCameraTransform();
        }

        void HandleInputs()
        {
            HandleDragging();
            if (!dragging)
                HandleKeyboardMovement();
            HandleRotation();
            HandleZoom();
        }


        void HandleDragging()
        {
            if (dragging && Input.GetMouseButton(1))
            {
                AlignPositionToCursor(dragWorldPos, false);
            }
            else
            {
                dragging = Input.GetMouseButtonDown(1) && !EventSystem.current.IsPointerOverGameObject();
                if (dragging)
                    dragWorldPos = CursorToWorldPos(mainCamera.camera, float.NaN);
            }
        }

        void AlignPositionToCursor(Vector3 worldPos, bool onlyAlignTarget)
        {
            Vector3 currentCursorWorldPos = CursorToWorldPos(targetCamera.camera, worldPos.y);
            Vector3 change = MoveTargetInCamSpace(worldPos - currentCursorWorldPos);
            if (!onlyAlignTarget)
                mainCamera.camSpacePos += change;
        }

        void HandleKeyboardMovement()
        {
            Vector2Int inputDirection = Vector2Int.zero;
            if (Input.GetKey(KeyCode.W))
                inputDirection += Vector2Int.up;
            if (Input.GetKey(KeyCode.S))
                inputDirection += Vector2Int.down;
            if (Input.GetKey(KeyCode.A))
                inputDirection += Vector2Int.left;
            if (Input.GetKey(KeyCode.D))
                inputDirection += Vector2Int.right;

            if (inputDirection == Vector2Int.zero)
                return;
            float moveDistance = moveSpeed * Time.deltaTime * mainCamera.camSpacePos.y;
            MoveTargetInCamSpace(moveDistance * (Quaternion.Euler(0, mainCamera.rotation, 0) * new Vector3(inputDirection.x, 0, inputDirection.y)));
        }

        void HandleRotation()
        {
            var currentCursorXPos = Input.mousePosition.x;
            if (rotationDragging && Input.GetMouseButton(2))
            {
                var offset = (currentCursorXPos - rotationDragStartPos) / Screen.width;
                if (offset > rotationDragThreshold)
                {
                    rotationDragStartPos = currentCursorXPos;
                    RotateTarget(90);
                }
                else if (offset < -rotationDragThreshold)
                {
                    rotationDragStartPos = currentCursorXPos;
                    RotateTarget(-90);
                }
            }
            else
            {
                rotationDragging = Input.GetMouseButtonDown(2) && !EventSystem.current.IsPointerOverGameObject();
                rotationDragStartPos = currentCursorXPos;
            }

            if (Input.GetKeyUp(KeyCode.Q))
                RotateTarget(90);
            if (Input.GetKeyUp(KeyCode.E))
                RotateTarget(-90);
        }

        void HandleZoom()
        {
            float zoomChange = 0;
            if (!EventSystem.current.IsPointerOverGameObject() && Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f)
                zoomChange += Input.mouseScrollDelta.y * scrollZoomSpeed;
            if (Input.GetKey(KeyCode.T))
                zoomChange += Time.deltaTime * keysZoomSpeed;
            if (Input.GetKey(KeyCode.G))
                zoomChange -= Time.deltaTime * keysZoomSpeed;

            if (zoomChange != 0)
            {
                var prevWorldPos = CursorToWorldPos(mainCamera.camera, float.NaN);
                UpdateTargetZoom(zoomChange);
                AlignPositionToCursor(prevWorldPos, true);
            }
        }


        void UpdateTargetZoom(float change) => SetTargetZoom(targetCamera.camSpacePos.y + change);

        void SetTargetZoom(float zoom) => SetTargetCamSpacePosition(new(targetCamera.camSpacePos.x, zoom, targetCamera.camSpacePos.z));

        Vector3 MoveTargetInCamSpace(Vector3 move)
        {
            Vector3 prevPos = targetCamera.camSpacePos;
            SetTargetCamSpacePosition(targetCamera.camSpacePos + move);
            return targetCamera.camSpacePos - prevPos;
        }

        void SetTargetCamSpacePosition(Vector3 pos)
        {
            targetCamera.camSpacePos = new(
                Mathf.Clamp(pos.x, -distBounds.x, distBounds.x),
                Mathf.Clamp(pos.y, minZoom, maxZoom),
                Mathf.Clamp(pos.z, -distBounds.y, distBounds.y)
            );
            targetCamera.pitchAngle = Mathf.Lerp(minAngle, maxAngle, (targetCamera.camSpacePos.y - minZoom) / (maxZoom - minZoom));
            targetCamera.UpdateCameraTransform();
        }

        void RotateTarget(float amount)
        {
            targetCamera.rotation += amount;
            targetCamera.UpdateCameraTransform();
            if (dragging)
                AlignPositionToCursor(dragWorldPos, true);
        }

        /// <summary>
        /// Gets the approximate worldPosition the mouse cursor is over.
        /// If intersectionPlaneHeight is NaN, the intersection plane is determined automatically.
        /// </summary>
        Vector3 CursorToWorldPos(UnityEngine.Camera cam, float intersectionPlaneHeight)
        {
            var ray = cam.ScreenPointToRay(Input.mousePosition);

            if (float.IsNaN(intersectionPlaneHeight))
            {
                if (Physics.Raycast(ray, out RaycastHit selectionHit, 100, LayerMasks.coarseTerrain))
                    return selectionHit.point;
                intersectionPlaneHeight = 0;
            }

            return ray.origin - ray.direction * (ray.origin.y - intersectionPlaneHeight) / ray.direction.y;
        }
    }
}