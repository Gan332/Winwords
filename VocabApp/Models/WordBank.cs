namespace VocabApp.Models;

public class WordBank
{
    public string Name { get; set; } = string.Empty;
    public List<Card> Cards { get; set; } = new();
    public int Count => Cards.Count;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
