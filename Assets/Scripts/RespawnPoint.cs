using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
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
            helicopterController.SetLastRespawnPoint(GetCenterWorldPosition(), transform.rotation);
        Debug.Log($"RespawnPoint hit by helicopter '{helicopterRoot.name}'.", this);
    }

    Vector3 GetCenterWorldPosition()
    {
        var ownCollider = GetComponent<Collider>();
        if (ownCollider != null)
            return ownCollider.bounds.center;
        return transform.position;
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
