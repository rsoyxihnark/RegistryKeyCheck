using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace RegistryKey_Check
{
    public partial class DetailWindow : Window
    {
        private readonly ModelRegCommand mo;

        public DetailWindow(ModelRegCommand mo)
        {
            this.mo = mo;
            App.UseDarkTitleBar(this);
            InitializeComponent();
        }

        private void DetailWin_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void DetailWin_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxUID.Text = mo.ListId.ToString();
            TextBoxListCommand.Text = mo.ListCommand;
            TextBoxListFullPath.Text = mo.ListFullPath;
            TextBoxAvailableSubKey.Text = mo.AvailableSubKey;
            TextBoxListValueName.Text = mo.ListValueNameShown;
            TextBoxAvailableValueName.Text = mo.AvailableValueNameShown;
            TextBoxListValueType.Text = mo.ListValueTypeShown;
            TextBoxRegEditValueType.Text = mo.RegEditValueTypeShown;
            TextBoxListValueData.Text = mo.ListValueDataShown;
            TextBoxRegEditValueData.Text = mo.RegEditValueDataShown;
            TextBoxExecuteResult.Text = mo.ExecuteResult;
            TextBoxListMatchWithRegEdit.Text = mo.ListMatchWithRegEdit;
            TextBoxComplianceDetails.Text = mo.ComplianceDetails;
            string? status = mo.HasListWarning ? "OrangeBrush" : mo.ListMatchWithRegEdit == "True" ? "GreenBrush" : mo.ListMatchWithRegEdit == "False" ? "RedBrush" : null;
            if (status != null)
            {
                TextBoxListMatchWithRegEdit.Foreground = (Brush)FindResource(status);
                TextBoxComplianceDetails.Foreground = (Brush)FindResource(status);
            }
        }
    }
}
