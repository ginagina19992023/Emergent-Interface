using UnityEngine;

public class Skybox : MonoBehaviour
{
    private Transform helicopterRootInside;
    private HelicopterController helicopterInside;

    void OnTriggerEnter(Collider other)
    {
        if (!TryGetHelicopterRoot(other, out Transform root, out HelicopterController helicopter))
            return;
        if (helicopterRootInside == root)
            return;

        helicopterRootInside = root;
        helicopterInside = helicopter;
        if (helicopterInside != null)
            helicopterInside.SetVerticalMotionLocked(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!TryGetHelicopterRoot(other, out Transform root, out _))
            return;
        if (helicopterRootInside != root)
            return;

        if (helicopterInside != null)
            helicopterInside.SetVerticalMotionLocked(false);
        helicopterRootInside = null;
        helicopterInside = null;
    }

    static bool TryGetHelicopterRoot(Collider other, out Transform root, out HelicopterController helicopter)
    {
        root = null;
        helicopter = null;

        if (other.attachedRigidbody != null)
        {
            helicopter = other.attachedRigidbody.GetComponent<HelicopterController>();
            if (helicopter != null)
            {
                root = other.attachedRigidbody.transform;
                return true;
            }
        }

        helicopter = other.GetComponentInParent<HelicopterController>();
        if (helicopter != null)
        {
            root = helicopter.transform;
            return true;
        }

        return false;
    }

    void OnDisable()
    {
        if (helicopterInside != null)
            helicopterInside.SetVerticalMotionLocked(false);
        helicopterRootInside = null;
        helicopterInside = null;
    }
}
