#nullable enable

using System.Collections.Generic;
using Constants;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu class
/// Toggle between menu and detail view
/// </summary>
public class InterfaceController : MonoBehaviour
{
    public const int AdditionCount = 5;
    
    [SerializeField]
    private TextMeshProUGUI hud = null!;

    [SerializeField]
    private Transform main = null!;
    
    [SerializeField]
    private Text centerText = null!;

    [SerializeField]
    private Material uiMain = null!;
    
    [SerializeField]
    private Material uiExploration = null!;
    
    [SerializeField]
    private Material uiSelection = null!;
    
    [SerializeField]
    private Material uiSelected = null!;

    [SerializeField]
    private Material blackMaterial = null!;

    private MeshRenderer _mainMeshRenderer = null!;
    private Material? _previousMaterial;
    
    public Transform Main => main;

    public List<Transform> Additions { get; } = new(AdditionCount);

    private void Awake()
    {
        _mainMeshRenderer = Main.GetComponent<MeshRenderer>();
        var parent = Main.parent;
        
        // the first one is main
        // get all additions and add them to the list
        for (var i = 0; i < AdditionCount; i++)
        {
            Additions.Add(parent.transform.GetChild(i + 1));
        }
    }

    private void OnEnable()
    {
        SetMode(MenuMode.None);
    }

    public void SetMode(MenuMode mode, bool isSnapshotSelected = false)
    {
        switch (mode)
        {
            case MenuMode.None:
                SetMaterial(uiMain);
                SetHUD(StringConstants.MainModeInfo);
                SetCenterText(StringConstants.MainModeInfo);
                break;
            case MenuMode.Analysis:
                SetMaterial(uiExploration);
                SetHUD(StringConstants.ExplorationModeInfo);
                SetCenterText(StringConstants.ExplorationModeInfo);
                break;
            case MenuMode.Selection:
                SetMaterial(uiSelection);
                SetHUD(StringConstants.SelectionModeInfo);
                SetCenterText(StringConstants.SelectionModeInfo);
                break;
            case MenuMode.Selected:
                if (!isSnapshotSelected)
                {
                    SetMaterial(uiSelected);
                }
                break;
            default:
                SetMaterial(uiMain);
                break;
        }
    }

    public void BlackenOut()
    {
        _previousMaterial = _mainMeshRenderer.material;
        SetMaterial(blackMaterial);
    }

    public void RestorePreviousOverlay()
    {
        if (_previousMaterial == null)
        {
            return;
        }
        SetMaterial(_previousMaterial);
        _previousMaterial = null;
    }

    private void SetCenterText(string text) => centerText.text = text;

    private void SetHUD(string text = "") => hud.text = text;

    private void SetMaterial(Material mat) => _mainMeshRenderer.material = mat;
}
