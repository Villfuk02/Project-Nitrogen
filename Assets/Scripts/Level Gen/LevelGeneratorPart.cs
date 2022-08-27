using UnityEngine;

public abstract class LevelGeneratorPart : MonoBehaviour
{
    public bool started;
    public bool stopped;

    public abstract void Init();
}
