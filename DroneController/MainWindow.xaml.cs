using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Documents.DocumentStructures;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GPIO;

namespace DroneController
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CameraFeed _cameraFeed;
        
        public MainWindow()
        {
            InitializeComponent();
            _cameraFeed = new CameraFeed();
            UpdateCameraSignalStatus();
        }

        private void UpdateCameraSignalStatus()
        {
            if (_cameraFeed.feedOnline)
            {
                statusLabel.Content = "Telemetry Offline";
            }
            else
            {
                // Handle case where feed is offline
                statusLabel.Content = "Telemetry, Video Offline";
            }
        }

        private void btnClick(object sender, RoutedEventArgs e)
        {
            //put shit here and dont forget it this time
            
        }
    }
}