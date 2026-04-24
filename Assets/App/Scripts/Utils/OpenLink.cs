using UnityEngine;

public class OpenLink : MonoBehaviour
{
    [SerializeField] private string url;

    public void Open()
    {
        if (!string.IsNullOrEmpty(url))
        {
            Application.OpenURL(url);
        }
        else
        {
            Debug.LogError("URL is not set.");
        }
    }
}
