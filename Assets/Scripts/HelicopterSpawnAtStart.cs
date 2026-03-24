using UnityEngine;

/// <summary>
/// Teleports the helicopter to a spawn object and resets velocity. Heading can follow the spawn transform,
/// or use the spawn's forward direction flattened to the ground (so you aim with the Scene view blue arrow / Y rotation only).
/// </summary>
[DefaultExecutionOrder(100)]
public class HelicopterSpawnAtStart : MonoBehaviour
{
    public enum HeadingMode
    {
        /// <summary>Use the heading source transform rotation exactly (pitch, yaw, roll).</summary>
        MatchSpawnTransform = 0,
        /// <summary>
        /// Use horizontal flight direction from the heading source forward (rotate around Y in the editor; tilt without banking the craft).
        /// </summary>
        HorizontalForwardFromSpawn = 1,
    }

    [SerializeField] private string spawnPointObjectName = "SpawnPoint";

    [Tooltip("How heading is derived from the spawn. HorizontalForwardFromSpawn ignores pitch/roll on the spawn and uses only compass direction.")]
    [SerializeField] private HeadingMode headingMode = HeadingMode.HorizontalForwardFromSpawn;

    [Tooltip("If set, heading is taken from this transform instead of the spawn root. Use a child empty (e.g. under SpawnPoint) aimed along the runway.")]
    [SerializeField] private Transform headingOverride;

    [Tooltip("Extra nose-up (+) / nose-down (-) degrees applied after heading is resolved (e.g. match default glide pitch).")]
    [SerializeField] private float extraPitchDegrees;

    void Start()
    {
        ApplySpawn();
    }

    /// <summary>Teleport to spawn, reset rigidbody motion, and apply heading. Call after respawn without scene reload if needed.</summary>
    public void ApplySpawn()
    {
        GameObject spawn = GameObject.Find(spawnPointObjectName);
        if (spawn == null)
        {
            Debug.LogWarning($"HelicopterSpawnAtStart: No GameObject named '{spawnPointObjectName}' found.");
            return;
        }

        Transform headingSource = headingOverride != null ? headingOverride : spawn.transform;
        Quaternion rotation = ResolveHeading(headingSource);

        Vector3 position = spawn.transform.position;
        var rb = GetComponent<Rigidbody>();

        transform.SetPositionAndRotation(position, rotation);
        if (rb != null)
        {
            rb.position = position;
            rb.rotation = rotation;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    Quaternion ResolveHeading(Transform source)
    {
        switch (headingMode)
        {
            case HeadingMode.MatchSpawnTransform:
                return source.rotation;

            case HeadingMode.HorizontalForwardFromSpawn:
            {
                Vector3 flat = source.forward;
                flat.y = 0f;
                if (flat.sqrMagnitude < 1e-8f)
                    flat = Vector3.forward;
                else
                    flat.Normalize();

                Quaternion face = Quaternion.LookRotation(flat, Vector3.up);
                if (Mathf.Abs(extraPitchDegrees) > 1e-5f)
                    return Quaternion.AngleAxis(extraPitchDegrees, face * Vector3.right) * face;
                return face;
            }

            default:
                return source.rotation;
        }
    }
}
