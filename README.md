# VocabApp — WinUI 3 背单词应用

将 [vocab-app](https://github.com/fish/vocab-app) (HTML/Cordova) 重构为 WinUI 3 桌面应用。

## 功能

- 📖 **词库管理** — 导入 TXT 词库文件（支持 `Tab / - / | / ：` 等分隔符）
- 🎴 **闪卡模式** — 看单词翻卡显示释义，标记记住/忘记
- 📝 **答题模式** — 根据释义选择对应单词的四选一题型
- 📊 **学习统计** — 学习次数、平均正确率、总学习时长
- 🌓 **暗色模式** — 跟随系统主题自动切换
- ⌨️ **快捷键支持** — 空格翻卡，1/F 忘记，2/J 记住

## 构建

本项目使用 GitHub Actions 自动构建，构建产物为 MSIX 安装包。

### 在 GitHub Actions 中构建

1. Fork 或推送代码到 GitHub 仓库
2. 进入 Actions 页面，选择 **Build WinUI3 App** workflow
3. 点击 **Run workflow** 手动触发，或推送代码自动触发
4. 构建完成后在 Artifacts 下载对应平台的安装包

### 手动构建（需安装 .NET 8 SDK + VS 2022 + Windows App SDK）

```bash
# 还原
nuget restore VocabApp.sln

# 构建
msbuild VocabApp\VocabApp.csproj /p:Configuration=Release /p:Platform=x64
```

## 项目结构

```
VocabApp/
├── Models/          # 数据模型 (Card, WordBank, LearnSession, StudyHistory)
├── ViewModels/      # MVVM ViewModel (MainViewModel)
├── Views/           # XAML 视图
├── Services/        # 数据持久化 (DataService)
├── Helpers/         # 转换器 (BoolInvertConverter)
├── App.xaml         # 全局资源和主题
├── MainWindow.xaml  # 主窗口布局
└── VocabApp.csproj  # 项目文件
```
