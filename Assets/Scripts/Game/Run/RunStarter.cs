using Game.Run.Shared;
using UnityEngine;

namespace Game.Run
{
    public class RunStarter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GameObject runPersistencePrefab;
        [Header("Runtime variables")]
        [SerializeField] string seedString;
        [SerializeField] bool selectStartingBlueprints;
        [SerializeField] bool playTutorial;

        public void StartTutorial()
        {
            playTutorial = true;
            StartRun();
        }

        public void StartRunDefault()
        {
            if (!PersistentData.FinishedTutorial)
                playTutorial = true;
            StartRun();
        }

        public void StartRun()
        {
            RunPersistence runPersistence = Instantiate(runPersistencePrefab).GetComponent<RunPersistence>();

            if (playTutorial)
            {
                seedString = "TUTO RIAL";
                selectStartingBlueprints = false;
                runPersistence.level = -1;
                PersistentData.ClearProgress();
            }

            runPersistence.seedString = seedString;
            runPersistence.runSeed = SeedEncoder.GetSeedFromString(ref runPersistence.seedString);
            runPersistence.Init(playTutorial || selectStartingBlueprints);
            if (selectStartingBlueprints)
            {
                runPersistence.GetComponentInChildren<BlueprintRewardController>().MakeBlueprintSelection();
            }
            else
            {
                runPersistence.NextLevel();
            }
        }

        public void CustomRun() => SceneController.ChangeScene(SceneController.Scene.RunSettings, true, true);

        public void Exit() => Application.Quit();

        public void SetSeedString(string seedString) => this.seedString = seedString;
        public void SetSelectStartingBlueprints(bool selectStartingBlueprints) => this.selectStartingBlueprints = selectStartingBlueprints;
    }
}