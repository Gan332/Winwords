using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VocabApp.Models;
using VocabApp.Services;

namespace VocabApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DataService _dataService = new();

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private bool _isPaneOpen;

    // Word Banks
    public ObservableCollection<WordBank> Banks { get; } = new();

    [ObservableProperty]
    private WordBank? _selectedBank;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isDragging;

    // Learn Setup
    [ObservableProperty]
    private WordBank? _learnBank;

    [ObservableProperty]
    private bool _showLearnSetup = true;

    [ObservableProperty]
    private bool _showLearnSession;

    [ObservableProperty]
    private bool _showLearnResult;

    [ObservableProperty]
    private StudyMode _selectedMode = StudyMode.Flashcard;

    [ObservableProperty]
    private StudyOrder _selectedOrder = StudyOrder.Shuffled;

    [ObservableProperty]
    private bool _showDefinition;

    public ObservableCollection<WordBank> AvailableBanks { get; } = new();

    // Learn Session
    [ObservableProperty]
    private LearnSession? _session;

    [ObservableProperty]
    private int _currentIndex;

    [ObservableProperty]
    private int _totalCards;

    [ObservableProperty]
    private int _remembered;

    [ObservableProperty]
    private int _forgot;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _currentWord = string.Empty;

    [ObservableProperty]
    private string _currentDefinition = string.Empty;

    [ObservableProperty]
    private string _timerText = "00:00";

    [ObservableProperty]
    private bool _isPaused;

    // Quiz
    [ObservableProperty]
    private string _question = string.Empty;

    [ObservableProperty]
    private string _questionLabel = string.Empty;

    [ObservableProperty]
    private ObservableCollection<QuizOption> _quizOptions = new();

    [ObservableProperty]
    private bool _isQuizAnswered;

    // Result
    [ObservableProperty]
    private string _resultTitle = string.Empty;

    [ObservableProperty]
    private int _resultTotal;

    [ObservableProperty]
    private int _resultRemembered;

    [ObservableProperty]
    private int _resultForgot;

    [ObservableProperty]
    private double _resultAccuracy;

    [ObservableProperty]
    private string _resultDuration = string.Empty;

    // Stats
    [ObservableProperty]
    private int _statSessions;

    [ObservableProperty]
    private double _statAvgAccuracy;

    [ObservableProperty]
    private int _statTotalMinutes;

    public ObservableCollection<StudyHistory> History { get; } = new();

    private Timer? _timer;
    private DateTime _sessionStart;
    private readonly Random _random = new();

    public MainViewModel()
    {
        LoadBanks();
    }

    public void LoadBanks()
    {
        Banks.Clear();
        foreach (var b in _dataService.GetBanks())
            Banks.Add(b);

        AvailableBanks.Clear();
        foreach (var b in _dataService.GetBanks())
            AvailableBanks.Add(b);

        LoadHistory();
        UpdateStats();
    }

    public void LoadHistory()
    {
        History.Clear();
        foreach (var h in _dataService.GetHistory())
            History.Add(h);
    }

    public void UpdateStats()
    {
        var history = _dataService.GetHistory();
        StatSessions = history.Count;
        StatAvgAccuracy = history.Count > 0 ? Math.Round(history.Average(h => h.Accuracy), 1) : 0;
        StatTotalMinutes = history.Count > 0 ? (int)Math.Round(history.Sum(h => h.DurationSeconds) / 60.0) : 0;
    }

    [RelayCommand]
    public void ImportFile(string filePath)
    {
        try
        {
            var text = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            var cards = DataService.ParseTxt(text);

            if (cards.Count == 0)
            {
                StatusMessage = "未解析到有效词条，请确保每行格式为：单词 - 释义";
                return;
            }

            var name = Path.GetFileNameWithoutExtension(filePath);
            var bank = new WordBank
            {
                Name = name,
                Cards = cards,
                UpdatedAt = DateTime.Now
            };
            _dataService.SaveBank(bank);
            LoadBanks();
            StatusMessage = $"✅ 成功导入「{name}」({cards.Count} 个单词)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ 导入失败: {ex.Message}";
        }
    }

    [RelayCommand]
    public void DeleteBank(string name)
    {
        _dataService.DeleteBank(name);
        LoadBanks();
        StatusMessage = $"已删除词库「{name}」";
    }

    [RelayCommand]
    public void StudyBank(string name)
    {
        var bank = _dataService.GetBank(name);
        if (bank == null) return;

        LearnBank = bank;
        SelectedTabIndex = 1;
    }

    [RelayCommand]
    public void StartLearn()
    {
        if (LearnBank == null || LearnBank.Cards.Count == 0) return;

        var cards = SelectedOrder == StudyOrder.Shuffled
            ? LearnBank.Cards.OrderBy(_ => _random.Next()).ToList()
            : new List<Card>(LearnBank.Cards);

        Session = new LearnSession
        {
            Bank = LearnBank,
            Mode = SelectedMode,
            Order = SelectedOrder,
            Cards = cards,
            CurrentIndex = 0,
            Remembered = 0,
            Forgot = 0,
            StartTime = DateTime.Now
        };

        ShowLearnSetup = false;
        ShowLearnSession = true;
        ShowLearnResult = false;

        _sessionStart = DateTime.Now;
        ShowDefinition = false;
        IsQuizAnswered = false;

        if (SelectedMode == StudyMode.Flashcard)
            ShowFlashcard();
        else
            ShowQuiz();

        StartTimer();
    }

    private void StartTimer()
    {
        _timer?.Dispose();
        _timer = new Timer(_ =>
        {
            var elapsed = DateTime.Now - _sessionStart;
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                TimerText = $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
            });
        }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    private void StopTimer()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void ShowFlashcard()
    {
        if (Session == null || Session.IsFinished) return;

        var card = Session.CurrentCard;
        CurrentWord = card.Word;
        CurrentDefinition = card.Definition;
        ShowDefinition = false;
        Session.Answered = false;
        UpdateProgress();
    }

    private void ShowQuiz()
    {
        if (Session == null || Session.IsFinished) return;

        var card = Session.CurrentCard;
        Question = card.Definition;
        QuestionLabel = "请选择对应的单词";

        // Generate options: 1 correct + 3 random wrong
        var allCards = Session.Bank.Cards;
        var wrongOptions = allCards
            .Where(c => c.Word != card.Word)
            .OrderBy(_ => _random.Next())
            .Take(3)
            .Select(c => c.Word)
            .ToList();

        var options = new List<string> { card.Word };
        options.AddRange(wrongOptions);
        // Shuffle
        options = options.OrderBy(_ => _random.Next()).ToList();

        QuizOptions.Clear();
        foreach (var opt in options)
            QuizOptions.Add(new QuizOption { Text = opt, IsCorrect = opt == card.Word });
        IsQuizAnswered = false;
        Session.Answered = false;
        UpdateProgress();
    }

    private void UpdateProgress()
    {
        if (Session == null) return;
        CurrentIndex = Session.CurrentIndex + 1;
        TotalCards = Session.Cards.Count;
        Remembered = Session.Remembered;
        Forgot = Session.Forgot;
        Progress = Session.Cards.Count > 0 ? (double)Session.CurrentIndex / Session.Cards.Count : 0;
    }

    [RelayCommand]
    public void FlipCard()
    {
        ShowDefinition = true;
        if (Session != null)
            Session.Answered = true;
    }

    [RelayCommand]
    public void AnswerCard(bool remembered)
    {
        if (Session == null || !Session.Answered) return;

        if (remembered)
            Session.Remembered++;
        else
            Session.Forgot++;

        Session.CurrentIndex++;
        Remembered = Session.Remembered;
        Forgot = Session.Forgot;

        if (Session.IsFinished)
            FinishSession();
        else
            ShowFlashcard();
    }

    public void HandleQuizAnswer(QuizOption selected)
    {
        if (Session == null || IsQuizAnswered || selected.IsSelected) return;

        IsQuizAnswered = true;
        Session.Answered = true;
        selected.IsSelected = true;
        selected.IsCorrectOption = selected.IsCorrect;

        // Highlight correct/wrong
        foreach (var opt in QuizOptions)
        {
            if (opt.IsCorrect) opt.IsCorrectOption = true;
            opt.IsDisabled = true;
        }

        if (selected.IsCorrect)
            Session.Remembered++;
        else
            Session.Forgot++;

        Remembered = Session.Remembered;
        Forgot = Session.Forgot;
    }

    [RelayCommand]
    public void NextQuizCard()
    {
        if (Session == null) return;
        Session.CurrentIndex++;

        if (Session.IsFinished)
            FinishSession();
        else
            ShowQuiz();
    }

    [RelayCommand]
    public void TogglePause()
    {
        IsPaused = !IsPaused;
        if (IsPaused)
            StopTimer();
        else
            StartTimer();
    }

    [RelayCommand]
    public void SaveAndQuit()
    {
        StopTimer();
        if (Session != null)
        {
            var elapsed = DateTime.Now - _sessionStart;
            var history = new StudyHistory
            {
                BankName = Session.Bank.Name,
                Total = Session.Remembered + Session.Forgot,
                Remembered = Session.Remembered,
                Accuracy = Session.Accuracy,
                DurationSeconds = (int)elapsed.TotalSeconds,
                Mode = Session.Mode,
                Date = DateTime.Now
            };
            _dataService.AddHistory(history);
        }
        QuitToSetup();
    }

    [RelayCommand]
    public void QuitToSetup()
    {
        StopTimer();
        IsPaused = false;
        Session = null;
        ShowLearnSetup = true;
        ShowLearnSession = false;
        ShowLearnResult = false;
        LoadBanks();
        UpdateStats();
    }

    private void FinishSession()
    {
        StopTimer();
        if (Session == null) return;

        var elapsed = DateTime.Now - _sessionStart;
        var history = new StudyHistory
        {
            BankName = Session.Bank.Name,
            Total = Session.Remembered + Session.Forgot,
            Remembered = Session.Remembered,
            Accuracy = Session.Accuracy,
            DurationSeconds = (int)elapsed.TotalSeconds,
            Mode = Session.Mode,
            Date = DateTime.Now
        };
        _dataService.AddHistory(history);

        ShowLearnSession = false;
        ShowLearnResult = true;
        ResultTitle = $"学习完成！";
        ResultTotal = Session.Remembered + Session.Forgot;
        ResultRemembered = Session.Remembered;
        ResultForgot = Session.Forgot;
        ResultAccuracy = Session.Accuracy;
        ResultDuration = $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";

        UpdateStats();
    }

    [RelayCommand]
    public void StudyAgain()
    {
        ShowLearnResult = false;
        StartLearn();
    }
}

public partial class QuizOption : ObservableObject
{
    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private bool _isCorrect;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isCorrectOption;

    [ObservableProperty]
    private bool _isDisabled;
}
