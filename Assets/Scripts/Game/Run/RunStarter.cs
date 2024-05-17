using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Run
{
    public class RunStarter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GameObject runPersistencePrefab;
        [Header("Runtime variables")]
        [SerializeField] string seedString;
        [SerializeField] bool selectStartingBlueprints;


        public void StartRun()
        {
            RunPersistence runPersistence = Instantiate(runPersistencePrefab).GetComponent<RunPersistence>();
            runPersistence.seedString = seedString;
            runPersistence.runSeed = SeedEncoder.GetSeedFromString(ref runPersistence.seedString);
            runPersistence.Init(selectStartingBlueprints);
            if (selectStartingBlueprints)
            {
                runPersistence.GetComponentInChildren<BlueprintRewardController>().MakeBlueprintSelection();
            }
            else
            {
                runPersistence.NextLevel();
            }
        }

        public void CustomRun() => SceneManager.LoadScene("Run Settings");

        public void Exit() => Application.Quit();

        public void SetSeedString(string seedString) => this.seedString = seedString;
        public void SetSelectStartingBlueprints(bool selectStartingBlueprints) => this.selectStartingBlueprints = selectStartingBlueprints;
    }
}