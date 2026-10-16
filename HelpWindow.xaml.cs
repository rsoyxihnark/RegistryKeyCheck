using System.Windows;

namespace RegistryKey_Check
{
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            App.UseDarkTitleBar(this);
            InitializeComponent();
        }

        private void HelpWin_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxHelpString.Text = ViewModels.HelpString;
        }
    }
}
