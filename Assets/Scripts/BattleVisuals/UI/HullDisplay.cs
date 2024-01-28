using BattleSimulation.Control;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleVisuals.UI
{
    public class HullDisplay : MonoBehaviour
    {
        [SerializeField] BattleController bc;
        [SerializeField] Image fill;
        [SerializeField] Image backgroundFill;
        [SerializeField] TextMeshProUGUI hullText;
        [SerializeField] TextMeshProUGUI maxHullText;
        [SerializeField] TextMeshProUGUI dmgTakenText;
        [SerializeField] int animationDivisor;
        [SerializeField] int hullDisplay;
        [SerializeField] float emptyWidth;
        [SerializeField] float minWidth;
        [SerializeField] float maxWidth;
        [SerializeField] float currentWidth;
        [SerializeField] float currentBackgroundWidth;

        void Update()
        {
            maxHullText.text = BattleController.runPersistence.MaxHull.ToString();

            float targetWidth = BattleController.runPersistence.Hull <= 0 ? emptyWidth : Mathf.Lerp(minWidth, maxWidth, BattleController.runPersistence.Hull / (float)BattleController.runPersistence.MaxHull);
            int backgroundValue = BattleController.runPersistence.Hull + Mathf.Max(bc.HullDmgTaken, 0);
            float targetBackgroundWidth = backgroundValue <= 0 ? emptyWidth : Mathf.Lerp(minWidth, maxWidth, backgroundValue / (float)BattleController.runPersistence.MaxHull);

            currentWidth = Mathf.Lerp(currentWidth, targetWidth, 10 * Time.deltaTime);
            currentBackgroundWidth = Mathf.Lerp(currentBackgroundWidth, targetBackgroundWidth, 10 * Time.deltaTime);
            fill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentWidth);
            backgroundFill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentBackgroundWidth);

            hullDisplay = BattleController.runPersistence.Hull - (BattleController.runPersistence.Hull - hullDisplay) * (animationDivisor - 1) / animationDivisor;
            hullText.text = hullDisplay.ToString();
            dmgTakenText.text = (-bc.HullDmgTaken).ToString();
        }
    }
}
