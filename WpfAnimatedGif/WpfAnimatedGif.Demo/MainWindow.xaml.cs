using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace WpfAnimatedGif.Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            _images = new ObservableCollection<string>
                      {
                          "pack://application:,,,/images/working.gif",
                          "pack://application:,,,/images/earth.gif",
                          "pack://application:,,,/images/radar.gif",
                          "pack://application:,,,/images/bomb.gif"
                      };
            DataContext = this;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "GIF images|*.gif";
            if (dlg.ShowDialog() == true)
            {
                //var converter = new ImageSourceConverter();
                //var imgSource = (ImageSource) converter.ConvertFrom(dlg.FileName);
                //ImageBehavior.SetAnimatedSource(img, imgSource);
                Images.Add(dlg.FileName);
                SelectedImage = dlg.FileName;
            }
        }

        private void AnimationCompleted(object sender, RoutedEventArgs e)
        {
            Completed = true;
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
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
