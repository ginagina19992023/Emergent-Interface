using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows <see cref="PlayerScore"/> on a UI <see cref="Text"/>. Assign an existing Text or leave empty to create one at runtime.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class ScoreDisplay : MonoBehaviour
{
  [SerializeField] private Text scoreText;
  [SerializeField] private PlayerScore playerScore;
  [SerializeField] private string prefix = "Score: ";
  [SerializeField] private int fontSize = 28;

  void Awake()
  {
    EnsureScoreText();
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

  private void EnsureScoreText()
  {
    if (scoreText != null)
      return;

    GameObject textGo = new GameObject("ScoreText");
    textGo.transform.SetParent(transform, false);
    RectTransform rt = textGo.AddComponent<RectTransform>();
    rt.anchorMin = new Vector2(0f, 1f);
    rt.anchorMax = new Vector2(0f, 1f);
    rt.pivot = new Vector2(0f, 1f);
    rt.anchoredPosition = new Vector2(24f, -24f);
    rt.sizeDelta = new Vector2(480f, 56f);

    scoreText = textGo.AddComponent<Text>();
    Font font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Helvetica" }, fontSize);
    if (font == null)
      font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    scoreText.font = font;
    scoreText.fontSize = fontSize;
    scoreText.color = Color.white;
    scoreText.alignment = TextAnchor.UpperLeft;
    scoreText.text = prefix + "0";
  }

  private void Refresh(int score)
  {
    if (scoreText != null)
      scoreText.text = prefix + score.ToString();
  }
}
