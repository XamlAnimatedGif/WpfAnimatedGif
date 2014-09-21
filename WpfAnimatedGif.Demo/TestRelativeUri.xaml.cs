using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfAnimatedGif.Demo
{
    /// <summary>
    /// Interaction logic for TestRelativeUri.xaml
    /// </summary>
    public partial class TestRelativeUri
    {
        public TestRelativeUri()
        {
            InitializeComponent();
            Img = new BitmapImage(new Uri("/WpfAnimatedGif.Demo;component/Images/earth.gif", UriKind.Relative));
        }


        public ImageSource Img
        {
            get { return (ImageSource)GetValue(ImgProperty); }
            set { SetValue(ImgProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Img.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImgProperty =
            DependencyProperty.Register("Img", typeof(ImageSource), typeof(TestRelativeUri), new PropertyMetadata(null));


    }
}
