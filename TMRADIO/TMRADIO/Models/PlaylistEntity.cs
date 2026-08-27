using System.IO;
using static TMRADIO.Constants.Links;

namespace TMRADIO.Models
{
    public class PlaylistEntity
    {
        private string title = string.Empty;
        private string show = string.Empty;

        public string Title { get => title; set => title = value; }
        public string Show { get => show; set => show = value; }
        public string ImageArt { get; set; } = string.Empty;
        public string ImageSourceForPlaylist { get => GetImageForPlaylist(); }
        public string ImageSourceForRecentlyPlayedEpisodes {  get => GetImageForRecentlyPlayedEpisodes(); }
        public string ImageSourceForFavouriteEpisodes { get => GetImageForFavouriteEpisodes(); }
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        private string GetImageForPlaylist()
        {
            string file = $"{EXTERNAL_CACHE_DIR}/Temp/{Title}.jpg";

            if (File.Exists(file))
            {
                return file;
            }
            else
            {
                return $"{EXTERNAL_CACHE_DIR}/tm_radio_episode.jpg";
            }
        }

        private string GetImageForRecentlyPlayedEpisodes()
        {
            string file = $"{RECENT_EPISODES_TUMBNAILS_DIR}/{Title}.jpg";

            if (File.Exists(file))
            {
                return file;
            }
            else
            {
                return "tm_radio_episode";
            }
        }

        private string GetImageForFavouriteEpisodes()
        {
            string file = $"{FAVOURITE_EPISODES_TUMBNAILS_DIR}/{Title}.jpg";

            if (File.Exists(file))
            {
                return file;
            }
            else
            {
                return "tm_radio_episode";
            }
        }
    }
}
