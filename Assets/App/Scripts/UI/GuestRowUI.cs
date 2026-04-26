using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuestRowUI : MonoBehaviour
{
    #region Inspector Variables

    [Tooltip("Label displaying the guest's pre-filled name.")]
    [SerializeField] private TMP_Text nameLabel;

    [Tooltip("Toggle: guest IS attending. Part of an attendance ToggleGroup.")]
    [SerializeField] private Toggle attendingToggle;

    [Tooltip("Toggle: guest is NOT attending. Part of an attendance ToggleGroup.")]
    [SerializeField] private Toggle notAttendingToggle;

    [Tooltip("Meal toggle: meat (carne). Set isOn = true by default in prefab Inspector.")]
    [SerializeField] private Toggle meatToggle;

    [Tooltip("Meal toggle: vegetarian (vegetariano).")]
    [SerializeField] private Toggle vegetarianToggle;

    #endregion

    #region Properties

    public string GuestName => nameLabel != null ? nameLabel.text : string.Empty;

    public bool IsAttending => attendingToggle != null && attendingToggle.isOn;

    public string SelectedMeal
    {
        get
        {
            if (meatToggle != null && meatToggle.isOn)              return "nessuna";
            if (vegetarianToggle != null && vegetarianToggle.isOn)  return "vegetariano";
            return "non specificato"; // fallback — prefab default (meatToggle.isOn) prevents this
        }
    }

    #endregion

    #region Public Methods

    public void Initialize(string guestName)
    {
        if (nameLabel != null)
            nameLabel.text = guestName;
    }

    public void PlaySound()
    {
        SoundManager.Instance.Play("bttn_click");
    }

    #endregion
}
