using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Reads input via the new Input System and exposes normalized helicopter control values.
/// Attach to the same GameObject as HelicopterController.
/// Uses the existing InputSystem_Actions asset:
///   Move  (WASD / stick)  → Yaw (X) + Pitch (Y)
///   Jump  (Space)         → Press rate determines vertical momentum (fast = rise, medium = hover, slow = fall).
///   Attack (Left Mouse)   → Shoot
/// Steering (A/D) uses velocity-based smoothing - rapid presses build up steering momentum.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class HelicopterInput : MonoBehaviour
{
  public float Yaw { get; private set; }
  public float Pitch { get; private set; }
  public bool ShootPressed { get; private set; }

  /// <summary>
  /// Current button press rate (presses per second), smoothed over a sliding window.
  /// </summary>
  public float PressRate { get; private set; }

  [Header("Lift (Jump) Settings")]
  [Tooltip("Time window (seconds) over which button presses are counted to calculate rate.")]
  [SerializeField] private float rateWindow = 1f;

  [Tooltip("How quickly the press rate decays when no new presses occur.")]
  [SerializeField] private float rateDecay = 3f;

  [Header("Steering Smoothing Settings")]
  [Tooltip("Acceleration added to yaw velocity per key press.")]
  [SerializeField] private float yawAccelerationPerPress = 0.15f;

  [Tooltip("How quickly yaw velocity decays toward zero (friction).")]
  [SerializeField] private float yawVelocityDecay = 0.3f;

  [Tooltip("Maximum yaw velocity magnitude.")]
  [SerializeField] private float maxYawVelocity = 1f;

  private InputAction moveAction;
  private InputAction jumpAction;
  private InputAction attackAction;

  private readonly Queue<float> pressTimestamps = new Queue<float>();
  private bool jumpNeedsRelease;

  private float yawVelocity;
  private bool leftNeedsRelease;
  private bool rightNeedsRelease;

  void Awake()
  {
    var playerInput = GetComponent<PlayerInput>();
    moveAction = playerInput.actions["Move"];
    jumpAction = playerInput.actions["Jump"];
    attackAction = playerInput.actions["Attack"];
  }

  void Update()
  {
    Vector2 move = moveAction.ReadValue<Vector2>();
    Pitch = move.y;

    UpdateYawSteering(move.x);

    if (jumpAction.WasReleasedThisFrame())
      jumpNeedsRelease = false;

    if (jumpAction.WasPressedThisFrame() && !jumpNeedsRelease)
    {
      pressTimestamps.Enqueue(Time.time);
      jumpNeedsRelease = true;
    }

    UpdatePressRate();

    ShootPressed = attackAction.WasPressedThisFrame();
  }

  private void UpdateYawSteering(float rawYaw)
  {
    bool leftPressed = rawYaw < -0.5f;
    bool rightPressed = rawYaw > 0.5f;

    if (!leftPressed)
      leftNeedsRelease = false;
    if (!rightPressed)
      rightNeedsRelease = false;

    if (leftPressed && !leftNeedsRelease)
    {
      yawVelocity -= yawAccelerationPerPress;
      leftNeedsRelease = true;
    }

    if (rightPressed && !rightNeedsRelease)
    {
      yawVelocity += yawAccelerationPerPress;
      rightNeedsRelease = true;
    }

    yawVelocity = Mathf.Clamp(yawVelocity, -maxYawVelocity, maxYawVelocity);

    yawVelocity = Mathf.MoveTowards(yawVelocity, 0f, yawVelocityDecay * Time.deltaTime);

    Yaw = yawVelocity;
  }

  private void UpdatePressRate()
  {
    float windowStart = Time.time - rateWindow;

    while (pressTimestamps.Count > 0 && pressTimestamps.Peek() < windowStart)
      pressTimestamps.Dequeue();

    float targetRate = pressTimestamps.Count / rateWindow;

    if (targetRate > PressRate)
      PressRate = targetRate;
    else
      PressRate = Mathf.MoveTowards(PressRate, targetRate, rateDecay * Time.deltaTime);
  }
}
