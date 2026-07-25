using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

public class AssemblySnapZone : MonoBehaviour
{
    [Header("Identity")]
    public string zoneId;
    public SwitchPartType acceptedPartType;
    public int acceptedModuleIndex;
    public bool ignoreModuleIndex = false;
    
    [Header("AND Prerequisites — ALL must be satisfied")]
    public List<string> prerequisiteZoneIds;

    [Header("OR Prerequisites — ANY ONE must be satisfied (empty = skip check)")]
    public List<string> orPrerequisiteZoneIds;

    [Header("Options")]
    public bool isOptional = false; // Optional zones don't block AssemblyComplete

    [Header("State — do not edit in Inspector at runtime")]
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
    
    private bool IsCorrectPart(SwitchPart part)
    {
        if (part == null) return false;
        bool typeMatches = part.partType == acceptedPartType;
        bool moduleMatches = ignoreModuleIndex || (part.moduleIndex == acceptedModuleIndex);
        return typeMatches && moduleMatches;
    }
    
    public void SetZoneActive(bool active)
    {
        isActive = active;
        _socket.socketActive = active;
        
        if (_zoneRenderer != null)
        {
            _zoneRenderer.enabled = active; // Hide zone visualization when inactive
            SetZoneColor(active ? ReadyColor : InactiveColor);
        }
    }
    
    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (!isActive) return;
        
        // Check if the hovering object is the correct part
        SwitchPart part = args.interactableObject.transform.GetComponent<SwitchPart>();
        if (part == null) return;
        
        bool valid = IsCorrectPart(part);
        
        // Highlight the part
        part.SetHighlight(valid ? HighlightState.Valid : HighlightState.Invalid);
        
        // Highlight the zone itself
        SetZoneColor(valid ? HoverValidColor : HoverInvalidColor);
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
        if (part == null || !IsCorrectPart(part))
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

        // Activate child zones parented to this zone in the Hierarchy
        foreach (Transform child in transform)
        {
            AssemblySnapZone childZone = child.GetComponent<AssemblySnapZone>();
            if (childZone != null)
                AssemblyManager.Instance.EvaluateZone(childZone);
        }
    }
    
    private void SetZoneColor(Color color)
    {
        if (_zoneRenderer == null) return;
        _zoneRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_BaseColor", color);
        _zoneRenderer.SetPropertyBlock(_propBlock);
    }
}