using UnityEngine;
using TMPro;

/// <summary>
/// Shows <see cref="PlayerScore"/> on a UI score label.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class ScoreDisplay : MonoBehaviour
{
  [SerializeField] private TMP_Text scoreText;
  [SerializeField] private PlayerScore playerScore;

  void Awake()
  {
    ResolveScoreText();
  }

  void Start()
  {
    if (playerScore == null)
      playerScore = FindFirstObjectByType<PlayerScore>();

    if (playerScore != null)
    {
      playerScore.OnScoreChanged += Refresh;
      Refresh(playerScore.Score);
    }
    else
      Refresh(0);
  }

  void OnDestroy()
  {
    if (playerScore != null)
      playerScore.OnScoreChanged -= Refresh;
  }

  private void ResolveScoreText()
  {
    if (scoreText != null)
      return;

    Transform scoreCounter = transform.Find("ScoreCounter");
    if (scoreCounter == null)
    {
      GameObject scoreCounterObject = GameObject.Find("ScoreCounter");
      if (scoreCounterObject != null)
        scoreCounter = scoreCounterObject.transform;
    }

    if (scoreCounter != null)
      scoreText = scoreCounter.GetComponent<TMP_Text>();

    if (scoreText == null)
      Debug.LogWarning("ScoreDisplay: Could not find TMP_Text on 'ScoreCounter'.");
  }

  private void Refresh(int score)
  {
    if (scoreText != null)
      scoreText.text = score.ToString();
  }
}
