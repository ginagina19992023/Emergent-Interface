using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads input via the new Input System and exposes normalized helicopter control values.
/// Attach to the same GameObject as HelicopterController.
/// Uses the existing InputSystem_Actions asset:
///   Move  (WASD / stick)  → Yaw (X) + Pitch (Y)
///   Jump  (Space)         → Throttle up
///   Sprint (Left Shift)   → Throttle down
///   Attack (Left Mouse)   → Shoot
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class HelicopterInput : MonoBehaviour
{
  public float Throttle { get; private set; }
  public float Yaw { get; private set; }
  public float Pitch { get; private set; }
  public bool ShootPressed { get; private set; }

  private InputAction moveAction;
  private InputAction jumpAction;
  private InputAction sprintAction;
  private InputAction attackAction;

  void Awake()
  {
    var playerInput = GetComponent<PlayerInput>();
    moveAction = playerInput.actions["Move"];
    jumpAction = playerInput.actions["Jump"];
    sprintAction = playerInput.actions["Sprint"];
    attackAction = playerInput.actions["Attack"];
  }

  void Update()
  {
    Vector2 move = moveAction.ReadValue<Vector2>();
    Yaw = move.x;
    Pitch = move.y;

    float throttleUp = jumpAction.IsPressed() ? 1f : 0f;
    float throttleDown = sprintAction.IsPressed() ? 1f : 0f;
    Throttle = throttleUp - throttleDown;

    ShootPressed = attackAction.WasPressedThisFrame();
  }
}
