using UnityEngine;

/// <summary>
/// Creates and animates helicopter rotor blades on top of the helicopter.
/// Spin speed is proportional to the helicopter's upward thrust (press rate).
/// Attach as a child of the helicopter GameObject.
/// </summary>
public class HelicopterRotor : MonoBehaviour
{
    [Header("Rotor Configuration")]
    [Tooltip("Number of rotor blades.")]
    [SerializeField] private int bladeCount = 2;

    [Tooltip("Length of each blade (should extend beyond helicopter body to be visible from inside).")]
    [SerializeField] private float bladeLength = 5f;

    [Tooltip("Width of each blade.")]
    [SerializeField] private float bladeWidth = 0.3f;

    [Tooltip("Thickness of each blade.")]
    [SerializeField] private float bladeThickness = 0.05f;

    [Tooltip("Height offset above the helicopter pivot (for a 1x1x1 cube, 0.5+ places it on top).")]
    [SerializeField] private float heightOffset = 0.55f;

    [Header("Spin Settings")]
    [Tooltip("Minimum rotation speed (degrees/sec) when idle.")]
    [SerializeField] private float minSpinSpeed = 180f;

    [Tooltip("Maximum rotation speed (degrees/sec) at full thrust.")]
    [SerializeField] private float maxSpinSpeed = 2000f;

    [Tooltip("How quickly the rotor accelerates/decelerates to target speed.")]
    [SerializeField] private float spinAcceleration = 5f;

    [Header("Visual")]
    [Tooltip("Color of the rotor blades.")]
    [SerializeField] private Color bladeColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    [Tooltip("Material for the blades (if null, uses default unlit material).")]
    [SerializeField] private Material bladeMaterial;

    private HelicopterInput helicopterInput;
    private Transform rotorHub;
    private float currentSpinSpeed;
    private float maxPressRate = 5f;

    void Start()
    {
        helicopterInput = GetComponentInParent<HelicopterInput>();
        if (helicopterInput == null)
        {
            Debug.LogError("HelicopterRotor: No HelicopterInput found in parent hierarchy!");
            enabled = false;
            return;
        }

        CreateRotorBlades();
    }

    void Update()
    {
        UpdateSpinSpeed();
        RotateBlades();
    }

    private void CreateRotorBlades()
    {
        GameObject hubObj = new GameObject("RotorHub");
        rotorHub = hubObj.transform;
        rotorHub.SetParent(transform);
        rotorHub.localPosition = new Vector3(0f, heightOffset, 0f);
        rotorHub.localRotation = Quaternion.identity;

        Material mat = bladeMaterial;
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = bladeColor;
        }

        float angleStep = 360f / bladeCount;

        for (int i = 0; i < bladeCount; i++)
        {
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = $"Blade_{i}";
            blade.transform.SetParent(rotorHub);

            blade.transform.localScale = new Vector3(bladeLength, bladeThickness, bladeWidth);
            blade.transform.localPosition = Vector3.zero;
            blade.transform.localRotation = Quaternion.Euler(0f, angleStep * i, 0f);

            Renderer renderer = blade.GetComponent<Renderer>();
            renderer.material = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Collider col = blade.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }
    }

    private void UpdateSpinSpeed()
    {
        float pressRate = helicopterInput.PressRate;
        float thrustNormalized = Mathf.Clamp01(pressRate / maxPressRate);

        float targetSpeed = Mathf.Lerp(minSpinSpeed, maxSpinSpeed, thrustNormalized);

        currentSpinSpeed = Mathf.Lerp(currentSpinSpeed, targetSpeed, spinAcceleration * Time.deltaTime);
    }

    private void RotateBlades()
    {
        if (rotorHub == null) return;

        rotorHub.Rotate(Vector3.up, currentSpinSpeed * Time.deltaTime, Space.Self);
    }
}
