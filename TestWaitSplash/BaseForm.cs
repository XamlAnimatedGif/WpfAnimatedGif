using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using WaitSplash;

namespace TestWaitSplash
{
    public partial class BaseForm : Form
    {
        public static Splash WaitSplash = new Splash();
        public Action OnStartupAction;

        public BaseForm()
        {
            InitializeComponent();


            Application.Idle += Application_Idle;
        }

        void Application_Idle(object sender, EventArgs e)
        {
            Application.Idle -= Application_Idle;

            ParallelInvoke(OnStartupAction);
        }

        public async void ParallelInvoke(Action doSomething)
        {
            WaitSplash.Start();

            if (doSomething != null) await Task.Run(doSomething);

            WaitSplash.Stop();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            WaitSplash.CenterToParent(this.Width, this.Height, this.Left, this.Top);
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);

            WaitSplash.CenterToParent(this.Width, this.Height, this.Left, this.Top);
        }


        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            WaitSplash.Focus();
        }
    }
}
