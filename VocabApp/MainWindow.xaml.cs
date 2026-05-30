using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using VocabApp.Models;
using VocabApp.ViewModels;

namespace VocabApp;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new();

    public MainWindow()
    {
        this.InitializeComponent();
        LoadBanksToCombo();
    }

    private void LoadBanksToCombo()
    {
        BankComboBox.Items.Clear();
        foreach (var bank in ViewModel.Banks)
            BankComboBox.Items.Add(bank.Name);
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var item = args.SelectedItem as NavigationViewItem;
        var tag = item?.Tag?.ToString();

        BanksPage.Visibility = Visibility.Collapsed;
        LearnPage.Visibility = Visibility.Collapsed;
        StatsPage.Visibility = Visibility.Collapsed;

        switch (tag)
        {
            case "banks":
                BanksPage.Visibility = Visibility.Visible;
                break;
            case "learn":
                LearnPage.Visibility = Visibility.Visible;
                if (!ViewModel.ShowLearnSession && !ViewModel.ShowLearnResult)
                {
                    ViewModel.ShowLearnSetup = true;
                    LearnSetupPanel.Visibility = Visibility.Visible;
                    LoadBanksToCombo();
                }
                break;
            case "stats":
                StatsPage.Visibility = Visibility.Visible;
                ViewModel.LoadHistory();
                ViewModel.UpdateStats();
                break;
        }
    }

    private async void BtnImport_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker
        {
            ViewMode = Windows.Storage.Pickers.PickerViewMode.List,
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add(".txt");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            ViewModel.ImportFile(file.Path);
            LoadBanksToCombo();
            ShowStatus(ViewModel.StatusMessage);
        }
    }

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "导入 TXT 词库";
        ((Border)sender).BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            Microsoft.UI.Colors.Indigo);
    }

    private void DropZone_DragLeave(object sender, DragEventArgs e)
    {
        ((Border)sender).BorderBrush = Application.Current.Resources["CardStrokeColorDefault"] as Microsoft.UI.Xaml.Media.Brush
            ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
    }

    private async void DropZone_Drop(object sender, DragEventArgs e)
    {
        ((Border)sender).BorderBrush = Application.Current.Resources["CardStrokeColorDefault"] as Microsoft.UI.Xaml.Media.Brush
            ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);

        if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count > 0 && items[0] is Windows.Storage.StorageFile file)
            {
                if (file.FileType == ".txt")
                {
                    ViewModel.ImportFile(file.Path);
                    LoadBanksToCombo();
                    ShowStatus(ViewModel.StatusMessage);
                }
            }
        }
    }

    private void BtnStudyBank_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string name)
        {
            ViewModel.StudyBank(name);
            NavView.SelectedItem = NavLearn;
        }
    }

    private void BtnDeleteBank_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string name)
        {
            ViewModel.DeleteBank(name);
            LoadBanksToCombo();
        }
    }

    private void BankComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BankComboBox.SelectedItem is string name)
            ViewModel.LearnBank = ViewModel.Banks.FirstOrDefault(b => b.Name == name);
    }

    private void ModeFlashcard_Checked(object sender, RoutedEventArgs e) => ViewModel.SelectedMode = StudyMode.Flashcard;
    private void ModeQuiz_Checked(object sender, RoutedEventArgs e) => ViewModel.SelectedMode = StudyMode.Quiz;

    private void BtnStartLearn_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.LearnBank == null)
        {
            ShowStatus("⚠️ 请先选择一个词库");
            return;
        }
        ViewModel.StartLearn();
        SyncLearnUI();
    }

    private void BtnFlip_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.FlipCardCommand.Execute(null);
        SyncFlashcard();
    }

    private void BtnForgot_Click(object sender, RoutedEventArgs e) => AnswerCard(false);
    private void BtnRemember_Click(object sender, RoutedEventArgs e) => AnswerCard(true);

    private void AnswerCard(bool remembered)
    {
        ViewModel.AnswerCardCommand.Execute(remembered);
        if (ViewModel.ShowLearnResult)
        {
            SyncResultUI();
        }
        else
        {
            SyncFlashcard();
        }
    }

    private void QuizOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is QuizOption opt)
        {
            ViewModel.HandleQuizAnswer(opt);
            SyncQuiz();
        }
    }

    private void BtnNextQuiz_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NextQuizCardCommand.Execute(null);
        if (ViewModel.ShowLearnResult)
            SyncResultUI();
        else
            SyncQuiz();
    }

    private void BtnPause_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.TogglePauseCommand.Execute(null);
    }

    private void BtnQuit_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.QuitToSetupCommand.Execute(null);
        LearnSetupPanel.Visibility = Visibility.Visible;
        LearnSessionPanel.Visibility = Visibility.Collapsed;
        ResultPanel.Visibility = Visibility.Collapsed;
    }

    private void BtnAgain_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.StudyAgainCommand.Execute(null);
        SyncLearnUI();
    }

    private void BtnBackToSetup_Click(object sender, RoutedEventArgs e)
    {
        LearnSetupPanel.Visibility = Visibility.Visible;
        LearnSessionPanel.Visibility = Visibility.Collapsed;
        ResultPanel.Visibility = Visibility.Collapsed;
        ViewModel.ShowLearnSetup = true;
        LoadBanksToCombo();
    }

    private void SyncLearnUI()
    {
        LearnSetupPanel.Visibility = ViewModel.ShowLearnSetup ? Visibility.Visible : Visibility.Collapsed;
        LearnSessionPanel.Visibility = ViewModel.ShowLearnSession ? Visibility.Visible : Visibility.Collapsed;
        ResultPanel.Visibility = ViewModel.ShowLearnResult ? Visibility.Visible : Visibility.Collapsed;

        if (ViewModel.ShowLearnSession)
        {
            if (ViewModel.SelectedMode == StudyMode.Flashcard)
            {
                FlashcardPanel.Visibility = Visibility.Visible;
                FlashcardActions.Visibility = Visibility.Visible;
                QuizPanel.Visibility = Visibility.Collapsed;
                SyncFlashcard();
            }
            else
            {
                FlashcardPanel.Visibility = Visibility.Collapsed;
                FlashcardActions.Visibility = Visibility.Collapsed;
                QuizPanel.Visibility = Visibility.Visible;
                SyncQuiz();
            }
        }
    }

    private void SyncFlashcard()
    {
        if (ViewModel.ShowDefinition)
        {
            DefinitionPanel.Visibility = Visibility.Visible;
            BtnFlip.Visibility = Visibility.Collapsed;
            FlashcardAnswerButtons.Visibility = Visibility.Visible;
        }
        else
        {
            DefinitionPanel.Visibility = Visibility.Collapsed;
            BtnFlip.Visibility = Visibility.Visible;
            FlashcardAnswerButtons.Visibility = Visibility.Collapsed;
        }
    }

    private void SyncQuiz()
    {
        BtnNextQuiz.Visibility = ViewModel.IsQuizAnswered ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SyncResultUI()
    {
        LearnSetupPanel.Visibility = Visibility.Collapsed;
        LearnSessionPanel.Visibility = Visibility.Collapsed;
        ResultPanel.Visibility = Visibility.Visible;
    }

    private void ShowStatus(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        StatusText.Text = message;
        StatusBorder.Visibility = Visibility.Visible;
        var timer = new Microsoft.UI.Dispatching.DispatcherTimer();
        timer.Interval = TimeSpan.FromSeconds(3);
        timer.Tick += (s, e) =>
        {
            StatusBorder.Visibility = Visibility.Collapsed;
            timer.Stop();
        };
        timer.Start();
    }

    // Keyboard shortcuts
    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        // We use the window's Content control instead
    }

    private void SetupKeyboardShortcuts()
    {
        // Hook keyboard on the root element
        if (Content is FrameworkElement root)
        {
            root.KeyDown += (s, e) =>
            {
                if (!ViewModel.ShowLearnSession) return;

                if (e.Key == Windows.System.VirtualKey.Escape || e.Key == Windows.System.VirtualKey.Space)
                {
                    if (ViewModel.SelectedMode == StudyMode.Flashcard)
                    {
                        if (!ViewModel.ShowDefinition)
                        {
                            ViewModel.FlipCardCommand.Execute(null);
                            SyncFlashcard();
                            e.Handled = true;
                        }
                        else if (e.Key == Windows.System.VirtualKey.Space)
                        {
                            e.Handled = true;
                        }
                    }
                    else if (ViewModel.SelectedMode == StudyMode.Quiz && !ViewModel.IsQuizAnswered)
                    {
                        // Space in quiz is no-op, let it pass
                    }
                }
                else if (e.Key == Windows.System.VirtualKey.Number1 || e.Key == Windows.System.VirtualKey.F)
                {
                    if (ViewModel.ShowDefinition && ViewModel.SelectedMode == StudyMode.Flashcard)
                        AnswerCard(false);
                }
                else if (e.Key == Windows.System.VirtualKey.Number2 || e.Key == Windows.System.VirtualKey.J)
                {
                    if (ViewModel.ShowDefinition && ViewModel.SelectedMode == StudyMode.Flashcard)
                        AnswerCard(true);
                }
                else if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    if (ViewModel.SelectedMode == StudyMode.Flashcard && !ViewModel.ShowDefinition)
                    {
                        ViewModel.FlipCardCommand.Execute(null);
                        SyncFlashcard();
                        e.Handled = true;
                    }
                }
            };
        }
    }
}
