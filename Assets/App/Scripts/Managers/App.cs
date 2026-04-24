using System;
using System.Collections.Generic;
using UIAnimations;
using UnityEngine;
using UnityEngine.SceneManagement;

public class App : SingletonComponent<App>
{
    #region Constants

    private const string PasswordKey = "saved_password";

    #endregion

    #region Inspector Variables

    [Header("Routing")]
    [SerializeField] private string inviteSceneId = "Invite";
    [SerializeField] private string homeSceneId = "Home";

    [Header("Group Database")]
    [Tooltip("All guest groups and their passwords. Add one entry per group.")]
    [SerializeField] private List<GroupData> groupDatabase = new List<GroupData>();

    [Space]
    [SerializeField] private GameObject loadingUI;
    #endregion

    #region Member Variables

    private Dictionary<string, GroupData> _groupMap;

    #endregion

    #region Properties

    /// <summary>The group whose password was entered. Null until unlocked.</summary>
    public GroupData CurrentGroup { get; private set; }

    /// <summary>True once a valid password has been entered or restored from PlayerPrefs.</summary>
    public bool IsUnlocked => CurrentGroup != null;

    #endregion

    #region Events

    /// <summary>
    /// Fired after a valid password is entered or session is restored on startup.
    /// ContentGate components subscribe to this to reveal locked content.
    /// </summary>
    public static event Action OnUnlocked;

    #endregion

    #region Unity Methods

    protected override void Awake()
    {
        base.Awake();

        // Screen settings
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;

        // Build O(1) password lookup — OrdinalIgnoreCase handles "rossi" vs "Rossi"
        _groupMap = new Dictionary<string, GroupData>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in groupDatabase)
        {
            if (!string.IsNullOrEmpty(group.password))
                _groupMap[group.password] = group;
        }

        // Restore unlock state from previous session (ROUT-02, PASS-05)
        string saved = PlayerPrefs.GetString(PasswordKey, "");
        if (!string.IsNullOrEmpty(saved) && _groupMap.TryGetValue(saved, out var savedGroup))
            SetGroup(savedGroup);
    }

    public void BootUp()
    {
        SoundManager.Instance.Play("default", loop: true, 0f);
        SceneManager.sceneLoaded += OnSceneLoaded;
        loadingUI.SetActive(true); // show loading UI on first scene
        SceneManager.LoadScene(ResolveDestination());
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Attempts to unlock the app with the given password.
    /// Returns true and fires OnUnlocked on success; returns false on failure.
    /// </summary>
    public bool TryUnlock(string password)
    {
        if (string.IsNullOrEmpty(password)) return false;

        string trimmed = password.Trim(); // mobile keyboards may add trailing space
        if (_groupMap.TryGetValue(trimmed, out var group))
        {
            PlayerPrefs.SetString(PasswordKey, trimmed);
            PlayerPrefs.Save(); // REQUIRED — WebGL never fires OnApplicationQuit
            SetGroup(group);
            return true;
        }
        return false;
    }

    public void GoToInviteScene()
    {
        loadingUI.SetActive(true); // show loading UI on first scene
        SceneManager.LoadScene(inviteSceneId);
    }

    public void GoToHomeScene()
    {
        loadingUI.SetActive(true); // show loading UI on first scene
        SceneManager.LoadScene(homeSceneId);
    }

    #endregion

    #region Private Methods

    private string ResolveDestination()
    {
#if UNITY_EDITOR
        // Application.absoluteURL returns "" in Editor — default to Home for Play Mode
        return homeSceneId;
#else
        // Returning guest: valid password already stored → always Home (ROUT-02)
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString(PasswordKey, "")))
            return homeSceneId;

        // First visit: read ?type= URL param (ROUT-01, ROUT-03)
        return GetQueryParam("type") == "invite" ? inviteSceneId : homeSceneId;
#endif
    }

    private static string GetQueryParam(string key)
    {
        string url = Application.absoluteURL;
        int qIdx = url.IndexOf('?');
        if (qIdx < 0) return "";

        string query = url.Substring(qIdx + 1);
        // Strip fragment (#anchor) if present
        int hashIdx = query.IndexOf('#');
        if (hashIdx >= 0) query = query.Substring(0, hashIdx);

        foreach (string part in query.Split('&'))
        {
            string[] kv = part.Split('=');
            if (kv.Length == 2 && kv[0] == key)
                return Uri.UnescapeDataString(kv[1]);
        }
        return "";
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (loadingUI != null && loadingUI.activeInHierarchy)
        {
            loadingUI.SetActive(false);
        }
    }

    private void SetGroup(GroupData group)
    {
        CurrentGroup = group;
        OnUnlocked?.Invoke();
    }

    #endregion
}
