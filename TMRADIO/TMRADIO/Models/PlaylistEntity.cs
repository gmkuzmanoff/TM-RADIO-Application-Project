using System;
using System.IO;
using static TMRADIO.Constants.Links;

namespace TMRADIO.Models
{
    public class PlaylistEntity
    {
        private string title = string.Empty;
        private string show = string.Empty;

        public string ShowId { get; set; }
        public string Title { get => title; set => title = value; }
        public string Show { get => show; set => show = value; }
        public string ImageArt { get; set; } = string.Empty;
        public string ImageSource { get => GetImageSource(); }
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        private string GetImageSource()
        {
            string file = $"{THUMBNAILS_DIR}/{ShowId}/{Title}.jpg";

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
