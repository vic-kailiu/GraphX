using GraphX.Models;
using ShowcaseExample.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace ShowcaseExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Properties
        /// <summary>
        /// Random generator
        /// </summary>
        private Random Rand = new Random();
        #endregion

        private const int datasourceSize = 5000;

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
            ThemedGraph_Constructor();
            Closed += MainWindow_Closed;

            Loaded += MainWindow_Loaded;
        }


        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            if(PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}