using Data.Loader;
using Data.WorldGen;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

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
            if (!finishedLoading_)
                return;

            finishedLoading_ = false;
            print("Finished loading");
            onLoaded.Invoke();
        }
    }
}
