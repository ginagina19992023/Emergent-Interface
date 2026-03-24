using UnityEngine;

/// <summary>
/// Elapsed gameplay time in seconds. Uses <see cref="Time.deltaTime"/> so it stops when <c>timeScale</c> is 0 (game over / level complete).
/// </summary>
public class GameTimer : MonoBehaviour
{
  public float ElapsedSeconds { get; private set; }

  void Update()
  {
    ElapsedSeconds += Time.deltaTime;
  }
}
