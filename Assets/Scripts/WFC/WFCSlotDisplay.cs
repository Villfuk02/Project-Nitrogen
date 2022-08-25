using UnityEngine;

public class WFCSlotDisplay : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Vector2Int slotPos;
    public Gradient entropyGradient;

    void Update()
    {
        WFCSlot slot = WFCGenerator.state.GetSlot(slotPos.x, slotPos.y);
        if (slot.Collapsed != -1)
        {
            spriteRenderer.sprite = WFCGenerator.allModules[slot.Collapsed].sprite;
            spriteRenderer.flipX = WFCGenerator.allModules[slot.Collapsed].flip;
            float brightness = Mathf.Pow(2 / 3f, WorldUtils.MAX_HEIGHT - slot.Height + WFCGenerator.allModules[slot.Collapsed].graphicsHeightOffset);
            spriteRenderer.color = new Color(brightness, brightness, brightness, 1);
            transform.localRotation = Quaternion.Euler(0, 0, -90 * WFCGenerator.allModules[slot.Collapsed].rotate);
        }
        else
        {
            spriteRenderer.color = entropyGradient.Evaluate(slot.TotalEntropy / WFCGenerator.maxEntropy);
        }
    }
}
