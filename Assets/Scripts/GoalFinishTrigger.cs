using UnityEngine;

/// <summary>
/// When the helicopter enters this collider (use a trigger volume), shows the level-complete UI with the current score.
/// </summary>
public class GoalFinishTrigger : MonoBehaviour
{
    [SerializeField] private LevelCompleteUI levelCompleteUi;

    bool _completed;

    void Start()
    {
        if (levelCompleteUi == null)
            levelCompleteUi = FindFirstObjectByType<LevelCompleteUI>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (_completed)
            return;
        if (!TryGetHelicopterRoot(other, out _))
            return;
        if (levelCompleteUi == null)
            return;

        _completed = true;
        levelCompleteUi.Show();
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
