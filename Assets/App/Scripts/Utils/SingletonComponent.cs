using UnityEngine;

public class SingletonComponent<T> : MonoBehaviour
{
    public static T Instance;

    protected virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = gameObject.GetComponent<T>();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}