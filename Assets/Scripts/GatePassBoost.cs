using UnityEngine;

/// <summary>
/// Applies a speed boost along the helicopter’s facing when it enters this trigger.
/// Use a trigger collider sized to the gate opening; the visible mesh can stay non-trigger.
/// </summary>
public class GatePassBoost : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Instant velocity change along the helicopter’s facing direction (m/s).")]
    private float velocityImpulse = 12f;

    [SerializeField]
    [Tooltip("Extra acceleration along the helicopter’s facing for the duration below (each physics step).")]
    private float extraForwardAcceleration = 18f;

    [SerializeField]
    [Tooltip("How long the sustained acceleration lasts (seconds).")]
    private float boostDuration = 2.5f;

    private Transform _helicopterRootInside;

    void OnTriggerEnter(Collider other)
    {
        if (!TryGetHelicopterRoot(other, out Transform root))
            return;
        if (_helicopterRootInside == root)
            return;

        _helicopterRootInside = root;
        var heli = root.GetComponent<HelicopterController>();
        if (heli != null)
            heli.ApplyDirectionalBoost(root.forward, velocityImpulse, extraForwardAcceleration, boostDuration);
    }

    void OnTriggerExit(Collider other)
    {
        if (!TryGetHelicopterRoot(other, out Transform root))
            return;
        if (_helicopterRootInside == root)
            _helicopterRootInside = null;
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
