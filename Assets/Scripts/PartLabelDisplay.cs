using UnityEngine;
using TMPro;

public class PartLabelDisplay : MonoBehaviour
{
    [Header("Assign the PartLabel prefab here")]
    public GameObject labelPrefab;

    [Header("Height above part center")]
    public float heightOffset = 0.04f;

    private GameObject _label;
    private TextMeshProUGUI _text;

    private static readonly string[] ModuleNames =
    {
        "Driver Module",
        "Receiver Module 1",
        "Receiver Module 2",
        "Receiver Module 3"
    };

    void Start()
    {
        if (labelPrefab == null)
        {
            Debug.LogError("[PartLabelDisplay] labelPrefab not assigned.");
            return;
        }
        _label = Instantiate(labelPrefab);
        _text = _label.GetComponentInChildren<TextMeshProUGUI>();
        _label.SetActive(false);
    }

    public void ShowLabel(SwitchPart part, Vector3 worldPosition)
    {
        if (_label == null) return;

        if (part == null)
        {
            _label.SetActive(false);
            return;
        }

        string moduleName = (part.moduleIndex >= 0 && part.moduleIndex < ModuleNames.Length)
            ? ModuleNames[part.moduleIndex]
            : part.moduleIndex == -1 ? "Global" : $"Module {part.moduleIndex}";

        _text.text = $"<b>{part.partType}</b>\n<size=70%>{moduleName}</size>";

        _label.SetActive(true);
        _label.transform.position = worldPosition + Vector3.up * heightOffset;

        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 dir = _label.transform.position - cam.transform.position;
            if (dir.sqrMagnitude > 0.0001f)
                _label.transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}