using System;
using System.Collections.Generic;
using UnityEngine;

public class GroupContentGate : MonoBehaviour
{
    #region Inspector Variables

    [Tooltip("The GameObject to show or hide. Assign the content root in the Inspector.")]
    [SerializeField] private GameObject content;

    [Tooltip("Leave empty to show for ALL authenticated groups. Populate with group passwords to restrict to specific groups only.")]
    [SerializeField] private List<string> visibleToPasswords = new List<string>();

    #endregion

    #region Unity Methods

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
        if (content == null) return;
        if (App.Instance == null) { content.SetActive(false); return; }
        if (!App.Instance.IsUnlocked) { content.SetActive(false); return; }

        if (visibleToPasswords == null || visibleToPasswords.Count == 0)
        {
            content.SetActive(true);
            return;
        }

        string currentPassword = App.Instance.CurrentGroup?.password ?? string.Empty;
        bool isVisible = visibleToPasswords.Exists(
            p => string.Equals(p, currentPassword, StringComparison.OrdinalIgnoreCase)
        );
        content.SetActive(isVisible);
    }

    #endregion
}
