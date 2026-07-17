using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

public class AssemblySnapZone : MonoBehaviour
{
    [Header("Identity")]
    public string zoneId;
    public SwitchPartType acceptedPartType;
    public int acceptedModuleIndex;
    
    [Header("Prerequisites")]
    public List<string> prerequisiteZoneIds;
    
    [Header("State")]
    public bool isSatisfied = false;
    public bool isActive = false;
    
    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor _socket;
    private Renderer _zoneRenderer;
    private MaterialPropertyBlock _propBlock;
    
    // Colors for the snap zone visualization
    private static readonly Color InactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    private static readonly Color ReadyColor = new Color(0.2f, 0.8f, 1f, 0.4f);
    private static readonly Color HoverValidColor = new Color(0.1f, 1f, 0.3f, 0.6f);
    private static readonly Color HoverInvalidColor = new Color(1f, 0.2f, 0.1f, 0.6f);
    private static readonly Color SatisfiedColor = new Color(0.1f, 1f, 0.1f, 0.15f);
    
    void Awake()
    {
        _socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        _zoneRenderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        
        // Start inactive
        SetZoneActive(false);
        
        // Subscribe to socket events
        _socket.hoverEntered.AddListener(OnHoverEntered);
        _socket.hoverExited.AddListener(OnHoverExited);
        _socket.selectEntered.AddListener(OnSelectEntered);
    }
    
    public void SetZoneActive(bool active)
    {
        isActive = active;
        _socket.socketActive = active;
        
        if (_zoneRenderer != null)
        {
            SetZoneColor(active ? ReadyColor : InactiveColor);
            _zoneRenderer.enabled = active; // Hide zone visualization when inactive
        }
    }
    
    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (!isActive) return;
        
        // Check if the hovering object is the correct part
        SwitchPart part = args.interactableObject.transform.GetComponent<SwitchPart>();
        if (part == null) return;
        
        bool isValid = (part.partType == acceptedPartType && 
                        part.moduleIndex == acceptedModuleIndex);
        
        // Highlight the part
        part.SetHighlight(isValid ? HighlightState.Valid : HighlightState.Invalid);
        
        // Highlight the zone itself
        SetZoneColor(isValid ? HoverValidColor : HoverInvalidColor);
    }
    
    private void OnHoverExited(HoverExitEventArgs args)
    {
        SwitchPart part = args.interactableObject.transform.GetComponent<SwitchPart>();
        if (part != null) part.SetHighlight(HighlightState.None);
        
        SetZoneColor(isActive ? ReadyColor : InactiveColor);
    }
    
    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Verify the snapped part is correct type
        SwitchPart part = args.interactableObject.transform.GetComponent<SwitchPart>();
        if (part == null || part.partType != acceptedPartType || 
            part.moduleIndex != acceptedModuleIndex)
        {
            // Wrong part forced in — eject it
            _socket.interactionManager.SelectExit(_socket, 
                (UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)args.interactableObject);
            part?.SetHighlight(HighlightState.Invalid);
            return;
        }
        
        // Correct snap — mark satisfied
        isSatisfied = true;
        part.SetHighlight(HighlightState.None);
        part.GetComponent<Rigidbody>().isKinematic = true; // Lock it in place
        SetZoneColor(SatisfiedColor);
        
        // Notify the AssemblyManager to check if new zones should unlock
        AssemblyManager.Instance.OnZoneSatisfied(zoneId);
    }
    
    private void SetZoneColor(Color color)
    {
        if (_zoneRenderer == null) return;
        _zoneRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_BaseColor", color);
        _zoneRenderer.SetPropertyBlock(_propBlock);
    }
}