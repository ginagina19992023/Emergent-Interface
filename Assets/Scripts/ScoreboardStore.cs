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

    public static bool TryRenameEntry(string existingTeamName, string newTeamName, out string errorMessage)
    {
        string existingNormalized = NormalizeTeamName(existingTeamName);
        string newNormalized = NormalizeTeamName(newTeamName);

        if (string.IsNullOrEmpty(existingNormalized))
        {
            errorMessage = "Invalid existing team name.";
            return false;
        }

        if (string.IsNullOrEmpty(newNormalized))
        {
            errorMessage = "Team name is required.";
            return false;
        }

        var data = Load();
        int entryIndex = -1;
        for (int i = 0; i < data.entries.Count; i++)
        {
            if (NormalizeTeamName(data.entries[i].teamName) == existingNormalized)
            {
                entryIndex = i;
                break;
            }
        }

        if (entryIndex < 0)
        {
            errorMessage = "Team not found.";
            return false;
        }

        for (int i = 0; i < data.entries.Count; i++)
        {
            if (i == entryIndex)
                continue;

            if (NormalizeTeamName(data.entries[i].teamName) == newNormalized)
            {
                errorMessage = "This team name is already taken.";
                return false;
            }
        }

        data.entries[entryIndex].teamName = newTeamName.Trim();
        Save(data);
        errorMessage = null;
        return true;
    }

    public static bool TryDeleteEntry(string teamName, out string errorMessage)
    {
        string normalized = NormalizeTeamName(teamName);
        if (string.IsNullOrEmpty(normalized))
        {
            errorMessage = "Invalid team name.";
            return false;
        }

        var data = Load();
        for (int i = 0; i < data.entries.Count; i++)
        {
            if (NormalizeTeamName(data.entries[i].teamName) != normalized)
                continue;

            data.entries.RemoveAt(i);
            Save(data);
            errorMessage = null;
            return true;
        }

        errorMessage = "Team not found.";
        return false;
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
