using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Adapted from https://stackoverflow.com/a/55863444
namespace InfiniteCombo.Nitrogen.Editor
{
    [InitializeOnLoad]
    public class EditorInit
    {
        public static readonly int? START_SCENE = 0;

        static EditorInit()
        {
            if (START_SCENE is int index)
            {
                var pathOfFirstScene = EditorBuildSettings.scenes[index].path;
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(pathOfFirstScene);
                EditorSceneManager.playModeStartScene = sceneAsset;
                Debug.Log(pathOfFirstScene + " was set as default play mode scene");
            }
            else
            {
                EditorSceneManager.playModeStartScene = null;
                Debug.Log("default play mode scene was unset");
            }
        }
    }
}
