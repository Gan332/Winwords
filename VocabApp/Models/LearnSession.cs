namespace VocabApp.Models;

public enum StudyMode
{
    Flashcard,
    Quiz
}

public enum StudyOrder
{
    Sequential,
    Shuffled
}

public class LearnSession
{
    public WordBank Bank { get; set; } = null!;
    public StudyMode Mode { get; set; }
    public StudyOrder Order { get; set; }
    public List<Card> Cards { get; set; } = new();
    public int CurrentIndex { get; set; }
    public int Remembered { get; set; }
    public int Forgot { get; set; }
    public bool Answered { get; set; }
    public DateTime StartTime { get; set; } = DateTime.Now;
    public TimeSpan Duration => DateTime.Now - StartTime;
    public bool IsFinished => CurrentIndex >= Cards.Count;
    public double Accuracy => (Remembered + Forgot) > 0
        ? Math.Round((double)Remembered / (Remembered + Forgot) * 100, 1)
        : 0;

    public Card CurrentCard => CurrentIndex < Cards.Count ? Cards[CurrentIndex] : null!;
}
