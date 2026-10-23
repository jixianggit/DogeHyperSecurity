using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DogeHyperSecurity.Interop;
using DogeHyperSecurity.Services;
using Wpf.Ui.Controls;

namespace DogeHyperSecurity.Views;

public partial class WelcomeView : UserControl
{
    private bool _passwordVisible;
    private bool _accountEnabled = true;
    private bool _cardEnabled = true;
    private bool _cardMode = true;

    public WelcomeView()
    {
        InitializeComponent();
        Loaded += WelcomeView_Loaded;
    }

    private string CurrentPassword => _passwordVisible ? PlainPassword.Text : PasswordInput.Password;

    private async void WelcomeView_Loaded(object sender, RoutedEventArgs e)
    {
        _ = LoadBulletinAsync();

        LoginSettings settings = ApplySavedSettings();

        if (_accountEnabled
            && settings.AutoLoginAccount
            && !string.IsNullOrWhiteSpace(AccountBox.Text)
            && !string.IsNullOrEmpty(CurrentPassword))
        {
            SetLoginMode(false);
            await AccountLoginAsync();
        }
        else if (_cardEnabled && settings.AutoLoginCard && !string.IsNullOrWhiteSpace(CardKeyBox.Text))
        {
            SetLoginMode(true);
            await CardLoginAsync();
        }
        else if (_accountEnabled && !_cardEnabled)
        {
            SetLoginMode(false);
        }
        else if (_cardEnabled)
        {
            SetLoginMode(true);
        }
    }

    private async Task LoadBulletinAsync()
    {
        string bulletin = await Task.Run(QpAuth.GetBulletin);
        AnnouncementText.Text = string.IsNullOrWhiteSpace(bulletin) ? "暂无公告" : bulletin.Trim();
    }

    private LoginSettings ApplySavedSettings()
    {
        LoginSettings settings = LoginSettings.Load();

        AutoLoginCheck.IsChecked = settings.AutoLoginAccount;
        RememberAccountCheck.IsChecked = settings.RememberAccount;
        CardAutoLoginCheck.IsChecked = settings.AutoLoginCard;
        RememberCardCheck.IsChecked = settings.RememberCard;

        if (settings.RememberAccount || settings.AutoLoginAccount)
        {
            AccountBox.Text = settings.Account;
            PasswordInput.Password = settings.Password;
        }

        if (settings.RememberCard || settings.AutoLoginCard)
        {
            CardKeyBox.Text = settings.CardKey;
        }

        return settings;
    }

    private void AccountTab_Click(object sender, RoutedEventArgs e)
    {
        SetLoginMode(false);
    }

    private void CardTab_Click(object sender, RoutedEventArgs e)
    {
        SetLoginMode(true);
    }

    public void SetLoginModeAvailability(bool accountEnabled, bool cardEnabled)
    {
        _accountEnabled = accountEnabled;
        _cardEnabled = cardEnabled;

        AccountTab.Visibility = accountEnabled ? Visibility.Visible : Visibility.Collapsed;
        CardTab.Visibility = cardEnabled ? Visibility.Visible : Visibility.Collapsed;

        if (accountEnabled && cardEnabled)
        {
            Grid.SetColumn(AccountTab, 0);
            Grid.SetColumnSpan(AccountTab, 1);
            Grid.SetColumn(CardTab, 1);
            Grid.SetColumnSpan(CardTab, 1);
        }
        else if (cardEnabled)
        {
            Grid.SetColumn(CardTab, 0);
            Grid.SetColumnSpan(CardTab, 2);
        }
        else if (accountEnabled)
        {
            Grid.SetColumn(AccountTab, 0);
            Grid.SetColumnSpan(AccountTab, 2);
        }

        if (!accountEnabled && !cardEnabled)
        {
            AccountTab.Tag = null;
            CardTab.Tag = null;
            AccountPanel.Visibility = Visibility.Collapsed;
            CardPanel.Visibility = Visibility.Collapsed;
            return;
        }

        if (cardEnabled && !accountEnabled)
        {
            SetLoginMode(true);
        }
        else if (accountEnabled && !cardEnabled)
        {
            SetLoginMode(false);
        }
        else
        {
            SetLoginMode(_cardMode);
        }
    }

    private void SetLoginMode(bool cardMode)
    {
        _cardMode = cardMode;

        AccountTab.Tag = cardMode ? null : "Selected";
        CardTab.Tag = cardMode ? "Selected" : null;

        AccountPanel.Visibility = cardMode ? Visibility.Collapsed : Visibility.Visible;
        CardPanel.Visibility = cardMode ? Visibility.Visible : Visibility.Collapsed;
        CardStatus.Visibility = Visibility.Collapsed;

        if (cardMode)
        {
            CardKeyBox.Focus();
        }
    }

    private void AccountBox_GotFocus(object sender, RoutedEventArgs e)
    {
        AccountUnderline.Visibility = Visibility.Visible;
    }

    private void AccountBox_LostFocus(object sender, RoutedEventArgs e)
    {
        AccountUnderline.Visibility = Visibility.Collapsed;
    }

    private void Password_GotFocus(object sender, RoutedEventArgs e)
    {
        PasswordUnderline.Visibility = Visibility.Visible;
    }

    private void Password_LostFocus(object sender, RoutedEventArgs e)
    {
        PasswordUnderline.Visibility = Visibility.Collapsed;
    }

    private void CardKeyBox_GotFocus(object sender, RoutedEventArgs e)
    {
        CardKeyUnderline.Visibility = Visibility.Visible;
    }

    private void CardKeyBox_LostFocus(object sender, RoutedEventArgs e)
    {
        CardKeyUnderline.Visibility = Visibility.Collapsed;
    }

    private void TogglePassword_Click(object sender, RoutedEventArgs e)
    {
        _passwordVisible = !_passwordVisible;

        if (_passwordVisible)
        {
            PlainPassword.Text = PasswordInput.Password;
            PasswordInput.Visibility = Visibility.Collapsed;
            PlainPassword.Visibility = Visibility.Visible;
            EyeIcon.Symbol = SymbolRegular.EyeOff24;
            PlainPassword.Focus();
            PlainPassword.CaretIndex = PlainPassword.Text.Length;
        }
        else
        {
            PasswordInput.Password = PlainPassword.Text;
            PlainPassword.Visibility = Visibility.Collapsed;
            PasswordInput.Visibility = Visibility.Visible;
            EyeIcon.Symbol = SymbolRegular.Eye24;
        }
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(sender, AccountLoginButton))
        {
            await AccountLoginAsync();
        }
        else
        {
            await CardLoginAsync();
        }
    }

    private async Task AccountLoginAsync()
    {
        string user = AccountBox.Text.Trim();
        string password = CurrentPassword;
        if (user.Length == 0 || password.Length == 0)
        {
            return;
        }

        AccountLoginButton.IsEnabled = false;
        SetStatus(AccountStatus, "正在登录…", "#6B6B6B");

        try
        {
            QpResult result = await Task.Run(() => QpAuth.AccountLogin(user, password));
            if (result.Success)
            {
                SetStatus(AccountStatus, $"登录成功，剩余 {result.Message}", "#107C10");
                SaveAccountSettings();
            }
            else
            {
                SetStatus(AccountStatus, result.Message, "#C42B1C");
            }
        }
        finally
        {
            AccountLoginButton.IsEnabled = true;
        }
    }

    private async Task CardLoginAsync()
    {
        string key = CardKeyBox.Text.Trim();
        if (key.Length == 0)
        {
            return;
        }

        CardLoginButton.IsEnabled = false;
        SetStatus(CardStatus, "正在验证卡密…", "#6B6B6B");

        try
        {
            QpResult result = await Task.Run(() => QpAuth.CardLogin(key));
            if (result.Success)
            {
                string remain = string.IsNullOrWhiteSpace(result.Message) ? "未知" : result.Message;
                SaveCardSettings();

                // 授权交给外挂进程，避免同一卡密在两个进程重复登录
                QpAuth.Logout();
                QpResult launch = await Task.Run(() => GameLauncher.Launch(key));

                if (launch.Success)
                {
                    SetStatus(CardStatus, $"登录成功，剩余 {remain}，外挂已启动", "#107C10");
                    Application.Current?.Shutdown();
                }
                else
                {
                    SetStatus(CardStatus, $"授权成功，但外挂启动失败：{launch.Message}", "#C42B1C");
                }
            }
            else
            {
                string message = result.Message;
                if (message.Contains("绑定的机器", StringComparison.Ordinal))
                {
                    message += "，请再次登录";
                }

                SetStatus(CardStatus, message, "#C42B1C");
            }
        }
        finally
        {
            CardLoginButton.IsEnabled = true;
        }
    }

    private void SaveAccountSettings()
    {
        LoginSettings settings = LoginSettings.Load();
        bool remember = RememberAccountCheck.IsChecked == true;
        bool autoLogin = AutoLoginCheck.IsChecked == true;

        settings.RememberAccount = remember;
        settings.AutoLoginAccount = autoLogin;
        settings.LoginMode = "account";

        if (remember || autoLogin)
        {
            settings.Account = AccountBox.Text.Trim();
            settings.Password = CurrentPassword;
        }
        else
        {
            settings.Account = string.Empty;
            settings.Password = string.Empty;
        }

        settings.Save();
    }

    private void SaveCardSettings()
    {
        LoginSettings settings = LoginSettings.Load();
        bool remember = RememberCardCheck.IsChecked == true;
        bool autoLogin = CardAutoLoginCheck.IsChecked == true;

        settings.RememberCard = remember;
        settings.AutoLoginCard = autoLogin;
        settings.LoginMode = "card";

        if (remember || autoLogin)
        {
            settings.CardKey = CardKeyBox.Text.Trim();
        }
        else
        {
            settings.CardKey = string.Empty;
        }

        settings.Save();
    }

    private static void SetStatus(System.Windows.Controls.TextBlock target, string text, string color)
    {
        target.Text = text;
        target.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        target.Visibility = Visibility.Visible;
    }
}
