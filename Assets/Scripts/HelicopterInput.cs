using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads input via the new Input System and exposes normalized helicopter control values.
/// Attach to the same GameObject as HelicopterController.
/// Uses the existing InputSystem_Actions asset:
///   Move  (WASD / stick)  → Yaw (X) + Pitch (Y)
///   Jump  (Space)         → Each click adds upward velocity (release required between presses; holding does nothing).
///   Attack (Left Mouse)   → Shoot
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class HelicopterInput : MonoBehaviour
{
  public float Yaw { get; private set; }
  public float Pitch { get; private set; }
  public bool ShootPressed { get; private set; }

  private InputAction moveAction;
  private InputAction jumpAction;
  private InputAction attackAction;

  private bool jumpPressedThisFrame;
  /// <summary>True after a jump is consumed until the button is released, so the next press only counts after release.</summary>
  private bool jumpNeedsRelease;

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
    Yaw = move.x;
    Pitch = move.y;

    if (jumpAction.WasReleasedThisFrame())
      jumpNeedsRelease = false;

    // Only count a new jump press after the button was released since the last counted press (no benefit from holding).
    if (jumpAction.WasPressedThisFrame() && !jumpNeedsRelease)
      jumpPressedThisFrame = true;

    ShootPressed = attackAction.WasPressedThisFrame();
  }

  /// <summary>
  /// Returns true once per jump click; consumed so the controller can add one upward impulse per tap. Holding does nothing.
  /// </summary>
  public bool ConsumeJumpPressed()
  {
    if (!jumpPressedThisFrame) return false;
    jumpPressedThisFrame = false;
    jumpNeedsRelease = true;
    return true;
  }
}
