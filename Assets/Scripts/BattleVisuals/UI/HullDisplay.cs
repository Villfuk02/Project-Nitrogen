using BattleSimulation.Control;
using Game.Run;
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
        RunPersistence runPersistence_;

        void Awake()
        {
            runPersistence_ = GameObject.FindGameObjectWithTag("RunPersistence").GetComponent<RunPersistence>();
        }

        void Update()
        {
            maxHullText.text = runPersistence_.MaxHull.ToString();

            float targetWidth = runPersistence_.Hull <= 0 ? emptyWidth : Mathf.Lerp(minWidth, maxWidth, runPersistence_.Hull / (float)runPersistence_.MaxHull);
            int backgroundValue = runPersistence_.Hull + Mathf.Max(bc.HullDmgTaken, 0);
            float targetBackgroundWidth = backgroundValue <= 0 ? emptyWidth : Mathf.Lerp(minWidth, maxWidth, backgroundValue / (float)runPersistence_.MaxHull);

            currentWidth = Mathf.Lerp(currentWidth, targetWidth, 10 * Time.deltaTime);
            currentBackgroundWidth = Mathf.Lerp(currentBackgroundWidth, targetBackgroundWidth, 10 * Time.deltaTime);
            fill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentWidth);
            backgroundFill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentBackgroundWidth);

            hullDisplay = runPersistence_.Hull - (runPersistence_.Hull - hullDisplay) * (animationDivisor - 1) / animationDivisor;
            hullText.text = hullDisplay.ToString();
            dmgTakenText.text = (-bc.HullDmgTaken).ToString();
        }
    }
}
