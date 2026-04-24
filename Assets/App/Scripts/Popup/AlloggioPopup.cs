using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AlloggioPopup : Popup
{
    #region Inspector Variables

    [Tooltip("Panel shown when the current group has hasApartment = true.")]
    [SerializeField] private GameObject apartmentPanel;

    [Tooltip("TextMeshProUGUI label for the apartment name.")]
    [SerializeField] private TextMeshProUGUI apartmentNameLabel;

    [Tooltip("Image component to show the apartment image. Only active if the current group has a non-null apartmentImage.")]
    [SerializeField] private Image apartmentImage;
    
    [Tooltip("Button that opens the apartment website link. Only active if the current group has a non-empty apartmentMapsLink.")]
    [SerializeField] private Button openAptLinkButton;

    [Tooltip("TextMeshProUGUI label for apartment notes. Each entry in apartmentNotes is on its own line.")]
    [SerializeField] private TextMeshProUGUI apartmentNotesLabel;

    [Tooltip("Panel shown when the current group does NOT have a reserved apartment.")]
    [SerializeField] private GameObject generalSuggestionsPanel;

    #endregion

    #region Public Methods

    public override void OnShowing(object[] inData)
    {
        base.OnShowing(inData);

        if (App.Instance == null || App.Instance.CurrentGroup == null)
        {
            SetPanelActive(showApartment: false);
            return;
        }

        GroupData group = App.Instance.CurrentGroup;

        if (group.hasApartment)
        {
            SetPanelActive(showApartment: true);

            if (apartmentNameLabel != null)
                apartmentNameLabel.text = group.apartmentName;
            if (apartmentImage != null)
            {
                if (group.apartmentImage != null)
                {
                    apartmentImage.gameObject.SetActive(true);
                    apartmentImage.sprite = group.apartmentImage;
                }
                else
                {
                    apartmentImage.gameObject.SetActive(false);
                }
            }
            if (openAptLinkButton != null)
            {
                if (!string.IsNullOrEmpty(group.apartmentLink))
                {
                    openAptLinkButton.gameObject.SetActive(true);
                    openAptLinkButton.onClick.RemoveAllListeners();
                    openAptLinkButton.onClick.AddListener(() =>
                    {
                        Application.OpenURL(group.apartmentLink);
                        SoundManager.Instance.Play("bttn_click");
                    });
                }
                else
                {
                    openAptLinkButton.gameObject.SetActive(false);
                }
            }
            if (apartmentNotesLabel != null)
                apartmentNotesLabel.text = string.Join("\n", group.apartmentNotes);
        }
        else
        {
            SetPanelActive(showApartment: false);
        }
    }

    #endregion

    #region Private Methods

    private void SetPanelActive(bool showApartment)
    {
        if (apartmentPanel != null)
            apartmentPanel.SetActive(showApartment);

        if (generalSuggestionsPanel != null)
            generalSuggestionsPanel.SetActive(!showApartment);
    }

    #endregion
}
