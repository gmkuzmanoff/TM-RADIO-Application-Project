using Android.Webkit;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using TMRADIO.Droid.Services;
using TMRADIO.Interfaces;
using Xamarin.Forms;
using static AndroidX.Concurrent.Futures.CallbackToFutureAdapter;
using static TMRADIO.Constants.Links;

[assembly: Dependency(typeof(ApplicationUpdater))]
namespace TMRADIO.Droid.Services
{
    public class ApplicationUpdater : IApplicationUpdater
    {
        public event EventHandler<TMRADIO.Interfaces.DownloadEventArgs> OnFileDownloaded;

        public void DownloadFile(string url, string folder)
        {
            try
            {
                WebClient client = new WebClient();
                client.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                string updateFile = Path.Combine(EXTERNAL_CACHE_DIR, "update.apk");
                client.DownloadFileAsync(new Uri(url), updateFile);
            }
            catch (Exception)
            {
                OnFileDownloaded?.Invoke(this, new TMRADIO.Interfaces.DownloadEventArgs(false));
            }
        }

        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                OnFileDownloaded?.Invoke(this, new TMRADIO.Interfaces.DownloadEventArgs(false));
            }
            else
            {
                OnFileDownloaded?.Invoke(this, new TMRADIO.Interfaces.DownloadEventArgs(true));
            }
        }
    }
}