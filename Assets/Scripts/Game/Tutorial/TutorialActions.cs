using BattleSimulation.Control;
using BattleSimulation.Selection;
using BattleSimulation.World.WorldBuilder;
using BattleVisuals.Camera;
using BattleVisuals.Selection;
using Coffee.UISoftMask;
using Game.Run;
using Game.Shared;
using TMPro;
using UnityEngine;
using Utils;

namespace Game.Tutorial
{
    public class TutorialActions : MonoBehaviour
    {
        static readonly int Show = Animator.StringToHash("Show");
        static readonly int Hide = Animator.StringToHash("Hide");
        [Header("References")]
        [SerializeField] TutorialController controller;
        [SerializeField] TextMeshProUGUI tutorialText;
        [SerializeField] Animator tutorialTextAnimator;
        [SerializeField] WorldBuilder worldBuilder;
        [SerializeField] Blueprint.Blueprint budgetSentryBlueprint;
        [SerializeField] Blueprint.Blueprint surfaceDrillBlueprint;
        [SerializeField] GameObject surfaceDrillWithoutFuel;
        [SerializeField] Blueprint.Blueprint grenadeBlueprint;
        [SerializeField] SoftMask wavesMask;
        [SerializeField] GameObject wavesLabels;
        [SerializeField] BattleCameraTransform cameraTransform;
        [SerializeField] WaveController waveController;
        [SerializeField] BlueprintMenu blueprintMenu;
        [SerializeField] BlueprintMenuDisplay blueprintMenuDisplay;
        [SerializeField] BattleController battleController;
        [SerializeField] InfoPanel.InfoPanel infoPanel;
        [SerializeField] TextMeshProUGUI infoPanelTitle;
        [Header("Runtime variables")]
        [SerializeField] bool shown;
        [SerializeField] string newTutorialText;

        void Start()
        {
            WaveController.START_WAVE.RegisterReaction(OnWaveStarted, 1);
            WaveController.ON_WAVE_FINISHED.RegisterReaction(OnWaveFinished, 1);
        }

        void OnDestroy()
        {
            WaveController.START_WAVE.UnregisterReaction(OnWaveStarted);
            WaveController.ON_WAVE_FINISHED.UnregisterReaction(OnWaveFinished);
        }

        void Update()
        {
            if (cameraTransform.camSpacePos.XZ().sqrMagnitude > 4)
                controller.TriggerPhase(2);
            if (cameraTransform.camSpacePos.y < 5.5f)
                controller.TriggerPhase(3);
            if (Mathf.Abs(cameraTransform.rotation - Mathf.RoundToInt(cameraTransform.rotation)) > 0.1f)
                controller.TriggerPhase(4);
            if (battleController.material == 0)
                controller.TriggerPhase(9);
            if (battleController.material == 0)
                controller.TriggerPhase(12);
            if (!infoPanel.visible)
                controller.TriggerPhase(18);
            else if (infoPanelTitle.text == "Big Skull")
                controller.TriggerPhase(21);
        }

        void OnWaveStarted()
        {
            switch (waveController.wave)
            {
                case 1:
                    controller.TriggerPhase(5);
                    break;
                case 2:
                    controller.TriggerPhase(7);
                    break;
                case 3:
                    controller.TriggerPhase(10);
                    break;
                case 4:
                    controller.TriggerPhase(13);
                    break;
                case 5:
                    controller.TriggerPhase(15);
                    break;
                case 6:
                    controller.TriggerPhase(17);
                    break;
                case 8:
                    controller.TriggerPhase(20);
                    break;
            }
        }

        void OnWaveFinished()
        {
            switch (waveController.wave)
            {
                case 1:
                    controller.TriggerPhase(6);
                    break;
                case 2:
                    controller.TriggerPhase(8);
                    break;
                case 3:
                    controller.TriggerPhase(11);
                    break;
                case 4:
                    controller.TriggerPhase(14);
                    break;
                case 5:
                    controller.TriggerPhase(16);
                    break;
                case 6:
                    controller.TriggerPhase(18);
                    break;
                case 7:
                    controller.TriggerPhase(19);
                    break;
                case 10:
                    controller.TriggerPhase(22);
                    break;
            }
        }

        public void ShowTutorialText(string text)
        {
            tutorialTextAnimator.SetTrigger(Show);
            newTutorialText = text;
            if (shown)
                Invoke(nameof(ChangeTutorialText), 0.5f);
            else
                ChangeTutorialText();
            shown = true;
        }

        void ChangeTutorialText()
        {
            tutorialText.text = newTutorialText;
            SoundController.PlaySound(SoundController.Sound.Notification, 0.5f, 1, 0, null, SoundController.Priority.High);
        }

        public void HideTutorialText()
        {
            tutorialTextAnimator.SetTrigger(Hide);
            shown = false;
        }

        public void HideTutorialTextAfterAWhile()
        {
            Invoke(nameof(HideTutorialText), 2.5f);
        }

        public void ModifyHubBlueprint()
        {
            var blueprint = worldBuilder.hubBlueprint.Clone();
            blueprint.name += " (Incomplete)";
            blueprint.materialProduction = -1;
            blueprint.energyProduction = -1;
            worldBuilder.hubBlueprint = blueprint;
        }

        public void PlaceBudgetSentry()
        {
            worldBuilder.PlacePermanentBuilding(budgetSentryBlueprint, new(4, 8));
        }

        public void HideWavesPreview()
        {
            wavesMask.alpha = 0;
            wavesLabels.SetActive(false);
        }

        public void ShowWavesPreview()
        {
            wavesMask.alpha = 1;
            wavesLabels.SetActive(true);
        }

        public void SetDisableNextWave(bool disableNextWave)
        {
            waveController.disableNextWave = disableNextWave;
        }

        public void AddSentryBlueprint()
        {
            blueprintMenu.buildings.Add(new(budgetSentryBlueprint.Clone(), 0));
            blueprintMenuDisplay.InitItems();
        }

        public void RemoveResources()
        {
            battleController.material = 0;
            battleController.energy = 0;
        }

        public void AddMaterials(int materials)
        {
            battleController.material += materials;
        }

        public void AddDrillBlueprint()
        {
            var blueprint = surfaceDrillBlueprint.Clone();
            blueprint.prefab = surfaceDrillWithoutFuel;
            blueprintMenu.buildings.Add(new(blueprint, 1));
            blueprintMenu.buildings[0].cooldown = 2;
            blueprintMenuDisplay.InitItems();
        }

        public void AddEnergy(int energy)
        {
            battleController.energy += energy;
        }

        public void AddGrenadeBlueprint()
        {
            blueprintMenu.abilities.Add(new(grenadeBlueprint.Clone(), 0));
            blueprintMenuDisplay.InitItems();
        }

        public void FixDrillBlueprint()
        {
            blueprintMenu.buildings[1] = new(surfaceDrillBlueprint.Clone(), 1);
            blueprintMenuDisplay.InitItems();
        }

        public void SetupRunBlueprints()
        {
            GameObject.FindGameObjectWithTag(TagNames.RUN_PERSISTENCE).GetComponent<RunPersistence>().blueprints = new() { surfaceDrillBlueprint, budgetSentryBlueprint, grenadeBlueprint };
        }
    }
}