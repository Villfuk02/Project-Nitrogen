using Data.LevelGen;
using Data.Loader;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Data
{
    public class StartLoader : MonoBehaviour
    {
        bool finishedLoading_;
        ILoader[] loaders_;
        [SerializeField] UnityEvent onLoaded;

        void Start()
        {
            loaders_ = new ILoader[] { new TerrainTypeLoader(Path.Combine(Application.dataPath, "TerrainTypes")) };
            StartCoroutine(LoadAll());
        }

        public IEnumerator LoadAll()
        {
            foreach (var loader in loaders_)
            {
                yield return null;
                loader.LoadAll();
            }
            finishedLoading_ = true;
        }

        void Update()
        {
            if (finishedLoading_)
            {
                finishedLoading_ = false;
                Debug.Log("Finished loading");
                onLoaded.Invoke();
            }
        }

        public void ChangeScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
