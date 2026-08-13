using UnityEngine;

/// <summary>
/// Repositions AR-SampleScene UI elements when screen orientation changes.
/// Attach to the AssemblyHints Canvas in AR-SampleScene only.
/// Requires correct anchors already set (see user manual Section 11).
/// This script fine-tunes offsets per orientation.
/// </summary>
public class ARAdaptiveUI : MonoBehaviour
{
    [Header("UI Rect Transforms — assign in Inspector")]
    public RectTransform hintTextRect;
    public RectTransform counterTextRect;
    public RectTransform recoverButtonRect;

    [Header("Portrait dimensions (pixels) — for scaling)")]
        // ---------------------------------------------------------
    // Reference resolution from the user's portrait layout
    // ---------------------------------------------------------

    private const float ReferenceWidth = 1080f;
    private const float ReferenceHeight = 2400f;

    // ---------------------------------------------------------
    // Existing portrait layout
    // ---------------------------------------------------------
    [Header("Portrait offsets (pixels from anchor edge)")]
    private static readonly Vector2 HintPosition =
        new Vector2(-250f, 900f);

    private static readonly Vector2 HintSize =
        new Vector2(1000f, 100f);

    private static readonly Vector2 CountPosition =
        new Vector2(-250f, 1000f);

    private static readonly Vector2 CountSize =
        new Vector2(1000f, 100f);

    private static readonly Vector2 ButtonPosition =
        new Vector2(0f, -1000f);

    private static readonly Vector2 ButtonSize =
        new Vector2(400f, 160f);

    private int _lastWidth;
    private int _lastHeight;

    void Start()
    {
        _lastWidth = Screen.width;
        _lastHeight = Screen.height;

        ApplyLayout();
    }

    void Update()
    {
        if (Screen.width != _lastWidth ||
            Screen.height != _lastHeight)
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            ApplyLayout();
        }
    }

    private void ApplyLayout()
    {
        float widthScale =
            Screen.width / ReferenceWidth;

        float heightScale =
            Screen.height / ReferenceHeight;

        // -----------------------------------------------------
        // Use the same scaling factor for X/Y so the layout
        // keeps its proportions when the orientation changes.
        // -----------------------------------------------------

        float scale = Mathf.Min(widthScale, heightScale);

        // -----------------------------------------------------
        // HINT TEXT
        // -----------------------------------------------------

        if (hintTextRect != null)
        {
            hintTextRect.anchoredPosition = new Vector2(
                HintPosition.x * widthScale,
                HintPosition.y * heightScale
            );

            hintTextRect.sizeDelta = new Vector2(
                HintSize.x * widthScale,
                HintSize.y * scale
            );
        }

        // -----------------------------------------------------
        // COUNTER
        // -----------------------------------------------------

        if (counterTextRect != null)
        {
            counterTextRect.anchoredPosition = new Vector2(
                CountPosition.x * widthScale,
                CountPosition.y * heightScale
            );

            counterTextRect.sizeDelta = new Vector2(
                CountSize.x * widthScale,
                CountSize.y * scale
            );
        }

        // -----------------------------------------------------
        // RECOVER BUTTON
        // -----------------------------------------------------

        if (recoverButtonRect != null)
        {
            recoverButtonRect.anchoredPosition = new Vector2(
                ButtonPosition.x * widthScale,
                ButtonPosition.y * heightScale
            );

            recoverButtonRect.sizeDelta = new Vector2(
                ButtonSize.x * widthScale,
                ButtonSize.y * scale
            );
        }
    }
}