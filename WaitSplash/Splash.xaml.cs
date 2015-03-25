using System;
using System.Globalization;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Threading;

namespace WaitSplash
{
    /// <summary>
    /// Interaction logic for Splash.xaml
    /// </summary>
    [UIPermissionAttribute(SecurityAction.Demand, Window = UIPermissionWindow.AllWindows)]
    public partial class Splash : Window
    {
        #region Properties

        protected object SyncLocker = new object();
        protected int ShowTimes = 0;
        protected long TickCount;
        protected DispatcherTimer TickCounter;

        #endregion

        #region Constructors

        public Splash()
        {
            this.InitializeComponent();

            TickCounter = new DispatcherTimer();
            TickCounter.Tick += TickCounter_Tick;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Centers to parent.
        /// </summary>
        /// <param name="pWidth">Width of the parent form</param>
        /// <param name="pHeight">Height of the parent form</param>
        /// <param name="pLeft">Left of the parent form</param>
        /// <param name="pTop">Top of the parent form</param>
        public void CenterToParent(double pWidth, double pHeight, double pLeft, double pTop)
        {
            double ox = pWidth / 2;
            double oy = pHeight / 2;

            this.Dispatcher.Invoke(() =>
            {
                this.Left = (ox - this.Width / 2) + pLeft;
                this.Top = (oy - this.Height / 2) + pTop;
            });
        }

        void TickCounter_Tick(object sender, System.EventArgs e)
        {
            lblTimeNo.Text = ((Environment.TickCount - TickCount) / 1000).ToString(CultureInfo.InvariantCulture);
        }

        public void Start()
        {
            lock (SyncLocker)
            {
                if (++ShowTimes == 1)
                {
                    TickCount = Environment.TickCount;
                    TickCounter.Start();
                    this.Show();
                }
            }
        }

        public void Stop()
        {
            lock (SyncLocker)
            {
                if (--ShowTimes <= 0)
                {
                    this.Hide();
                    TickCounter.Stop();
                }
            }
        }

        #endregion
    }
}
