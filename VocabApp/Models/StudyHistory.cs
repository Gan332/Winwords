namespace VocabApp.Models;

public class StudyHistory
{
    public string BankName { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Remembered { get; set; }
    public double Accuracy { get; set; }
    public int DurationSeconds { get; set; }
    public StudyMode Mode { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
}
