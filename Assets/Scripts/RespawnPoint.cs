using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    [Header("Editor Gizmo")]
    [SerializeField] Color gizmoColor = new(0.2f, 0.9f, 0.3f, 0.9f);
    [SerializeField] float gizmoSizeMultiplier = 0.05f;
    [SerializeField] bool drawDirectionArrow = true;
    [SerializeField] float arrowLengthMultiplier = 0.5f;

    void Awake()
    {
        var ownCollider = GetComponent<Collider>();
        if (ownCollider != null && !ownCollider.isTrigger)
            ownCollider.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!TryGetHelicopterRoot(other, out Transform helicopterRoot))
            return;
        var helicopterController = helicopterRoot.GetComponent<HelicopterController>();
        if (helicopterController != null)
            helicopterController.SetLastRespawnPoint(GetCenterWorldPosition(), GetRespawnRotation());
        Debug.Log($"RespawnPoint hit by helicopter '{helicopterRoot.name}'.", this);
    }

    Vector3 GetCenterWorldPosition()
    {
        var ownCollider = GetComponent<Collider>();
        if (ownCollider != null)
            return ownCollider.bounds.center;
        return transform.position;
    }

    void OnDrawGizmos()
    {
        DrawRespawnGizmo(false);
    }

    void OnDrawGizmosSelected()
    {
        DrawRespawnGizmo(true);
    }

    void DrawRespawnGizmo(bool isSelected)
    {
        Gizmos.color = isSelected ? Color.yellow : gizmoColor;
        Vector3 center = GetCenterWorldPosition();
        float referenceSize = GetReferenceSize();
        float gizmoRadius = referenceSize * gizmoSizeMultiplier;
        float arrowLength = referenceSize * arrowLengthMultiplier;
        Vector3 normal = GetRespawnNormal();

        Gizmos.DrawSphere(center, gizmoRadius);

        if (!drawDirectionArrow)
            return;

        Vector3 tip = center + normal * arrowLength;
        Gizmos.DrawLine(center, tip);
        Gizmos.DrawSphere(tip, gizmoRadius * 0.35f);
    }

    float GetReferenceSize()
    {
        var ownCollider = GetComponent<Collider>();
        if (ownCollider == null)
            return 1f;
        float size = ownCollider.bounds.extents.magnitude;
        return Mathf.Max(0.5f, size);
    }

    Vector3 GetRespawnNormal()
    {
        Vector3 normal = transform.up;
        if (normal.sqrMagnitude < 1e-6f)
            return Vector3.forward;
        return normal.normalized;
    }

    Quaternion GetRespawnRotation()
    {
        Vector3 normal = GetRespawnNormal();
        return Quaternion.FromToRotation(Vector3.forward, normal);
    }

    static bool TryGetHelicopterRoot(Collider other, out Transform root)
    {
        root = null;
        if (other.attachedRigidbody != null)
        {
            if (other.attachedRigidbody.GetComponent<HelicopterController>() != null)
            {
                root = other.attachedRigidbody.transform;
                return true;
            }
        }

        var heli = other.GetComponentInParent<HelicopterController>();
        if (heli != null)
        {
            root = heli.transform;
            return true;
        }

        return false;
    }
}
