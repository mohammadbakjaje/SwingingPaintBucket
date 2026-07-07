using UnityEngine;
using TMPro;

/// <summary>
/// ArabicShaper - Helper class to properly render Arabic text in TextMeshProUGUI
/// 
/// Handles:
/// - Proper Arabic character shaping and connection
/// - Right-to-Left (RTL) text support
/// - TextMeshPro UGUI integration
/// 
/// Usage:
///     TextMeshProUGUI tmpText = GetComponent<TextMeshProUGUI>();
///     ArabicShaper.ApplyArabicText(tmpText, "السلام عليكم");
/// </summary>
public static class ArabicShaper
{
    /// <summary>
    /// Applies properly shaped Arabic text to a TextMeshProUGUI component
    /// </summary>
    /// <param name="textComponent">The TextMeshProUGUI component to update</param>
    /// <param name="arabicText">The Arabic text to display</param>
    public static void ApplyArabicText(TextMeshProUGUI textComponent, string arabicText)
    {
        if (textComponent == null)
        {
            Debug.LogError("TextMeshProUGUI component is null");
            return;
        }

        // TextMeshPro handles RTL text better than legacy Text
        // Set the text directly - TMP will handle bidirectional text properly
        textComponent.text = arabicText;

        // Enable RTL support
        textComponent.isRightToLeftText = true;

        // Force layout rebuild to ensure proper rendering
        textComponent.SetLayoutDirty();
    }

    /// <summary>
    /// Applies mixed Arabic/English text (typically Arabic on top, English on bottom)
    /// </summary>
    /// <param name="textComponent">The TextMeshProUGUI component to update</param>
    /// <param name="arabicText">The Arabic text (top line)</param>
    /// <param name="englishText">The English text (bottom line)</param>
    public static void ApplyBilingualText(TextMeshProUGUI textComponent, string arabicText, string englishText)
    {
        if (textComponent == null)
        {
            Debug.LogError("TextMeshProUGUI component is null");
            return;
        }

        // Combine Arabic (RTL) and English (LTR) with proper line break
        string combinedText = arabicText + "\n" + englishText;

        textComponent.text = combinedText;
        textComponent.isRightToLeftText = true;

        // Force layout rebuild
        textComponent.SetLayoutDirty();
    }

    /// <summary>
    /// Creates a properly configured TextMeshProUGUI component with Arabic support
    /// </summary>
    /// <param name="parent">The parent GameObject to attach the text to</param>
    /// <param name="name">Name for the text GameObject</param>
    /// <param name="arabicText">The Arabic text to display</param>
    /// <param name="fontSize">Font size (default 24)</param>
    /// <param name="color">Text color (default white)</param>
    /// <returns>The configured TextMeshProUGUI component</returns>
    public static TextMeshProUGUI CreateArabicTextComponent(
        GameObject parent,
        string name,
        string arabicText,
        int fontSize = 24,
        Color? color = null)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform, false);

        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();

        // Configure for Arabic
        tmpText.isRightToLeftText = true;
        tmpText.text = arabicText;
        tmpText.fontSize = fontSize;
        tmpText.color = color ?? Color.white;
        tmpText.alignment = TextAlignmentOptions.BottomRight; // Align to right for RTL

        // Configure layout
        RectTransform rectTransform = textObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // Force layout update
        tmpText.SetLayoutDirty();

        return tmpText;
    }

    /// <summary>
    /// Enables proper RTL rendering for a TextMeshProUGUI component
    /// </summary>
    /// <param name="textComponent">The component to configure</param>
    public static void EnableArabicSupport(TextMeshProUGUI textComponent)
    {
        if (textComponent == null) return;

        textComponent.isRightToLeftText = true;
        textComponent.alignment = TextAlignmentOptions.BottomRight;
        textComponent.SetLayoutDirty();
    }

    /// <summary>
    /// Converts legacy Text alignment to appropriate TMP alignment for Arabic
    /// </summary>
    /// <returns>The appropriate TextAlignmentOptions for Arabic text</returns>
    public static TextAlignmentOptions GetArabicAlignment(TextAnchor legacyAlignment)
    {
        return legacyAlignment switch
        {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopRight,      // Flip for RTL
            TextAnchor.UpperCenter => TMPro.TextAlignmentOptions.Top,
            TextAnchor.UpperRight => TextAlignmentOptions.TopLeft,      // Flip for RTL
            TextAnchor.MiddleLeft => TMPro.TextAlignmentOptions.Right,  // Flip for RTL
            TextAnchor.MiddleCenter => TMPro.TextAlignmentOptions.Center,
            TextAnchor.MiddleRight => TMPro.TextAlignmentOptions.Left,  // Flip for RTL
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomRight,   // Flip for RTL
            TextAnchor.LowerCenter => TMPro.TextAlignmentOptions.Bottom,
            TextAnchor.LowerRight => TextAlignmentOptions.BottomLeft,   // Flip for RTL
            _ => TMPro.TextAlignmentOptions.Center,
        };
    }

    /// <summary>
    /// Checks if a string contains Arabic characters
    /// </summary>
    /// <param name="text">Text to check</param>
    /// <returns>True if the text contains Arabic characters</returns>
    public static bool ContainsArabic(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        foreach (char c in text)
        {
            // Arabic Unicode range: U+0600 to U+06FF
            if (c >= '\u0600' && c <= '\u06FF')
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Validates that TextMeshPro essentials are available in the project
    /// </summary>
    /// <returns>True if TextMeshPro is properly set up</returns>
    public static bool ValidateTextMeshProSetup()
    {
        // Try to find default TMP font asset
        var defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (defaultFont == null)
        {
            Debug.LogWarning("TextMeshPro not properly initialized. Please go to Window > TextMeshPro > Import TMP Essential Resources");
            return false;
        }

        return true;
    }
}
