using BattleSimulation.Control;
using Game.Run;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace BattleVisuals.UI
{
    public class HullDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] BattleController bc;
        [SerializeField] Image fill;
        [SerializeField] Image backgroundFill;
        [SerializeField] TextMeshProUGUI hullText;
        [SerializeField] TextMeshProUGUI maxHullText;
        [SerializeField] TextMeshProUGUI dmgTakenText;
        [Header("Settings")]
        [SerializeField] int convergenceDivisor;
        [SerializeField] float emptyWidth;
        [SerializeField] float minWidth;
        [SerializeField] float maxWidth;
        [Header("Runtime variables")]
        [SerializeField] int hullDisplay;
        [SerializeField] float currentWidth;
        [SerializeField] float currentBackgroundWidth;
        RunPersistence runPersistence_;

        void Awake()
        {
            runPersistence_ = GameObject.FindGameObjectWithTag(TagNames.RUN_PERSISTENCE).GetComponent<RunPersistence>();
        }

        void Update()
        {
            UpdateFill();
            UpdateBackgroundFill();
            UpdateTexts();
        }

        void UpdateTexts()
        {
            maxHullText.text = runPersistence_.maxHull.ToString();
            MathUtils.StepTowards(ref hullDisplay, runPersistence_.hull, convergenceDivisor);
            hullText.text = hullDisplay.ToString();
            dmgTakenText.text = (-bc.hullDmgTaken).ToString();
        }

        void UpdateBackgroundFill()
        {
            int previousHull = runPersistence_.hull + Mathf.Max(bc.hullDmgTaken, 0);
            float targetBackgroundWidth = previousHull <= 0 ? emptyWidth : Mathf.Lerp(minWidth, maxWidth, previousHull / (float)runPersistence_.maxHull);
            currentBackgroundWidth = Mathf.Lerp(currentBackgroundWidth, targetBackgroundWidth, 10 * Time.deltaTime);
            backgroundFill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentBackgroundWidth);
        }

        void UpdateFill()
        {
            float targetWidth = runPersistence_.hull <= 0 ? emptyWidth : Mathf.Lerp(minWidth, maxWidth, runPersistence_.hull / (float)runPersistence_.maxHull);
            currentWidth = Mathf.Lerp(currentWidth, targetWidth, 10 * Time.deltaTime);
            fill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentWidth);
        }
    }
}