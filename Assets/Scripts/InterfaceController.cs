#nullable enable

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Menu class
/// Toggle between menu and detail view
/// </summary>
public class InterfaceController : MonoBehaviour
{
    [SerializeField]
    private Transform main = null!;
    
    private readonly List<AttachmentPoint> attachmentPoints = new();
    
    public Transform Main => main;

    private void Awake()
    {
        for (var i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).TryGetComponent<AttachmentPoint>(out var ap))
            {
                attachmentPoints.Add(ap);
            }
        }
    }

    public AttachmentPoint? GetNextAttachmentPoint()
    {
        return attachmentPoints.FirstOrDefault(ap => !ap.HasAttachment);
    }
}
