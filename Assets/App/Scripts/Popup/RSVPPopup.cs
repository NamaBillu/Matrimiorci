using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class RSVPPopup : Popup
{
    #region Inspector Variables
    [Space]
    [Tooltip("Google Apps Script doGet deployment URL. Set after deploying RSVPSheet.gs.")]
    [SerializeField] private string appScriptUrl = "";

    [Tooltip("The RSVP form panel — shown when the guest has not yet submitted.")]
    [SerializeField] private GameObject formPanel;

    [Tooltip("The read-only confirmation panel — shown when already submitted (D-04).")]
    [SerializeField] private GameObject confirmationPanel;

    [Tooltip("Submit button — disabled during in-flight request and permanently after success.")]
    [SerializeField] private Button submitButton;

    [Tooltip("Spinner GameObject shown while the submission request is in flight (RSVP-09).")]
    [SerializeField] private GameObject spinnerObject;

    [Tooltip("Feedback label used to display the error message on submission failure (RSVP-11).")]
    [SerializeField] private GameObject feedbackLabelObject;
    [SerializeField] private TMP_Text feedbackLabel;

    [Tooltip("Container Transform under the ScrollView. GuestRowUI prefabs are instantiated here at runtime.")]
    [SerializeField] private Transform guestRowContainer;

    [Tooltip("GuestRowUI prefab — one row is instantiated per memberNames entry.")]
    [SerializeField] private GameObject guestRowPrefab;

    [Tooltip("Panel shown only for groups with hasBreakfastPref = true (RSVP-07 / D-03).")]
    [SerializeField] private GameObject breakfastPanel;

    [Tooltip("All breakfast option Toggles. Labels are read from each Toggle's child TMP_Text at submit time.")]
    [SerializeField] private List<Toggle> breakfastToggles = new();

    [Tooltip("Shared notes / dietary restrictions TMP_InputField for the whole group (RSVP-05 + RSVP-06 / D-05).")]
    [SerializeField] private TMP_InputField notesInput;

    #endregion

    #region Member Variables

    private const string SubmittedKey = "RSVPSubmitted";

    private readonly List<GuestRowUI> _guestRows = new();
    private Coroutine _submitCoroutine;

    #endregion

    #region Public Methods

    public override void OnShowing(object[] inData)
    {
        base.OnShowing(inData);

        // D-04: if already submitted, show read-only confirmation instead of the form
        if (PlayerPrefs.GetInt(SubmittedKey, 0) == 1)
        {
            SetPanelActive(showForm: false);
            return;
        }

        SetPanelActive(showForm: true);
        ResetFormState();

        if (App.Instance == null || App.Instance.CurrentGroup == null)
        {
            Debug.LogErrorFormat("[RSVPPopup] CurrentGroup is null — cannot populate form");
            return;
        }

        GroupData group = App.Instance.CurrentGroup;

        PopulateGuestRows(group);

        // D-03: show breakfast panel only for apartment groups (hasBreakfastPref flag)
        if (breakfastPanel != null)
            breakfastPanel.SetActive(group.hasBreakfastPref);
    }

    public override void OnHiding()
    {
        // Cancel any in-flight submission to prevent callbacks firing on a deactivated GameObject
        if (_submitCoroutine != null)
        {
            StopCoroutine(_submitCoroutine);
            _submitCoroutine = null;

            if (submitButton != null)  submitButton.interactable = true;
            if (spinnerObject != null) spinnerObject.SetActive(false);
        }

        base.OnHiding();
    }

    /// <summary>Wire to the Submit button's onClick in the Inspector.</summary>
    public void OnSubmitClicked()
    {
        if (_submitCoroutine != null) return; // double-tap guard
        _submitCoroutine = StartCoroutine(SubmitRSVP());
    }

    #endregion

    #region Private Methods

    private void SetPanelActive(bool showForm)
    {
        if (formPanel != null)         formPanel.SetActive(showForm);
        if (confirmationPanel != null) confirmationPanel.SetActive(!showForm);
    }

    private void ResetFormState()
    {
        if (submitButton != null)  submitButton.interactable = true;
        if (spinnerObject != null) spinnerObject.SetActive(false);

        if (feedbackLabelObject != null)
            feedbackLabelObject.SetActive(false);

        if (notesInput != null)
        {
            notesInput.text = string.Empty;
            notesInput.lineType = TMP_InputField.LineType.MultiLineNewline;
            notesInput.characterLimit = 300;
        }
    }

    private void PopulateGuestRows(GroupData group)
    {
        if (guestRowContainer != null)
        {
            foreach (Transform child in guestRowContainer)
                Destroy(child.gameObject);
        }
        _guestRows.Clear();

        if (guestRowPrefab == null || guestRowContainer == null) return;

        foreach (string memberName in group.memberNames)
        {
            GameObject rowGo = Instantiate(guestRowPrefab, guestRowContainer);
            GuestRowUI row = rowGo.GetComponent<GuestRowUI>();

            if (row == null)
            {
                Debug.LogErrorFormat("[RSVPPopup] guestRowPrefab is missing a GuestRowUI component");
                continue;
            }

            row.Initialize(memberName);
            _guestRows.Add(row);
        }
    }

    private IEnumerator SubmitRSVP()
    {
        if (submitButton != null)  submitButton.interactable = false;
        if (spinnerObject != null) spinnerObject.SetActive(true);
        if (feedbackLabelObject != null) feedbackLabelObject.SetActive(false);

        string url = BuildSubmitUrl();

        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = 15; // RSVP-11: 15 seconds for venue Wi-Fi

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // D-04: persist flag immediately — OnApplicationQuit does not fire on WebGL tab close
            PlayerPrefs.SetInt(SubmittedKey, 1);
            PlayerPrefs.Save();

            SetPanelActive(showForm: false);
        }
        else
        {
            // RSVP-11: show Italian error message with retry option
            if (feedbackLabel != null && feedbackLabelObject != null)
            {
                feedbackLabel.text = "Errore nell'invio. Riprova.";
                feedbackLabelObject.SetActive(true);
            }

            if (submitButton != null) submitButton.interactable = true;
        }

        if (spinnerObject != null) spinnerObject.SetActive(false);
        _submitCoroutine = null;
    }

    private string BuildSubmitUrl()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(appScriptUrl);

        // Group code identifies the submission
        sb.Append("?code=");
        sb.Append(Uri.EscapeDataString(App.Instance.CurrentGroup.password));

        // Per-guest fields — flat indexed params (RESEARCH.md: URL Construction Decision)
        for (int i = 0; i < _guestRows.Count; i++)
        {
            GuestRowUI row = _guestRows[i];
            sb.Append($"&guest{i}Name=");
            sb.Append(Uri.EscapeDataString(row.GuestName));
            sb.Append($"&guest{i}Attendance=");
            sb.Append(row.IsAttending ? "si" : "no");
            sb.Append($"&guest{i}Meal=");
            sb.Append(Uri.EscapeDataString(row.SelectedMeal));
        }

        // D-03: breakfast preference (apartment groups only, hasBreakfastPref)
        if (App.Instance.CurrentGroup.hasBreakfastPref)
        {
            sb.Append("&breakfast=");
            sb.Append(Uri.EscapeDataString(GetBreakfastValue()));
        }

        // D-05: shared notes field covering RSVP-05 (dietary) + RSVP-06 (general notes)
        sb.Append("&notes=");
        sb.Append(Uri.EscapeDataString(notesInput != null ? notesInput.text : string.Empty));

        return sb.ToString();
    }

    private string GetBreakfastValue()
    {
        var selected = new List<string>();

        foreach (Toggle t in breakfastToggles)
        {
            if (t == null || !t.isOn) continue;
            TMP_Text label = t.GetComponentInChildren<TMP_Text>();
            if (label != null)
                selected.Add(label.text);
        }

        return selected.Count > 0 ? string.Join(", ", selected) : "non specificato";
    }

    #endregion
}
