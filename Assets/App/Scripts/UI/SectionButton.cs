using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SectionButton : MonoBehaviour
{
    #region Inspector Variables

    [Tooltip("The popup id to open when the section is unlocked (or requiresUnlock is false).")]
    [SerializeField] private string popupId = "";

    [Tooltip("When false, the button always opens the target popup regardless of lock state.")]
    [SerializeField] private bool requiresUnlock = true;

    [Tooltip("GameObject shown over the button while it is locked. Hide/show is driven by Refresh().")]
    [SerializeField] private GameObject lockIcon;

    [Tooltip("Optional label shown when locked, e.g. 'Inserisci il codice per sbloccare'.")]
    [SerializeField] private GameObject hintLabel;

    #endregion

    #region Member Variables

    private Button _button;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        App.OnUnlocked += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        App.OnUnlocked -= Refresh;
    }

    #endregion

    #region Private Methods

    private void Refresh()
    {
        bool isLocked = requiresUnlock && (App.Instance == null || !App.Instance.IsUnlocked);

        if (lockIcon != null)
            lockIcon.SetActive(isLocked);

        if (hintLabel != null)
            hintLabel.SetActive(isLocked);

        if (_button == null) return;

        _button.onClick.RemoveAllListeners();

        if (isLocked)
            _button.onClick.AddListener(OpenPasswordPopup);
        else
            _button.onClick.AddListener(OpenTargetPopup);
    }

    private void OpenPasswordPopup()
    {
        PopupManager.Instance.Show("PasswordPopup");
        SoundManager.Instance.Play("bttn_click");
    }

    private void OpenTargetPopup()
    {
        if (string.IsNullOrEmpty(popupId)) return;
        PopupManager.Instance.Show(popupId);
        SoundManager.Instance.Play("bttn_click");
    }

    #endregion
}
