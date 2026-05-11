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

            Directory.CreateDirectory($"{ExternalCacheDir}/Temp");

            CleanTempDir();
        }


        protected override void OnSleep()
        {
            
        }

        protected override void OnResume()
        {
        }

        private void CleanTempDir()
        {
            string directoryTemp = $"{ExternalCacheDir}/Temp";
            DirectoryInfo directory = new DirectoryInfo(directoryTemp);

            foreach (var file in directory.GetFiles())
            {
                file.Delete();
            }
        }
    }
}
