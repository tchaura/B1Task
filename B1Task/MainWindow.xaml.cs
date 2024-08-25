using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using B1Task.TableGenerator;

namespace B1Task
{
    public partial class MainWindow : Window
    {
        private const string ConnectionString =
            "Server=127.0.0.1;Port=5432;Database=postgres;UserId=postgres;Password=admin; Include Error Detail=true;";
        public MainWindow()
        {
            InitializeComponent();
        }
        
        private void ExcelReaderButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new ExcelReaderWindow(ConnectionString);
            window.Owner = this;
            window.ShowDialog();
        }

        private void GenerateTableButton_Click(object sender, RoutedEventArgs e)
        {
            TableGeneratorWindow window = new TableGeneratorWindow();
            window.Owner = this;
            window.ShowDialog();
        }
    }
}
