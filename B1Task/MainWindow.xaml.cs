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
        private readonly string _connectionString;
        public MainWindow()
        {
            InitializeComponent();
            
            var connectionString = ConfigurationHelper.GetConfiguration()["ConnectionString"];
            _connectionString = connectionString ?? throw new NullReferenceException("Connection string is not set");
        }
        
        private void ExcelReaderButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new ExcelReaderWindow(_connectionString)
            {
                Owner = this
            };
            window.ShowDialog();
        }

        private void GenerateTableButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new TableGeneratorWindow(_connectionString)
            {
                Owner = this
            };
            window.ShowDialog();
        }
    }
}
