namespace TMRADIO.Constants
{
    public static class Links
    {
        public static string TMRADIO_LOGO = "https://www.tm-radio.com/pic/logo/MasterLogo_h128w223_png24.png";
        public static string XSPF = "http://www.tm-radio.com:8000/tribalmixes.xspf";
        public static string SHEDULE = "https://www.tm-radio.com/schedule.php";
        public static string SHOWS = "https://www.tm-radio.com/shows.php";
        public static string TMRADIO_URL = "http://stream.tm-radio.com:8000/tribalmixes";
        public static string ExternalCacheDir = Android.App.Application.Context.ExternalCacheDir.AbsolutePath;
        public static string XmlRecentlyPlayedFile = $"{ExternalCacheDir}/RecentlyPlayed.xml";
    }
}
