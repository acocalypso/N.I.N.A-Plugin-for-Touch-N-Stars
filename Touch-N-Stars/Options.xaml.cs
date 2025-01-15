using System.ComponentModel.Composition;
using System.Windows;
using System.Diagnostics;
using System.Windows.Navigation;

namespace TouchNStars {
    [Export(typeof(ResourceDictionary))]
    partial class Options : ResourceDictionary
    {
        public Options() 
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            _ = Process.Start(new ProcessStartInfo(e.Uri.OriginalString) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}