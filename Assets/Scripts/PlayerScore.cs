using System;
using UnityEngine;

/// <summary>
/// Tracks the player's score. Lives on the <see cref="GameState"/> object (or any single scene singleton). Other systems add points via <see cref="AddScore"/> or <see cref="Instance"/>.
/// </summary>
public class PlayerScore : MonoBehaviour
{
  public static PlayerScore Instance { get; private set; }

  public int Score { get; private set; }
  public event Action<int> OnScoreChanged;

  void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Debug.LogWarning("Multiple PlayerScore components in the scene; destroying duplicate.");
      Destroy(this);
      return;
    }
    Instance = this;
  }

  void OnDestroy()
  {
    if (Instance == this)
      Instance = null;
  }

  public void AddScore(int amount)
  {
    if (amount == 0)
      return;
    Score += amount;
    OnScoreChanged?.Invoke(Score);
  }
}
