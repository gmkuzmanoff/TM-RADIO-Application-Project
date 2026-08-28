using System.IO;
using Xamarin.Forms;
using static TMRADIO.Constants.Links;

namespace TMRADIO
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
            base.OnStart();

            Directory.CreateDirectory(THUMBNAILS_DIR);
        }


        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
