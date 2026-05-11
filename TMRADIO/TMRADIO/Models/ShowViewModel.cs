using System.IO;
using static TMRADIO.Constants.Links;

namespace TMRADIO.Models
{
    public class ShowViewModel
    {
        private string title = string.Empty;

        public string Id { get; set; } = string.Empty;
        public string Title { get => title; set => title = value; }
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string ImageSource { get => GetImage(); }
        public GroupType GroupType { get; set; }

        public string GetImage()
        {
            string file = $"{ExternalCacheDir}/{Title}.jpg";

            if (File.Exists(file))
            {
                return file;
            }
            else
            {
                return "compact_disc";
            }
        }
    }
}
