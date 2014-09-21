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
    /// Interaction logic for AnimatedImageViewer.xaml
    /// </summary>
    public partial class AnimatedImageViewer : INotifyPropertyChanged
    {
        public AnimatedImageViewer()
        {
            InitializeComponent();
            _images = new ObservableCollection<string>
                      {
                          "pack://application:,,,/images/working.gif",
                          "pack://application:,,,/images/earth.gif",
                          "pack://application:,,,/images/radar.gif",
                          "pack://application:,,,/images/bomb.gif",
                          "pack://application:,,,/images/bomb-once.gif",
                          "pack://application:,,,/images/nonanimated.png",
                          "pack://application:,,,/images/monster.gif",
                          "pack://siteoforigin:,,,/images/siteoforigin.gif",
                          "pack://application:,,,/images/partialfirstframe.gif",
                          "http://i.imgur.com/rCK6xzh.gif"
                      };
            DataContext = this;
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "GIF images|*.gif";
            if (dlg.ShowDialog() == true)
            {
                Images.Add(dlg.FileName);
                SelectedImage = dlg.FileName;
            }
        }

        private void AnimationCompleted(object sender, RoutedEventArgs e)
        {
            Completed = true;
            if (_controller != null)
                SetPlayPauseEnabled(_controller.IsPaused || _controller.IsComplete);
        }

        private ObservableCollection<string> _images;
        public ObservableCollection<string> Images
        {
            get { return _images; }
            set
            {
                _images = value;
                OnPropertyChanged("Images");
            }
        }

        private string _selectedImage;
        public string SelectedImage
        {
            get { return _selectedImage; }
            set
            {
                _selectedImage = value;
                OnPropertyChanged("SelectedImage");
                Completed = false;
                Dispatcher.BeginInvoke(ImageChanged, DispatcherPriority.Background);
            }
        }

        private void ImageChanged()
        {
            if (_controller != null)
                _controller.CurrentFrameChanged -= ControllerCurrentFrameChanged;

            _controller = ImageBehavior.GetAnimationController(img);

            if (_controller != null)
            {
                _controller.CurrentFrameChanged += ControllerCurrentFrameChanged;
                sldPosition.Value = 0;
                sldPosition.Maximum = _controller.FrameCount - 1;
                SetPlayPauseEnabled(_controller.IsPaused || _controller.IsComplete);
            }
        }

        private void ControllerCurrentFrameChanged(object sender, EventArgs e)
        {
            if (_controller != null)
            {
                sldPosition.Value = _controller.CurrentFrame;
                Debug.WriteLine("ControllerCurrentFrameChanged: {0}", sldPosition.Value);
            }
        }

        private bool _useDefaultRepeatBehavior = true;
        public bool UseDefaultRepeatBehavior
        {
            get { return _useDefaultRepeatBehavior; }
            set
            {
                _useDefaultRepeatBehavior = value;
                OnPropertyChanged("UseDefaultRepeatBehavior");
                if (value)
                    RepeatBehavior = default(RepeatBehavior);
            }
        }


        private bool _repeatForever;
        public bool RepeatForever
        {
            get { return _repeatForever; }
            set
            {
                _repeatForever = value;
                OnPropertyChanged("RepeatForever");
                if (value)
                    RepeatBehavior = RepeatBehavior.Forever;
            }
        }


        private bool _useSpecificRepeatCount;
        public bool UseSpecificRepeatCount
        {
            get { return _useSpecificRepeatCount; }
            set
            {
                _useSpecificRepeatCount = value;
                OnPropertyChanged("UseSpecificRepeatCount");
                if (value)
                    RepeatBehavior = new RepeatBehavior(RepeatCount);
            }
        }

        private int _repeatCount = 3;
        public int RepeatCount
        {
            get { return _repeatCount; }
            set
            {
                _repeatCount = value;
                OnPropertyChanged("RepeatCount");
                if (UseSpecificRepeatCount)
                    RepeatBehavior = new RepeatBehavior(value);
            }
        }

        private bool _completed;
        public bool Completed
        {
            get { return _completed; }
            set
            {
                _completed = value;
                OnPropertyChanged("Completed");
            }
        }

        private RepeatBehavior _repeatBehavior;
        public RepeatBehavior RepeatBehavior
        {
            get { return _repeatBehavior; }
            set
            {
                _repeatBehavior = value;
                OnPropertyChanged("RepeatBehavior");
                Completed = false;
                Dispatcher.BeginInvoke(ImageChanged, DispatcherPriority.Background);
            }
        }

        private bool _autoStart = true;
        public bool AutoStart
        {
            get { return _autoStart; }
            set
            {
                _autoStart = value;
                OnPropertyChanged("AutoStart");
            }
        }

        private bool _synchronized;
        public bool Synchronized
        {
            get { return _synchronized; }
            set
            {
                _synchronized = value;
                OnPropertyChanged("Synchronized");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private ImageAnimationController _controller;

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            if (_controller != null)
                _controller.Pause();
            SetPlayPauseEnabled(true);
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (_controller != null)
                _controller.Play();
            Completed = false;
            SetPlayPauseEnabled(false);
        }

        private void sldPosition_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_controller != null)
            {
                var currentFrame = _controller.CurrentFrame;
                if (currentFrame >= 0 && currentFrame != (int)sldPosition.Value)
                    _controller.GotoFrame((int)sldPosition.Value);
            }
        }

        private void SetPlayPauseEnabled(bool isPaused)
        {
            btnPause.IsEnabled = !isPaused;
            btnPlay.IsEnabled = isPaused;
        }

        private void btnOpenUrl_Click(object sender, RoutedEventArgs e)
        {
            string url = Interaction.InputBox("Enter the URL of the image to load", "Enter URL");
            if (!string.IsNullOrEmpty(url))
            {
                Images.Add(url);
                SelectedImage = url;
            }
        }

        private void AnimationLoaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Animation loaded: {0}", ImageBehavior.GetIsAnimationLoaded(img));
        }
    }
}
