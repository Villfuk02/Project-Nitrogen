using Game.Blueprint;
using Game.InfoPanel;
using UnityEngine;
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
        [Header("Runtime values")]
        public Selectable selected;
        public Selectable hovered;
        public Placement placing;
        public int rotation;
        public Vector3? hoverTilePosition;
        public bool resetVisuals;

        void Awake()
        {
            selectionMask_ = LayerMask.GetMask(LayerNames.SELECTION);
            coarseTerrainMask_ = LayerMask.GetMask(LayerNames.COARSE_TERRAIN);
        }
        void Update()
        {
            // mouse movement
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (!EventSystem.current.IsPointerOverGameObject() && Physics.Raycast(ray, out RaycastHit selectionHit, 100, selectionMask_))
                hovered = selectionHit.transform.GetComponent<Selectable>();
            else
                hovered = null;

            if (!EventSystem.current.IsPointerOverGameObject() && Physics.Raycast(ray, out RaycastHit terrainHit, 100, coarseTerrainMask_))
                hoverTilePosition = WorldUtils.WorldPosToTilePos(terrainHit.point);
            else
                hoverTilePosition = hovered == null ? null : hovered.transform.position;

            // num keys
            for (int i = 0; i < 9; i++)
            {
                KeyCode key = KeyCode.Alpha1 + i;
                if (!Input.GetKeyDown(key))
                    continue;
                // replace with select / unavailable / deselect based on blueprintMenu state
                if (blueprintMenu.selected == i)
                    DeselectFromMenu();
                else
                    SelectFromMenu(i);
            }

            // R
            if (Input.GetKeyDown(KeyCode.R))
                rotation++; //TODO: press and hold

            // right click
            if (Input.GetMouseButtonUp(1))
            {
                DeselectFromMenu();
                DeselectInWorld();
            }

            if (placing != null)
                resetVisuals |= placing.Setup(hovered, rotation, hoverTilePosition, transform);

            //left click
            if (Input.GetMouseButtonUp(0))
            {
                if (placing != null)
                {
                    if (placing.IsValid() && blueprintMenu.OnPlace())
                    {
                        placing.Place();
                        placing = null;
                        DeselectFromMenu();
                        DeselectInWorld();
                        resetVisuals = true;
                    }
                    else
                    {
                        // TODO: feedback for the player
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

            //destroy selected building
            if (Input.GetKeyDown(KeyCode.Delete) && selected != null && selected.tile != null && selected.tile.Building is { permanent: false } b)
                b.Delete();
        }

        public void SelectInWorld(Selectable select)
        {
            DeselectFromMenu();
            selected = select;
            resetVisuals = true;

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
            resetVisuals = true;
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
            resetVisuals = true;
            infoPanel.ShowBlueprint(blueprint, originalBlueprint);
        }

        public void DeselectFromMenu()
        {
            if (placing != null)
                Destroy(placing.gameObject);
            placing = null;
            blueprintMenu.Deselect();
            resetVisuals = true;
            infoPanel.Hide();
        }
    }
}
