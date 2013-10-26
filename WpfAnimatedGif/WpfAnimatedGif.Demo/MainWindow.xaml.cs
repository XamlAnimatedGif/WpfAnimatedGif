using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.VisualBasic;
using Microsoft.Win32;

namespace WpfAnimatedGif.Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnGC_Click(object sender, RoutedEventArgs e)
        {
            GC.Collect();
        }
    }
}
