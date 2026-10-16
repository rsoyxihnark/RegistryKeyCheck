using System.Windows;
using System.Windows.Input;

namespace RegistryKey_Check
{
    public partial class DetailWindow : Window
    {
        public DetailWindow()
        {
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
            ModelRegCommand mo = ViewModels.ObsColl_RegCommands[ViewModels.DisplayingIDs];
            TextBoxUID.Text = mo.ListId.ToString();
            TextBoxListCommand.Text = mo.ListCommand;
            TextBoxListFullPath.Text = mo.ListFullPath;
            TextBoxAvailableSubKey.Text = mo.AvailableSubKey;
            TextBoxListValueName.Text = ViewModels.Shown(mo.ListValueName);
            TextBoxAvailableValueName.Text = ViewModels.Shown(mo.AvailableValueName);
            TextBoxListValueType.Text = ViewModels.Shown(mo.ListValueType);
            TextBoxRegEditValueType.Text = ViewModels.Shown(mo.RegEditValueType);
            TextBoxListValueData.Text = ViewModels.Shown(mo.ListValueData);
            TextBoxRegEditValueData.Text = ViewModels.Shown(mo.RegEditValueData);
            TextBoxExecuteResult.Text = mo.ExecuteResult;
            TextBoxListMatchWithRegEdit.Text = mo.ListMatchWithRegEdit;
            TextBoxComplianceDetails.Text = mo.ComplianceDetails;
        }
    }
}
