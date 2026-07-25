using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit.Filtering;

public class PartTypeSelectFilter : MonoBehaviour, IXRSelectFilter
{
    public SwitchPartType acceptedType;
    public int acceptedModuleIndex;
    public bool ignoreModuleIndex = false;
    public bool canProcess => isActiveAndEnabled;
    
    public bool Process(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor, 
                        UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable)
    {
        SwitchPart part = (interactable as MonoBehaviour)?.GetComponent<SwitchPart>();
        if (part == null) return false;
        return part.partType == acceptedType && 
               (ignoreModuleIndex || part.moduleIndex == acceptedModuleIndex);
    }
}