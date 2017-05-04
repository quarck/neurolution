using System;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using Microsoft.Win32;

namespace Neurolution
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly World world;
        private WorldView _worldView;

        public MainWindow()
        {
            InitializeComponent();

            string documents = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string workingFolder = $"{documents}\\Neurolution\\{DateTime.Now:yyyy-MM-dd-HH-mm}";

            world = new World(
                workingFolder,
                AppProperties.WorldSize, 
                AppProperties.FoodCountPerIteration,
                AppProperties.PredatorCountPerIteration,
                AppProperties.WorldWidth, 
                AppProperties.WorldHeight);
        }

        private void HideAllControls()
        {
            ButtonStartNew.Visibility = Visibility.Collapsed;
            ButtonLoadSavedWorld.Visibility = Visibility.Collapsed;
            ButtonLoadSavedTop.Visibility = Visibility.Collapsed;
            MultiThreaded.Visibility = Visibility.Collapsed;
        }

        private void buttonStartNew_Click(object sender, RoutedEventArgs e)
        {
            HideAllControls();
            _worldView = new WorldView(grid, world);
            world.MultiThreaded = MultiThreaded.IsChecked ?? false;
            new Thread(this.CalcThread).Start();
        }

        private void buttonLoadSavedTop_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "XML Files (.xml)|*.xml|All Files (*.*)|*.*";
            dialog.FilterIndex = 1;

            dialog.Multiselect = false;

            bool? userClickedOK = dialog.ShowDialog();

            if (userClickedOK == true)
            {
                // Open the selected file to read.
                string filename = dialog.FileName;

                world.InitializeFromTopFile(filename);

                _worldView = new WorldView(grid, world);
                HideAllControls();

                world.MultiThreaded = MultiThreaded.IsChecked ?? false;

                new Thread(this.CalcThread).Start();
            }
        }

        private void buttonLoadSavedWorld_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "XML Files (.xml)|*.xml|All Files (*.*)|*.*";
            dialog.FilterIndex = 1;

            dialog.Multiselect = false;

            bool? userClickedOK = dialog.ShowDialog();

            if (userClickedOK == true)
            {
                // Open the selected file to read.
                string filename = dialog.FileName;

                world.InitializeFromWorldFile(filename);

                _worldView = new WorldView(grid, world);
                HideAllControls();

                world.MultiThreaded = MultiThreaded.IsChecked ?? false;

                new Thread(this.CalcThread).Start();
            }
        }

        private void CalcThread()
        {
            //var start = DateTime.Now;
            //var end = start + TimeSpan.FromSeconds(20);

            for (long step = 0; ; ++step)
            {
                lock (world)
                {
                    world.Iterate(step);
                }

                if (step % 4 == 0)
                {
                    try
                    {
                        var s = step;
                        Dispatcher.Invoke(() => UpdateUI(s));
                    }
                    catch (TaskCanceledException)
                    {
                        world.Save();
                        break;
                    }
                }

                //if (DateTime.Now > end)
                //    break;
            }
        }

        private void UpdateUI(long step)
        {
            lock (world)
            {
                _worldView.UpdateFrom(world);
                GenerationLabel.Content = $"{step:D8}";
            }
        }
    }
}
