#nullable enable

using UnityEngine;

public class AttachmentPoint : MonoBehaviour
{
    public bool HasAttachment => obj != null;

    private Transform? obj;
    private Transform? originalParent;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;

    public void Attach(Transform t)
    {
        if (obj != null)
        {
            Debug.LogError("Object is already attached to this point!");
            return;
        }
        obj = t;
        originalParent = t.parent;
        t.transform.GetPositionAndRotation(out originalPosition, out originalRotation);
        originalScale = t.transform.localScale;
        
        t.transform.SetParent(transform);
        t.transform.SetPositionAndRotation(transform.position, Quaternion.identity);
        t.transform.localScale = new Vector3(1, 0.65f, 0.1f);
    }

    public void Detach()
    {
        if (obj == null)
        {
            return;
        }
        
        obj.SetParent(originalParent);
        obj.SetPositionAndRotation(originalPosition, originalRotation);
        obj.localScale = originalScale;

        obj = null;
        originalParent = null;
    }
}
