#nullable enable

using System.Collections.Generic;
using System.Linq;
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
    private const string MainModeInfo = "Main Menu";
    private const string SelectionModeInfo = "Selection Mode";
    private const string ExplorationModeInfo = "Exploration Mode";
    
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

    private MeshRenderer mainMeshRenderer = null!;
    private Material? previousMaterial;
    
    public Transform Main => main;

    public List<AttachmentPoint> AttachmentPoints { get; } = new(AdditionCount);

    private void Awake()
    {
        mainMeshRenderer = Main.GetComponent<MeshRenderer>();
        var parent = Main.parent;
        
        // the first one is main
        // get all additions and add them to the list
        for (var i = 0; i < AdditionCount; i++)
        {
            AttachmentPoints.Add(parent.transform.GetChild(i + 1).GetComponent<AttachmentPoint>());
        }
    }

    private void OnEnable()
    {
        SetMode(MenuMode.None);
    }

    public AttachmentPoint? GetNextAddition()
    {
        return AttachmentPoints.FirstOrDefault(ap => !ap.HasAttachment);
    }
    
    public void SetMode(MenuMode mode, bool isSnapshotSelected = false)
    {
        switch (mode)
        {
            case MenuMode.None:
                SetMaterial(uiMain);
                SetHUD(MainModeInfo);
                SetCenterText(MainModeInfo);
                break;
            case MenuMode.Analysis:
                SetMaterial(uiExploration);
                SetHUD(ExplorationModeInfo);
                SetCenterText(ExplorationModeInfo);
                break;
            case MenuMode.Selection:
                SetMaterial(uiSelection);
                SetHUD(SelectionModeInfo);
                SetCenterText(SelectionModeInfo);
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
        previousMaterial = mainMeshRenderer.material;
        SetMaterial(blackMaterial);
    }

    public void RestorePreviousOverlay()
    {
        if (previousMaterial == null)
        {
            return;
        }
        SetMaterial(previousMaterial);
        previousMaterial = null;
    }

    private void SetCenterText(string text) => centerText.text = text;

    private void SetHUD(string text = "") => hud.text = text;

    private void SetMaterial(Material mat) => mainMeshRenderer.material = mat;
}
