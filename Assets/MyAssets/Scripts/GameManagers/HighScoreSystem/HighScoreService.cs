using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public class HighScoreEntry
{
    public string name;
    public int score;
}

[Serializable]
public class HighScoreData
{
    public List<HighScoreEntry> entries = new();
}

public class HighScoreService
{
    const int MaxEntries = 5;
    readonly string filePath;

    public HighScoreData Data { get; private set; } = new HighScoreData();

    public HighScoreService(string fileName = "highscores.json")
    {
        filePath = Path.Combine(Application.persistentDataPath, fileName);
        Load();
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Data = new HighScoreData();
                Save();
                return;
            }

            string json = File.ReadAllText(filePath);
            Data = JsonUtility.FromJson<HighScoreData>(json) ?? new HighScoreData();
            Data.entries ??= new List<HighScoreEntry>();

            SortAndTrim();
        }
        catch
        {
            // Corrupt File -> Reset
            Data = new HighScoreData();
            Save();
        }
    }

    public void Save()
    {
        SortAndTrim();
        string json = JsonUtility.ToJson(Data, true);
        File.WriteAllText(filePath, json);
    }

    public IReadOnlyList<HighScoreEntry> GetTop() => Data.entries;

    public bool IsHighScore(int score)
    {
        if(Data.entries.Count < MaxEntries) return true;
        return score > Data.entries[Data.entries.Count - 1].score;
    }

    public void AddHighScore(string name, int score)
    {
        name = SanitizeName(name);

        Data.entries.Add(new HighScoreEntry { name = name, score = score });
        SortAndTrim();
        Save();
    }

    void SortAndTrim()
    {
       Data.entries = Data.entries
            .OrderByDescending(e => e.score)
            .Take(MaxEntries)
            .ToList();
    }

    static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "AAA";
        name = new string(name.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        if(name.Length == 0) return "AAA";
        if(name.Length > 3) name = name.Substring(0, 3);
        if(name.Length < 3) name = name.PadRight(3, 'A');
        return name;
    }
}
