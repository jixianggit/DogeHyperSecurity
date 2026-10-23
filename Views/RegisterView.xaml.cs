using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DogeHyperSecurity.Interop;

namespace DogeHyperSecurity.Views;

public partial class RegisterView : UserControl
{
    public event EventHandler? Cancelled;

    public RegisterView()
    {
        InitializeComponent();
    }

    private async void Register_Click(object sender, RoutedEventArgs e)
    {
        string user = RegAccountBox.Text.Trim();
        string password = RegPasswordBox.Text;
        string superPassword = RegSuperPasswordBox.Text;
        string cardKey = RegCardKeyBox.Text.Trim();

        if (user.Length == 0 || password.Length == 0 || superPassword.Length == 0 || cardKey.Length == 0)
        {
            SetStatus("请填写完整信息", "#C42B1C");
            return;
        }

        RegisterButton.IsEnabled = false;
        SetStatus("正在注册…", "#6B6B6B");

        try
        {
            QpResult result = await Task.Run(() => QpAuth.Register(user, password, superPassword, cardKey));
            SetStatus(result.Message, result.Success ? "#107C10" : "#C42B1C");
        }
        finally
        {
            RegisterButton.IsEnabled = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private void SetStatus(string text, string color)
    {
        RegStatus.Text = text;
        RegStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        RegStatus.Visibility = Visibility.Visible;
    }
}
