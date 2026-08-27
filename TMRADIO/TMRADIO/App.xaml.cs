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

            Directory.CreateDirectory($"{EXTERNAL_CACHE_DIR}/Temp");
            Directory.CreateDirectory(RECENT_EPISODES_TUMBNAILS_DIR);
            Directory.CreateDirectory(FAVOURITE_EPISODES_TUMBNAILS_DIR);

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
            string directoryTemp = $"{EXTERNAL_CACHE_DIR}/Temp";
            DirectoryInfo directory = new DirectoryInfo(directoryTemp);

            foreach (var file in directory.GetFiles())
            {
                file.Delete();
            }
        }
    }
}
