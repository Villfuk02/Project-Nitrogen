using Game.Blueprint;
using Game.InfoPanel;
using System.Linq;
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
        bool isSelectedBuilding_;

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
            if (Input.GetKeyDown(KeyCode.Delete) && selected != null && selected.tile != null && selected.tile.Building != null && selected.tile.Building is { permanent: false } b)
                b.Delete();
            if (isSelectedBuilding_ && selected != null && selected.tile != null && selected.tile.Building == null)
                DeselectInWorld();
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
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                DeselectFromMenu();
                DeselectInWorld();
            }
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
            Selectable newHover;
            if (!EventSystem.current.IsPointerOverGameObject() && Physics.Raycast(ray, out RaycastHit selectionHit, 100, selectionMask_))
                newHover = selectionHit.transform.GetComponent<Selectable>();
            else
                newHover = null;

            if (newHover != hovered)
            {
                hovered = newHover;
                if (hovered == null)
                    infoPanel.Hide(false, false);
                else
                    DisplayInfoSelectedInWorld(hovered, true);
            }

            if (!EventSystem.current.IsPointerOverGameObject() && Physics.Raycast(ray, out RaycastHit terrainHit, 100, coarseTerrainMask_))
                hoverTilePosition = WorldUtils.WorldPosToTilePos(terrainHit.point);
            else
                hoverTilePosition = hovered == null ? null : hovered.transform.position;

            if (hoverTilePosition != null)
            {
                Vector3 pointHighlightPos = hoverTilePosition.Value;
                float maxHeightInArea = WorldUtils.CARDINAL_DIRS.Select(d => World.WorldData.World.data.tiles.GetHeightAt((Vector2)pointHighlightPos + 0.2f * (Vector2)d)).Max();
                pointHighlightPos.z = maxHeightInArea;
                pointHighlight.transform.localPosition = WorldUtils.TilePosToWorldPos(pointHighlightPos);
            }
        }

        public void SelectInWorld(Selectable select)
        {
            DeselectFromMenu();
            selected = select;
            resetVisuals.Invoke();
            isSelectedBuilding_ = select.tile != null && select.tile.Building != null;

            DisplayInfoSelectedInWorld(select, false);
        }

        void DisplayInfoSelectedInWorld(Selectable select, bool hover)
        {
            if (select.tile != null)
            {
                if (select.tile.Building != null)
                    infoPanel.ShowBlueprinted(select.tile.Building, null, !hover, !hover);
                else
                    infoPanel.ShowTile(select.tile, !hover, !hover);
            }
            else if (select.attacker != null)
            {
                infoPanel.ShowAttacker(select.attacker, !hover, !hover);
            }
        }

        public void DeselectInWorld()
        {
            if (selected != null)
            {
                resetVisuals.Invoke();
                infoPanel.Hide(true, true);
            }

            selected = null;
            isSelectedBuilding_ = false;
        }

        public void HoverFromMenu(int index)
        {
            blueprintMenu.GetBlueprints(index, out var blueprint, out var original, out var cooldown);
            infoPanel.ShowBlueprint(blueprint, original, cooldown, true, false);
        }

        public void UnhoverFromMenu()
        {
            infoPanel.Hide(true, false);
        }

        public void SelectFromMenu(int index)
        {
            bool onlyDeselect = blueprintMenu.selected == index;

            DeselectInWorld();
            DeselectFromMenu();

            if (onlyDeselect)
                return;

            if (!blueprintMenu.TrySelect(index, out var blueprint, out _, out var cooldown))
                return;

            placing = Instantiate(blueprint.prefab, transform).GetComponent<Placement>();
            placing.GetComponent<IBlueprinted>().InitBlueprint(blueprint);
            placing.Setup(hovered, rotation, hoverTilePosition, transform);
            resetVisuals.Invoke();
            infoPanel.ShowBlueprinted(placing.GetComponent<IBlueprinted>(), cooldown, true, true);
        }

        public void DeselectFromMenu()
        {
            if (placing != null)
            {
                Destroy(placing.gameObject);
                resetVisuals.Invoke();
                placing = null;
            }

            infoPanel.Hide(true, true);
            blueprintMenu.Deselect();
        }
    }
}
