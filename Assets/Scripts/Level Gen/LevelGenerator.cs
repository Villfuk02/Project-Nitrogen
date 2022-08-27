using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public LevelGeneratorPart[] parts;
    public int partIndex;

    private void Update()
    {
        if (partIndex >= parts.Length)
            return;
        if (!parts[partIndex].started)
        {
            parts[partIndex].Init();
            parts[partIndex].started = true;
        }
        else if (parts[partIndex].stopped)
        {
            partIndex++;
        }
    }
}
