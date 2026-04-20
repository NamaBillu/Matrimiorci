using TMPro;
using UnityEngine;

public class PasswordPopup : Popup
{
    #region Inspector Variables

    [SerializeField] private TMP_InputField codeInputField;
    [SerializeField] private TextMeshProUGUI errorLabel;

    #endregion

    #region Public Methods

    /// <summary>
    /// Called by the Submit button's OnClick event in the Inspector.
    /// </summary>
    public void OnSubmit()
    {
        string code = codeInputField != null ? codeInputField.text : "";

        if (App.Instance.TryUnlock(code))
        {
            Hide(false); // success — close popup, inherited hide animation plays
        }
        else
        {
            ShowError("Codice non valido. Riprova.");
        }
    }

    #endregion

    #region Popup Lifecycle Overrides

    public override void OnShowing(object[] inData)
    {
        // Clear input and error every time the popup opens
        if (codeInputField != null)
            codeInputField.text = "";
        HideError();
    }

    #endregion

    #region Private Methods

    private void ShowError(string message)
    {
        if (errorLabel == null) return;
        errorLabel.text = message;
        errorLabel.gameObject.SetActive(true);
    }

    private void HideError()
    {
        if (errorLabel == null) return;
        errorLabel.gameObject.SetActive(false);
    }

    #endregion
}
