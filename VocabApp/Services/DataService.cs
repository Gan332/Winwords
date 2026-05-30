using System.Text.Json;
using VocabApp.Models;

namespace VocabApp.Services;

public class DataService
{
    private const string BanksKey = "vocab_banks";
    private const string HistoryKey = "vocab_history";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public List<WordBank> GetBanks()
    {
        var json = GetSetting(BanksKey);
        return string.IsNullOrEmpty(json)
            ? new List<WordBank>()
            : JsonSerializer.Deserialize<List<WordBank>>(json, JsonOptions) ?? new List<WordBank>();
    }

    public void SaveBanks(List<WordBank> banks)
    {
        var json = JsonSerializer.Serialize(banks, JsonOptions);
        SetSetting(BanksKey, json);
    }

    public void SaveBank(WordBank bank)
    {
        var banks = GetBanks();
        var existing = banks.FindIndex(b => b.Name == bank.Name);
        if (existing >= 0)
            banks[existing] = bank;
        else
            banks.Add(bank);
        SaveBanks(banks);
    }

    public void DeleteBank(string name)
    {
        var banks = GetBanks();
        banks.RemoveAll(b => b.Name == name);
        SaveBanks(banks);
    }

    public WordBank? GetBank(string name)
    {
        return GetBanks().FirstOrDefault(b => b.Name == name);
    }

    public List<StudyHistory> GetHistory()
    {
        var json = GetSetting(HistoryKey);
        return string.IsNullOrEmpty(json)
            ? new List<StudyHistory>()
            : JsonSerializer.Deserialize<List<StudyHistory>>(json, JsonOptions) ?? new List<StudyHistory>();
    }

    public void AddHistory(StudyHistory entry)
    {
        var history = GetHistory();
        history.Insert(0, entry);
        if (history.Count > 200)
            history = history.Take(200).ToList();
        var json = JsonSerializer.Serialize(history, JsonOptions);
        SetSetting(HistoryKey, json);
    }

    public static List<Card> ParseTxt(string text)
    {
        var cards = new List<Card>();
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            string[]? parts = null;

            if (trimmed.Contains('\t')) parts = trimmed.Split('\t');
            else if (trimmed.Contains(" - ")) parts = trimmed.Split(new[] { " - " }, StringSplitOptions.None);
            else if (trimmed.Contains(" | ")) parts = trimmed.Split(new[] { " | " }, StringSplitOptions.None);
            else if (trimmed.Contains('|')) parts = trimmed.Split('|');
            else if (trimmed.Contains('\uff1a')) parts = trimmed.Split('\uff1a');
            else if (trimmed.Contains(':')) { var idx = trimmed.IndexOf(':'); if (idx > 0) parts = [trimmed[..idx].Trim(), trimmed[(idx + 1)..].Trim()]; }

            if (parts is { Length: >= 2 })
            {
                cards.Add(new Card
                {
                    Word = parts[0].Trim(),
                    Definition = string.Join(" ", parts[1..]).Trim()
                });
            }
        }
        return cards;
    }

    private static string GetSetting(string key)
    {
        try
        {
            return Windows.Storage.ApplicationData.Current.LocalSettings.Values[key] as string ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void SetSetting(string key, string value)
    {
        try
        {
            Windows.Storage.ApplicationData.Current.LocalSettings.Values[key] = value;
        }
        catch { }
    }
}
