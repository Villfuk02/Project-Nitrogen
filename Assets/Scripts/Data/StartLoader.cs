using Data.LevelGen;
using Data.Loader;
using JetBrains.Annotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Data
{
    public class StartLoader : MonoBehaviour, ILoader
    {
        [CanBeNull] Task loadingTask_;
        ILoader[] loaders_;
        [SerializeField] UnityEvent onLoaded;

        void Start()
        {
            loaders_ = new ILoader[] { new TerrainTypeLoader(Path.Combine(Application.dataPath, "TerrainTypes")) };
            loadingTask_ = LoadAllAsync();
        }

        public Task LoadAllAsync()
        {
            var tasks = loaders_.Select(x => x.LoadAllAsync());
            return Task.WhenAll(tasks);
        }

        void Update()
        {
            if (loadingTask_ is { IsCompletedSuccessfully: true })
            {
                loadingTask_ = null;
                onLoaded.Invoke();
            }
        }
    }
}
