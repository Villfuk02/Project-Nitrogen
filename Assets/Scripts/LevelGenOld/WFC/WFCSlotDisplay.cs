using InfiniteCombo.Nitrogen.Assets.Scripts.Utils;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGenOld.WFC
{
    public class WFCSlotDisplay : MonoBehaviour
    {
        public MeshCollider meshCollider;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public Vector2Int slotPos;
        public Gradient entropyGradient;
        public Gradient colorGradient;
        public Mesh defaultMesh;
        public int lastCollapsed;

        void Update()
        {
            WFCSlot slot = WFCGenerator.state.GetSlot(slotPos.x, slotPos.y);
            if (slot.Collapsed != lastCollapsed)
            {
                if (slot.Collapsed != -1)
                {
                    meshFilter.mesh = WFCGenerator.ALL_MODULES[slot.Collapsed].mesh;
                    meshCollider.sharedMesh = meshFilter.mesh;
                    transform.localPosition = WorldUtils.SlotToWorldPos(slotPos.x, slotPos.y, slot.Height - WFCGenerator.ALL_MODULES[slot.Collapsed].graphicsHeightOffset);
                    transform.localScale = new Vector3(WFCGenerator.ALL_MODULES[slot.Collapsed].flip ? -1 : 1, 1, 1) * 1.01f;
                    transform.localRotation = Quaternion.Euler(0, 90 * WFCGenerator.ALL_MODULES[slot.Collapsed].rotate, 0);
                    meshRenderer.material.color = colorGradient.Evaluate(transform.localPosition.y * 0.35f + Random.value * 0.2f);
                }
                else
                {
                    meshFilter.mesh = defaultMesh;
                    transform.localPosition = WorldUtils.SlotToWorldPos(slotPos.x, slotPos.y, -1);
                }
            }
            else if (slot.Collapsed == -1)
            {
                meshRenderer.material.color = entropyGradient.Evaluate(slot.TotalEntropy / WFCGenerator.maxEntropy) + Color.white * (WFCGenerator.IsDirty(slot) ? 0.15f : 0);
            }
            lastCollapsed = slot.Collapsed;
        }
    }
}