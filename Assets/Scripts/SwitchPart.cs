using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SwitchPart : MonoBehaviour
{
    public SwitchPartType partType;
    public int moduleIndex; // 0=Driver, 1=Receiver1, 2=Receiver2, 3=Receiver3
    
    private Renderer[] _renderers;
    private MaterialPropertyBlock _propBlock;
    
    // Original colors stored for reset
    private Color[] _originalColors;
    
    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        
        // Store original colors
        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            // Use sharedMaterial to read the actual asset color
            // Fall back to white if BaseColor isn't present
            if (_renderers[i].sharedMaterial != null && 
                _renderers[i].sharedMaterial.HasProperty("_BaseColor"))
            {
                _originalColors[i] = _renderers[i].sharedMaterial.GetColor("_BaseColor");
            }
            else
            {
                _originalColors[i] = Color.white;
            }
        }
    }

    void OnEnable()
    {
        var interactable = GetComponent<XRGrabInteractable>();
        if (interactable != null)
        {
            interactable.hoverEntered.AddListener(OnHoverEntered);
            interactable.hoverExited.AddListener(OnHoverExited);
            interactable.selectEntered.AddListener(OnGrabbed);   // NEW
            interactable.selectExited.AddListener(OnReleased);   // NEW
        }
    }

    void OnDisable()
    {
        var interactable = GetComponent<XRGrabInteractable>();
        if (interactable != null)
        {
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
            interactable.hoverExited.RemoveListener(OnHoverExited);
            interactable.selectEntered.RemoveListener(OnGrabbed);   // NEW
            interactable.selectExited.RemoveListener(OnReleased);   // NEW
        }
    }

    private static readonly int LayerSwitchPart       = 8; // match your layer numbers
    private static readonly int LayerSwitchPartGrabbed = 9;

    // These are called by the existing hover listeners' parent: the XR Grab Interactable
    // Add these two additional event subscriptions inside OnEnable:

    // In OnEnable, ADD these two lines alongside the existing hover listeners:
    //     interactable.selectEntered.AddListener(OnGrabbed);
    //     interactable.selectExited.AddListener(OnReleased);
    // In OnDisable, ADD:
    //     interactable.selectEntered.RemoveListener(OnGrabbed);
    //     interactable.selectExited.RemoveListener(OnReleased);

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // Switch to grabbed layer — no longer collides with other SwitchPart objects
        SetLayerRecursively(gameObject, LayerSwitchPartGrabbed);
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        // Return to normal layer — resumes collisions
        SetLayerRecursively(gameObject, LayerSwitchPart);
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        PartLabelDisplay display = FindFirstObjectByType<PartLabelDisplay>();
        display?.ShowLabel(this, transform.position);
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        PartLabelDisplay display = FindFirstObjectByType<PartLabelDisplay>();
        display?.ShowLabel(null, Vector3.zero);
    }

    public void SetHighlight(HighlightState state)
    {
        if (state == HighlightState.None)
        {
            RestoreOriginalColors();
            return;
        }

        Color color = state == HighlightState.Valid
            ? new Color(0.2f, 0.5f, 1f, 0.8f) // Blue
            : new Color(1f, 0.2f, 0.2f, 0.8f); // Red

        foreach (var r in _renderers)
        {
            r.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_BaseColor", color);
            r.SetPropertyBlock(_propBlock);
        }
    }
    
    private void RestoreOriginalColors()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_BaseColor", _originalColors[i]);
            _renderers[i].SetPropertyBlock(_propBlock);
        }
    }
}

public enum HighlightState { None, Valid, Invalid }