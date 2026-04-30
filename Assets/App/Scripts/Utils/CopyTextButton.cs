using UnityEngine;

public class CopyTextButton : MonoBehaviour
{
    [SerializeField] private string textToCopy;

    public void CopyTextToClipboard()
    {
        GUIUtility.systemCopyBuffer = textToCopy;
    }
}
