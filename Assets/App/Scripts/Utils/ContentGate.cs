using UnityEngine;

public enum GateMode
{
    /// <summary>Content is visible but covered by a lock overlay. Overlay hides on unlock.</summary>
    LockedVisible,
    /// <summary>Content is invisible until unlocked. Hidden entirely, not just overlaid.</summary>
    Hidden
}

public class ContentGate : MonoBehaviour
{
    #region Inspector Variables

    [Tooltip("LockedVisible: content is visible but overlaid with a lock panel. Hidden: content is invisible until unlocked.")]
    [SerializeField] private GateMode mode = GateMode.LockedVisible;

    [Tooltip("Semi-transparent overlay GameObject shown when locked. Only used in LockedVisible mode. " +
             "MUST be a SIBLING of 'content' in the hierarchy, NOT a child — " +
             "if it were a child, content.SetActive(false) would hide the overlay too.")]
    [SerializeField] private GameObject lockOverlay;

    [Tooltip("The gated content root GameObject. In Hidden mode this is toggled active/inactive. " +
             "In LockedVisible mode this stays active; only the overlay is toggled.")]
    [SerializeField] private GameObject content;

    #endregion

    #region Unity Methods

    private void OnEnable()
    {
        App.OnUnlocked += Refresh;
        Refresh(); // apply current state immediately — handles gate activated after unlock already fired
    }

    private void OnDisable()
    {
        App.OnUnlocked -= Refresh;
    }

    #endregion

    #region Private Methods

    private void Refresh()
    {
        bool unlocked = App.Instance != null && App.Instance.IsUnlocked;

        switch (mode)
        {
            case GateMode.Hidden:
                if (content != null)
                    content.SetActive(unlocked);
                break;

            case GateMode.LockedVisible:
                if (lockOverlay != null)
                    lockOverlay.SetActive(!unlocked);
                break;
        }
    }

    #endregion
}
