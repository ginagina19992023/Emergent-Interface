using UnityEngine;

/// <summary>
/// Awards score when the helicopter enters this object's trigger collider.
/// Use a trigger <see cref="BoxCollider"/> (or similar) sized to the gate opening; the visible mesh can stay non-trigger.
/// </summary>
public class GatePassScore : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Points added when the helicopter passes through the gate (each time it enters after having left).")]
    private int pointsOnPass = 250;

    private Transform _helicopterRootInside;

    void OnTriggerEnter(Collider other)
    {
        if (pointsOnPass == 0 || PlayerScore.Instance == null)
            return;
        if (!TryGetHelicopterRoot(other, out Transform root))
            return;
        if (_helicopterRootInside == root)
            return;

        _helicopterRootInside = root;
        PlayerScore.Instance.AddScore(pointsOnPass);
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
