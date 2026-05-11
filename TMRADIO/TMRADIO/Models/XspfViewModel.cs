using Android.Graphics;
using static TMRADIO.Constants.Links;

namespace TMRADIO.Models
{
    public class XspfViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Info { get; set; } = string.Empty;
        public string StreamTitle { get; set; } = string.Empty;
        public string StreamDescription { get; set; } = string.Empty;
        public string StreamContentType { get; set; } = string.Empty;
        public string StreamBitrate { get; set; } = "unknown";
        public string StreamCurrentListeners { get; set; } = string.Empty;
        public string StreamPeakListeners { get; set; } = string.Empty;
        public string StreamGenre { get; set; } = string.Empty;
        public string Logo { get; set; } = "logo";
    }
}
