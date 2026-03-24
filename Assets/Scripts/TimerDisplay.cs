using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows <see cref="GameTimer"/> on a UI <see cref="Text"/>, matching <see cref="ScoreDisplay"/> / <see cref="HealthBarDisplay"/> layout.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class TimerDisplay : MonoBehaviour
{
  [SerializeField] private Text timerText;
  [SerializeField] private GameTimer gameTimer;
  [SerializeField] private string prefix = "Timer: ";
  [SerializeField] private int fontSize = 28;

  void Awake()
  {
    EnsureTimerText();
  }

  void Start()
  {
    if (gameTimer == null)
      gameTimer = FindFirstObjectByType<GameTimer>();
  }

  void Update()
  {
    if (gameTimer == null)
      return;
    Refresh(gameTimer.ElapsedSeconds);
  }

  private void EnsureTimerText()
  {
    if (timerText != null)
      return;

    GameObject textGo = new GameObject("TimerText");
    textGo.transform.SetParent(transform, false);
    RectTransform rt = textGo.AddComponent<RectTransform>();
    rt.anchorMin = new Vector2(0f, 1f);
    rt.anchorMax = new Vector2(0f, 1f);
    rt.pivot = new Vector2(0f, 1f);
    // Stack below HealthBarDisplay row (56px line height).
    rt.anchoredPosition = new Vector2(24f, -136f);
    rt.sizeDelta = new Vector2(480f, 56f);

    timerText = textGo.AddComponent<Text>();
    Font font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Helvetica" }, fontSize);
    if (font == null)
      font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    timerText.font = font;
    timerText.fontSize = fontSize;
    timerText.color = Color.white;
    timerText.alignment = TextAnchor.UpperLeft;
    timerText.text = prefix + "0:00";
  }

  private void Refresh(float elapsedSeconds)
  {
    if (timerText == null)
      return;
    int total = Mathf.FloorToInt(Mathf.Max(0f, elapsedSeconds));
    int m = total / 60;
    int s = total % 60;
    timerText.text = prefix + m.ToString() + ":" + s.ToString("00");
  }
}
