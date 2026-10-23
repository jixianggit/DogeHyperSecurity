using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DogeHyperSecurity.Services;
using Wpf.Ui.Controls;
using Button = System.Windows.Controls.Button;

namespace DogeHyperSecurity;

public partial class MainWindow : FluentWindow
{
    private TranslateTransform? _dialogDragTransform;
    private Point _dialogDragStart;

    public MainWindow()
    {
        InitializeComponent();

        LoginSettings settings = LoginSettings.Load();
        bool accountMode = string.Equals(settings.LoginMode, "account", StringComparison.OrdinalIgnoreCase);

        AccountLoginSwitch.IsChecked = accountMode;
        CardLoginSwitch.IsChecked = !accountMode;

        AccountLoginSwitch.Checked += LoginModeSwitch_Changed;
        AccountLoginSwitch.Unchecked += LoginModeSwitch_Changed;
        CardLoginSwitch.Checked += LoginModeSwitch_Changed;
        CardLoginSwitch.Unchecked += LoginModeSwitch_Changed;

        ApplyLoginModeAvailability();
    }

    private void LoginModeSwitch_Changed(object sender, RoutedEventArgs e)
    {
        // 控件状态更新完成后再校正，避免同一事件里被控件内部状态覆盖
        Dispatcher.BeginInvoke(new Action(() => EnforceLoginMode(sender)));
    }

    private void EnforceLoginMode(object sender)
    {
        bool isAccount = ReferenceEquals(sender, AccountLoginSwitch);
        bool accountOn = AccountLoginSwitch.IsChecked == true;
        bool cardOn = CardLoginSwitch.IsChecked == true;

        if (accountOn && cardOn)
        {
            if (isAccount)
            {
                CardLoginSwitch.IsChecked = false;
            }
            else
            {
                AccountLoginSwitch.IsChecked = false;
            }
        }
        else if (!accountOn && !cardOn)
        {
            if (isAccount)
            {
                AccountLoginSwitch.IsChecked = true;
            }
            else
            {
                CardLoginSwitch.IsChecked = true;
            }
        }

        ApplyLoginModeAvailability();
    }

    private void ApplyLoginModeAvailability()
    {
        bool accountEnabled = AccountLoginSwitch.IsChecked == true;
        bool cardEnabled = CardLoginSwitch.IsChecked == true;

        if (!accountEnabled && !cardEnabled)
        {
            // 保底：任何情况下至少开启一种
            CardLoginSwitch.IsChecked = true;
            cardEnabled = true;
        }

        WelcomePageView.SetLoginModeAvailability(accountEnabled, cardEnabled);

        // 卡密模式只保留「欢迎」，账号模式显示全部导航
        Visibility contentNavVisibility = accountEnabled ? Visibility.Visible : Visibility.Collapsed;
        NavRegister.Visibility = contentNavVisibility;
        NavRecharge.Visibility = contentNavVisibility;
        NavChangePassword.Visibility = contentNavVisibility;

        if (cardEnabled && WelcomePageView.Visibility != Visibility.Visible)
        {
            Navigate(NavWelcome, WelcomePageView);
        }
    }

    private void Dialog_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source || FindAncestor<Button>(source) is not null)
        {
            return;
        }

        if (sender is not FrameworkElement dialog)
        {
            return;
        }

        _dialogDragTransform = dialog.RenderTransform as TranslateTransform;
        if (_dialogDragTransform is null)
        {
            _dialogDragTransform = new TranslateTransform();
            dialog.RenderTransform = _dialogDragTransform;
        }

        _dialogDragStart = e.GetPosition(this);
        dialog.CaptureMouse();
        e.Handled = true;
    }

    private void Dialog_MouseMove(object sender, MouseEventArgs e)
    {
        if (_dialogDragTransform is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        Point current = e.GetPosition(this);
        _dialogDragTransform.X += current.X - _dialogDragStart.X;
        _dialogDragTransform.Y += current.Y - _dialogDragStart.Y;
        _dialogDragStart = current;
    }

    private void Dialog_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement dialog)
        {
            dialog.ReleaseMouseCapture();
        }

        _dialogDragTransform = null;
    }

    private static T? FindAncestor<T>(DependencyObject child) where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T match)
            {
                return match;
            }

            child = child is Visual ? VisualTreeHelper.GetParent(child) : LogicalTreeHelper.GetParent(child);
        }

        return null;
    }

    private void NavWelcome_Click(object sender, RoutedEventArgs e)
    {
        Navigate(NavWelcome, WelcomePageView);
    }

    private void NavRegister_Click(object sender, RoutedEventArgs e)
    {
        Navigate(NavRegister, RegisterPageView);
    }

    private void NavRecharge_Click(object sender, RoutedEventArgs e)
    {
        Navigate(NavRecharge, RechargePageView);
    }

    private void NavChangePassword_Click(object sender, RoutedEventArgs e)
    {
        Navigate(NavChangePassword, ChangePasswordPageView);
    }

    private void Form_Cancelled(object? sender, EventArgs e)
    {
        Navigate(NavWelcome, WelcomePageView);
    }

    private void Navigate(Button nav, FrameworkElement page)
    {
        NavWelcome.Tag = null;
        NavRegister.Tag = null;
        NavRecharge.Tag = null;
        NavChangePassword.Tag = null;
        nav.Tag = "Selected";

        HideAllDialogs();

        WelcomePageView.Visibility = ReferenceEquals(page, WelcomePageView) ? Visibility.Visible : Visibility.Collapsed;
        RegisterPageView.Visibility = ReferenceEquals(page, RegisterPageView) ? Visibility.Visible : Visibility.Collapsed;
        RechargePageView.Visibility = ReferenceEquals(page, RechargePageView) ? Visibility.Visible : Visibility.Collapsed;
        ChangePasswordPageView.Visibility = ReferenceEquals(page, ChangePasswordPageView) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void WarningClose_Click(object sender, RoutedEventArgs e)
    {
        HideDialog(WarningDialog);
    }

    private async void WarningConfirm_Click(object sender, RoutedEventArgs e)
    {
        HideDialog(WarningDialog);
        await Task.Delay(150);
        DialogLayer.Visibility = Visibility.Visible;
        NoticeDialog.Visibility = Visibility.Visible;
    }

    private void WarningCancel_Click(object sender, RoutedEventArgs e)
    {
        HideDialog(WarningDialog);
    }

    private void NoticeClose_Click(object sender, RoutedEventArgs e)
    {
        HideDialog(NoticeDialog);
    }

    private void HideDialog(UIElement dialog)
    {
        dialog.Visibility = Visibility.Collapsed;

        if (WarningDialog.Visibility != Visibility.Visible && NoticeDialog.Visibility != Visibility.Visible)
        {
            DialogLayer.Visibility = Visibility.Collapsed;
        }
    }

    private void HideAllDialogs()
    {
        WarningDialog.Visibility = Visibility.Collapsed;
        NoticeDialog.Visibility = Visibility.Collapsed;
        DialogLayer.Visibility = Visibility.Collapsed;
    }
}
