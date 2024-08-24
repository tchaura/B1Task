using System.Globalization;
using System.Windows;

namespace B1Task
{
    public partial class ProgressDialog : Window
    {
        public ProgressDialog()
        {
            InitializeComponent();
            Height = 100;
            ProgressTextGrid.Visibility = Visibility.Hidden;
        }
        
        public ProgressDialog(int maxValue)
        {
            InitializeComponent();
            progressBar.Maximum = maxValue;
            PlaceholderText.Visibility = Visibility.Hidden;
            
        }

        public void UpdateProgress(int value)
        {
            progressBar.Value += value;
            if (progressBar.Value == 100)
            {
                Close();
            }
        }

        public void UpdateProgressWithText(int value)
        {
            progressBar.Value += value;
            CompletedText.Text = progressBar.Value.ToString(CultureInfo.InvariantCulture);
            LeftText.Text = (progressBar.Maximum - progressBar.Value).ToString(CultureInfo.InvariantCulture);
            if (progressBar.Value == progressBar.Maximum) 
            {
                Close();
            }
        }
    }
}
