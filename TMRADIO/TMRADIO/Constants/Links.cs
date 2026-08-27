namespace TMRADIO.Constants
{
    public static class Links
    {
        public static string EMPTY_EPISODE_IMAGE = "https://www.tm-radio.com/pic/tm-radio-episode.png";
        public static string EMPTY_SHOW_IMAGE = "https://www.tm-radio.com/pic/tm-radio-show.png";
        public static string TMRADIO_LOGO = "https://www.tm-radio.com/pic/logo/MasterLogo_h128w223_png24.png";
        public static string XSPF = "http://www.tm-radio.com:8000/tribalmixes.xspf";
        public static string SHEDULE = "https://www.tm-radio.com/schedule.php";
        public static string SHOWS = "https://www.tm-radio.com/shows.php";
        public static string TMRADIO_STREAM_URL = "http://stream.tm-radio.com:8000/tribalmixes";
        public static string TM_FACEBOOK = "https://www.facebook.com/tmradiodotcom";
        public static string TM_TWITTER = "https://twitter.com/_TMRadio";
        public static string TM_WEBSITE = "https://www.tm-radio.com/";
        public static string GITHUB_REPOSITORY = "https://github.com/gmkuzmanoff/TM-RADIO-Application-Project";
        public static string EXTERNAL_CACHE_DIR = Android.App.Application.Context.ExternalCacheDir.AbsolutePath;
        public static string EXTERNAL_FILES_DIR = Android.App.Application.Context.GetExternalFilesDir("").AbsolutePath;
        public static string RECENT_EPISODES_TUMBNAILS_DIR = $"{EXTERNAL_FILES_DIR}/recentThumbs";
        public static string FAVOURITE_EPISODES_TUMBNAILS_DIR = $"{EXTERNAL_FILES_DIR}/favsThumbs";
        public static string XML_RECENTLYPLAYED_FILE = $"{EXTERNAL_FILES_DIR}/RecentlyPlayed.xml";
        public static string XML_FAVOURITES_FILE = $"{EXTERNAL_FILES_DIR}/Favourites.xml";
    }
}
