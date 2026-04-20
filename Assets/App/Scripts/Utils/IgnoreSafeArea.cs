using UnityEngine;

public class IgnoreSafeArea : MonoBehaviour
{
    [SerializeField] bool ignoreHeight = true;
    [SerializeField] bool ignoreWidth = true;

    RectTransform RectTransform { get { return (RectTransform)transform; } }
    SafeArea safeArea;
    Vector2 defaultOffsetMin;
    Vector2 defaultOffsetMax;
    bool init = false;

    private void Awake()
    {
        safeArea = FindFirstObjectByType<SafeArea>();
        defaultOffsetMin = RectTransform.offsetMin;
        defaultOffsetMax = RectTransform.offsetMax;
    }

    private void Start()
    {
        safeArea.SafeAreaChanged += IgnoreSafe;
        init = true;
        IgnoreSafe((RectTransform)safeArea.transform);
    }

    private void OnEnable()
    {
        if (!init) { return; }
        IgnoreSafe((RectTransform)safeArea.transform);
    }

    private void IgnoreSafe(RectTransform safeRect)
    {
        if (ignoreHeight)
        {
            RectTransform.anchorMin = Vector2.zero - safeRect.anchorMin;
            RectTransform.anchorMax = Vector2.one + (Vector2.one - safeRect.anchorMax);
        }
        if (ignoreWidth)
        {
            RectTransform.offsetMin = defaultOffsetMin - safeRect.offsetMin;
            RectTransform.offsetMax = defaultOffsetMax - safeRect.offsetMax;
        }
    }

    private void OnDestroy()
    {
        if (safeArea != null)
        {
            safeArea.SafeAreaChanged -= IgnoreSafe;
        }
    }
}
