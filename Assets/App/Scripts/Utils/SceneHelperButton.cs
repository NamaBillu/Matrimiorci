using UnityEngine;

public class SceneHelperButton : MonoBehaviour
{
    public void LoadInviteScene()
    {
        if (App.Instance != null)
            App.Instance.GoToInviteScene();
    }

    public void LoadHomeScene()
    {
        if (App.Instance != null)
            App.Instance.GoToHomeScene();
    }
}
