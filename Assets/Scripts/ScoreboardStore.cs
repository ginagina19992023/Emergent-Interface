using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent scoreboard store backed by PlayerPrefs JSON.
/// Team names are unique (case-insensitive, trimmed).
/// </summary>
public static class ScoreboardStore
{
    const string PlayerPrefsKey = "PersistentTeamScoreboard";

    [Serializable]
    public class ScoreboardEntry
    {
        public string teamName;
        public int score;
    }

    [Serializable]
    class ScoreboardData
    {
        public List<ScoreboardEntry> entries = new List<ScoreboardEntry>();
    }

    public static IReadOnlyList<ScoreboardEntry> GetEntries()
    {
        var data = Load();
        return data.entries;
    }

    public static bool TryAddEntry(string teamName, int score, out string errorMessage)
    {
        string normalized = NormalizeTeamName(teamName);
        if (string.IsNullOrEmpty(normalized))
        {
            errorMessage = "Team name is required.";
            return false;
        }

        var data = Load();
        for (int i = 0; i < data.entries.Count; i++)
        {
            if (NormalizeTeamName(data.entries[i].teamName) == normalized)
            {
                errorMessage = "This team name is already taken.";
                return false;
            }
        }

        data.entries.Add(new ScoreboardEntry
        {
            teamName = teamName.Trim(),
            score = score
        });

        data.entries.Sort((a, b) => b.score.CompareTo(a.score));
        Save(data);

        errorMessage = null;
        return true;
    }

    public static string NormalizeTeamName(string teamName)
    {
        if (string.IsNullOrWhiteSpace(teamName))
            return string.Empty;

        return teamName.Trim().ToLowerInvariant();
    }

    static ScoreboardData Load()
    {
        if (!PlayerPrefs.HasKey(PlayerPrefsKey))
            return new ScoreboardData();

        string raw = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
            return new ScoreboardData();

        try
        {
            var data = JsonUtility.FromJson<ScoreboardData>(raw);
            if (data == null || data.entries == null)
                return new ScoreboardData();

            return data;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to parse scoreboard data. Resetting scoreboard. Details: {ex.Message}");
            return new ScoreboardData();
        }
    }

    static void Save(ScoreboardData data)
    {
        string raw = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(PlayerPrefsKey, raw);
        PlayerPrefs.Save();
    }
}
