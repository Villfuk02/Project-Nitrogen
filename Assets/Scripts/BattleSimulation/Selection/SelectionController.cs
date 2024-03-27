using Game.Blueprint;
using Game.InfoPanel;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Utils;

namespace BattleSimulation.Selection
{
    public class SelectionController : MonoBehaviour
    {
        LayerMask selectionMask_;
        LayerMask coarseTerrainMask_;
        [Header("References")]
        [SerializeField] Camera mainCamera;
        [SerializeField] BlueprintMenu blueprintMenu;
        [SerializeField] InfoPanel infoPanel;
        [SerializeField] PointHighlight pointHighlight;
        [Header("Settings")]
        [SerializeField] float rotationHoldDelay;
        [SerializeField] float rotationInterval;
        [SerializeField] UnityEvent resetVisuals;
        [Header("Runtime variables")]
        public Selectable selected;
        public Selectable hovered;
        public Placement placing;
        public int rotation;
        public Vector3? hoverTilePosition;
        float rotationHoldTime_;
        float lastRotationTime_;

        void Awake()
        {
            selectionMask_ = LayerMask.GetMask(LayerNames.SELECTION);
            coarseTerrainMask_ = LayerMask.GetMask(LayerNames.COARSE_TERRAIN);
        }
        void Update()
        {
            UpdateHover();
            HandleNumberKeys();
            HandleRotation();
            HandleDeselect();

            if (placing != null && placing.Setup(hovered, rotation, hoverTilePosition, transform))
                resetVisuals.Invoke();

            HandleSelectOrPlace();
            HandleDelete();
        }

        void HandleDelete()
        {
            if (Input.GetKeyDown(KeyCode.Delete) && selected != null && selected.tile != null && selected.tile.Building is { permanent: false } b)
                b.Delete();
        }

        void HandleSelectOrPlace()
        {
            if (!Input.GetMouseButtonUp(0) || EventSystem.current.IsPointerOverGameObject())
                return;

            if (placing != null)
            {
                if (placing.IsValid() && blueprintMenu.TryPlace())
                {
                    placing.Place();
                    placing = null;
                    DeselectFromMenu();
                    DeselectInWorld();
                    resetVisuals.Invoke();
                }
                else
                {
                    // TODO: feedback for the player - sound effect
                }
            }
            else if (hovered != null)
            {
                SelectInWorld(hovered);
            }
            else
            {
                DeselectInWorld();
            }
        }

        void HandleDeselect()
        {
            if (!Input.GetMouseButtonUp(1) && !Input.GetKeyDown(KeyCode.Escape))
                return;

            DeselectFromMenu();
            DeselectInWorld();
        }

        void HandleRotation()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                rotation++;
                lastRotationTime_ = 0;
            }


            if (Input.GetKeyUp(KeyCode.R))
            {
                rotationHoldTime_ = 0;
                return;
            }

            if (!Input.GetKey(KeyCode.R))
                return;

            rotationHoldTime_ += Time.deltaTime;
            if (lastRotationTime_ == 0 && rotationHoldTime_ > rotationHoldDelay)
            {
                rotation++;
                lastRotationTime_ = rotationHoldDelay;
            }

            while (lastRotationTime_ >= rotationHoldDelay && rotationHoldTime_ > lastRotationTime_ + rotationInterval)
            {
                rotation++;
                lastRotationTime_ += rotationInterval;
            }
        }

        void HandleNumberKeys()
        {
            for (int i = 0; i < 10; i++)
            {
                KeyCode key = i == 9 ? KeyCode.Alpha0 : KeyCode.Alpha1 + i;
                if (!Input.GetKeyDown(key))
                    continue;
                if (blueprintMenu.selected == i)
                    DeselectFromMenu();
                else
                    SelectFromMenu(i);
            }
        }

        void UpdateHover()
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (!EventSystem.current.IsPointerOverGameObject() && Physics.Raycast(ray, out RaycastHit selectionHit, 100, selectionMask_))
                hovered = selectionHit.transform.GetComponent<Selectable>();
            else
                hovered = null;

            if (!EventSystem.current.IsPointerOverGameObject() && Physics.Raycast(ray, out RaycastHit terrainHit, 100, coarseTerrainMask_))
                hoverTilePosition = WorldUtils.WorldPosToTilePos(terrainHit.point);
            else
                hoverTilePosition = hovered == null ? null : hovered.transform.position;

            if (hoverTilePosition != null)
                pointHighlight.transform.localPosition = WorldUtils.TilePosToWorldPos(hoverTilePosition.Value);
        }

        public void SelectInWorld(Selectable select)
        {
            DeselectFromMenu();
            selected = select;
            resetVisuals.Invoke();

            if (select.tile != null)
            {
                if (select.tile.Building != null)
                    infoPanel.ShowBuilding(select.tile.Building);
                else
                    infoPanel.ShowTile(select.tile);
            }
            else if (select.attacker != null)
            {
                infoPanel.ShowAttacker(select.attacker);
            }
        }

        public void DeselectInWorld()
        {
            selected = null;
            resetVisuals.Invoke();
            infoPanel.Hide();
        }

        public void SelectFromMenu(int index)
        {
            DeselectInWorld();
            DeselectFromMenu();

            if (!blueprintMenu.TrySelect(index, out var blueprint, out var originalBlueprint))
                return;

            placing = Instantiate(blueprint.prefab, transform).GetComponent<Placement>();
            placing.GetComponent<IBlueprinted>().InitBlueprint(blueprint);
            placing.Setup(hovered, rotation, hoverTilePosition, transform);
            resetVisuals.Invoke();
            infoPanel.ShowBlueprint(blueprint, originalBlueprint);
        }

        public void DeselectFromMenu()
        {
            if (placing != null)
                Destroy(placing.gameObject);
            placing = null;
            blueprintMenu.Deselect();
            resetVisuals.Invoke();
            infoPanel.Hide();
        }
    }
}
