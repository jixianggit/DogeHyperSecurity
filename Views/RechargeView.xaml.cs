using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DogeHyperSecurity.Interop;

namespace DogeHyperSecurity.Views;

public partial class RechargeView : UserControl
{
    public event EventHandler? Cancelled;

    public RechargeView()
    {
        InitializeComponent();
    }

    private async void Recharge_Click(object sender, RoutedEventArgs e)
    {
        string user = RcAccountBox.Text.Trim();
        string cardKey = RcCardKeyBox.Text.Trim();

        if (user.Length == 0 || cardKey.Length == 0)
        {
            SetStatus("请填写完整信息", "#C42B1C");
            return;
        }

        RechargeButton.IsEnabled = false;
        SetStatus("正在充值…", "#6B6B6B");

        try
        {
            QpResult result = await Task.Run(() => QpAuth.Recharge(user, cardKey));
            SetStatus(result.Message, result.Success ? "#107C10" : "#C42B1C");
        }
        finally
        {
            RechargeButton.IsEnabled = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private void SetStatus(string text, string color)
    {
        RcStatus.Text = text;
        RcStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        RcStatus.Visibility = Visibility.Visible;
    }
}
