using TMPro;
using UnityEngine;

public class AlloggioPopup : Popup
{
    #region Inspector Variables

    [Tooltip("Panel shown when the current group has hasApartment = true.")]
    [SerializeField] private GameObject apartmentPanel;

    [Tooltip("TextMeshProUGUI label for the apartment name.")]
    [SerializeField] private TextMeshProUGUI apartmentNameLabel;

    [Tooltip("TextMeshProUGUI label for the apartment address.")]
    [SerializeField] private TextMeshProUGUI apartmentAddressLabel;

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

            if (apartmentAddressLabel != null)
                apartmentAddressLabel.text = group.apartmentAddress;

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
