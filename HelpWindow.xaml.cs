using System.Windows;

namespace RegistryKey_Check
{
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
            WindowTheme.UseDarkTitleBar(this);
        }

        private void HelpWin_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxHelpString.Text = ViewModels.HelpString;
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
