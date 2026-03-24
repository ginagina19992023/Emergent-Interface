using UnityEngine;

/// <summary>
/// Marker for the object that holds scene-wide gameplay state (<see cref="PlayerScore"/>, <see cref="PlayerHealth"/>, etc.).
/// Keeps data off the helicopter prefab while UI and gates resolve these via references or <c>FindFirstObjectByType</c>.
/// </summary>
public class GameState : MonoBehaviour { }
