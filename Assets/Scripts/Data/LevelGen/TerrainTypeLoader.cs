using Data.Loader;
using Data.Parsers;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Data.LevelGen
{
    public class TerrainTypeLoader : ILoader
    {
        static readonly string FileExtension = "txt";
        readonly string path_;

        public TerrainTypeLoader(string path)
        {
            path_ = path;
        }

        public Task LoadAllAsync()
        {
            foreach (var fileName in Directory.GetFiles(path_, $"*.{FileExtension}"))
            {
                PreprocessedParseStream s = new(Path.Combine(path_, fileName));
                TerrainType tt = TerrainType.Parse(s);
                Debug.Log(tt);
                foreach (var ttModule in tt.Modules)
                {
                    Debug.Log(ttModule);
                }
            }
            throw new System.NotImplementedException();
        }
    }
}
