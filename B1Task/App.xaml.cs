using System.CodeDom;
using System.Configuration;
using System.Data;
using System.Windows;

namespace B1Task
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {

                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                base.OnStartup(e);
        }
    }

}
