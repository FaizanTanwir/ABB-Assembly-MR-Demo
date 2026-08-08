using UnityEngine;

/// <summary>
/// Visual-only indicator showing where a part belongs in the assembled switch.
/// No physics, no XR interaction. Controlled entirely by its linked AssemblySnapZone.
/// </summary>
public class VisualSnapGuide : MonoBehaviour
{
    [HideInInspector] public string linkedZoneId;

    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;

    private static readonly Color InactiveColor  = new Color(0.4f, 0.4f, 0.4f, 0.0f);
    private static readonly Color ReadyColor     = new Color(0.2f, 0.8f, 1.0f, 0.40f);
    private static readonly Color HoverColor     = new Color(0.0f, 1.0f, 0.4f, 0.75f);
    private static readonly Color SatisfiedColor = new Color(0.1f, 1.0f, 0.1f, 0.20f);

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    public void SetState(GuideState state)
    {
        if (_renderer == null) return;

        switch (state)
        {
            case GuideState.Inactive:
                _renderer.enabled = false;
                break;
            case GuideState.Ready:
                _renderer.enabled = true;
                Apply(ReadyColor);
                break;
            case GuideState.HoverValid:
                _renderer.enabled = true;
                Apply(HoverColor);          // bright green pulse while hovering
                break;
            case GuideState.Satisfied:
                _renderer.enabled = true;
                Apply(SatisfiedColor);
                break;
        }
    }

    private void Apply(Color color)
    {
        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_BaseColor", color);
        _renderer.SetPropertyBlock(_propBlock);
    }

    public enum GuideState { Inactive, Ready, HoverValid, Satisfied }
}