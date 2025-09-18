using UnityEngine;

public abstract class PersistentSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this as T;
        DontDestroyOnLoad(gameObject);
    }
}

public abstract class SceneSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this as T;
        // Sahne yaþam döngüsüne baðlý, DontDestroyOnLoad YOK
    }
}
