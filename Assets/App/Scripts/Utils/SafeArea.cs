using System;
using UnityEngine;
using UnityEngine.UI;

public class SafeArea : MonoBehaviour
{
    [SerializeField] CanvasScaler canvasScaler;
    RectTransform rectTransform;
    Rect safeArea;
    Vector2 minAnchor;
    Vector2 maxAnchor;

    public Action<RectTransform> SafeAreaChanged { get; set; }

    private void Start()
    {
        if (canvasScaler.referenceResolution.x / canvasScaler.referenceResolution.y > Screen.width / Screen.height)
        {
            rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, canvasScaler.referenceResolution.x);
        }
        CalculateSafeArea();
    }

    private void OnRectTransformDimensionsChange()
    {
        CalculateSafeArea();
    }

    public void CalculateSafeArea()
    {
        rectTransform = gameObject.GetComponent<RectTransform>();
        safeArea = Screen.safeArea;
        minAnchor = safeArea.position;
        maxAnchor = minAnchor + safeArea.size;

        minAnchor.x /= Screen.width;
        minAnchor.y /= Screen.height;

        maxAnchor.x /= Screen.width;
        maxAnchor.y /= Screen.height;

        rectTransform.anchorMin = minAnchor;
        rectTransform.anchorMax = maxAnchor;
        SafeAreaChanged?.Invoke(rectTransform);
    }
}
