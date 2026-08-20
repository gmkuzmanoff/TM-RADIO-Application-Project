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
        public string ImageSource { get => GetImage(); }
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string GetImage()
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
    }
}
