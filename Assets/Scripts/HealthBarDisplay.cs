using UnityEngine;

/// <summary>
/// Shows lives by toggling one of six state objects (0Lives..5Lives) based on <see cref="PlayerHealth"/>.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class HealthBarDisplay : MonoBehaviour
{
  [SerializeField] private PlayerHealth playerHealth;
  [SerializeField] private GameObject zeroLives;
  [SerializeField] private GameObject oneLives;
  [SerializeField] private GameObject twoLives;
  [SerializeField] private GameObject threeLives;
  [SerializeField] private GameObject fourLives;
  [SerializeField] private GameObject fiveLives;

  private readonly GameObject[] liveStates = new GameObject[6];

  void Awake()
  {
    CacheLiveStates();
    AutoFindMissingLiveStates();
  }

  void Start()
  {
    if (playerHealth == null)
      playerHealth = FindFirstObjectByType<PlayerHealth>();

    if (playerHealth != null)
    {
      playerHealth.OnHealthChanged += Refresh;
      Refresh(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }
    else
      Refresh(0, 1);
  }

  void OnDestroy()
  {
    if (playerHealth != null)
      playerHealth.OnHealthChanged -= Refresh;
  }

  private void CacheLiveStates()
  {
    liveStates[0] = zeroLives;
    liveStates[1] = oneLives;
    liveStates[2] = twoLives;
    liveStates[3] = threeLives;
    liveStates[4] = fourLives;
    liveStates[5] = fiveLives;
  }

  private void AutoFindMissingLiveStates()
  {
    for (int i = 0; i < liveStates.Length; i++)
    {
      if (liveStates[i] == null)
        liveStates[i] = FindLiveState(i);
    }

    zeroLives = liveStates[0];
    oneLives = liveStates[1];
    twoLives = liveStates[2];
    threeLives = liveStates[3];
    fourLives = liveStates[4];
    fiveLives = liveStates[5];
  }

  private GameObject FindLiveState(int lives)
  {
    Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    string expectedName = lives + "Lives";
    for (int i = 0; i < allTransforms.Length; i++)
    {
      if (allTransforms[i].name == expectedName)
        return allTransforms[i].gameObject;
    }
    return null;
  }

  private void Refresh(int current, int _)
  {
    int clampedLives = Mathf.Clamp(current, 0, liveStates.Length - 1);
    for (int i = 0; i < liveStates.Length; i++)
    {
      if (liveStates[i] != null)
        liveStates[i].SetActive(i == clampedLives);
    }
  }
}
