using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ColorPreview : MonoBehaviour
{
    [SerializeField] private GameObject previewPanel;
    [SerializeField] private Image colorImage;
    [SerializeField] private TextMeshProUGUI colorHexText;
    [SerializeField] private List<ColorInfo> colorInfos = new();

    [Serializable]
    public class ColorInfo
    {
        public string HexColor;
        public string ColorName;
    }

    public void ShowColor(string hexColor)
    {
        string colorName = GetColorName(hexColor);
        if (!hexColor.StartsWith("#"))
        {
            hexColor = "#" + hexColor;
        }
        if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
        {
            previewPanel.SetActive(true);
            colorImage.color = color;
            colorHexText.text = $"{hexColor.ToUpperInvariant()} - {colorName}";

        }
        else
        {
            Debug.LogError($"Invalid hex color: {hexColor}");
            gameObject.SetActive(false);
        }
    }

    private string GetColorName(string hexColor)
    {
        var colorInfo = colorInfos.Find(info => string.Equals(info.HexColor, hexColor, StringComparison.OrdinalIgnoreCase));
        return colorInfo != null ? $"{colorInfo.ColorName}" : "";
    }
}
