using Data.Loader;
using Data.Parsers;
using System.IO;
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

        public void LoadAll()
        {
            var files = Directory.GetFiles(path_, $"*.{FileExtension}");
            var terrainTypes = new TerrainType[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                PreprocessedParseStream s = new(Path.Combine(path_, files[i]));
                TerrainType tt = TerrainType.Parse(s);
                terrainTypes[i] = tt;
            }

            TerrainTypes.inst = new(terrainTypes);
            Debug.Log($"Loaded {files.Length} terrain types");
        }
    }
}
