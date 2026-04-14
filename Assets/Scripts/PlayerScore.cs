using System;
using UnityEngine;

/// <summary>
/// Tracks the player's score. Lives on the <see cref="GameState"/> object (or any single scene singleton). Other systems add points via <see cref="AddScore"/> or <see cref="Instance"/>.
/// </summary>
public class PlayerScore : MonoBehaviour
{
  public static PlayerScore Instance { get; private set; }

  [Tooltip("Sound played whenever score is awarded.")]
  [SerializeField] private AudioClip scoreSound;

  [Tooltip("Volume for the score sound.")]
  [Range(0f, 3f)]
  [SerializeField] private float scoreSoundVolume = 2f;

  [Tooltip("Optional AudioSource for score SFX. If empty, one is created and configured as 2D.")]
  [SerializeField] private AudioSource scoreAudioSource;

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

    if (scoreAudioSource == null)
      scoreAudioSource = GetComponent<AudioSource>();
    if (scoreAudioSource == null)
      scoreAudioSource = gameObject.AddComponent<AudioSource>();

    scoreAudioSource.playOnAwake = false;
    scoreAudioSource.spatialBlend = 0f;
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

    if (scoreSound != null && scoreAudioSource != null)
      scoreAudioSource.PlayOneShot(scoreSound, scoreSoundVolume);
  }
}
