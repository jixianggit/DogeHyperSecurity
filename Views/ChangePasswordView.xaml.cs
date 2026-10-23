using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DogeHyperSecurity.Interop;

namespace DogeHyperSecurity.Views;

public partial class ChangePasswordView : UserControl
{
    public event EventHandler? Cancelled;

    public ChangePasswordView()
    {
        InitializeComponent();
    }

    private async void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        string user = CpAccountBox.Text.Trim();
        string superPassword = CpSuperPasswordBox.Text;
        string newPassword = CpNewPasswordBox.Text;

        if (user.Length == 0 || superPassword.Length == 0 || newPassword.Length == 0)
        {
            SetStatus("请填写完整信息", "#C42B1C");
            return;
        }

        ChangePasswordButton.IsEnabled = false;
        SetStatus("正在修改密码…", "#6B6B6B");

        try
        {
            QpResult result = await Task.Run(() => QpAuth.ChangePassword(user, superPassword, newPassword));
            SetStatus(result.Message, result.Success ? "#107C10" : "#C42B1C");
        }
        finally
        {
            ChangePasswordButton.IsEnabled = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private void SetStatus(string text, string color)
    {
        CpStatus.Text = text;
        CpStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        CpStatus.Visibility = Visibility.Visible;
    }
}
