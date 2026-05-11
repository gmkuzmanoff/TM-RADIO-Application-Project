namespace TMRADIO.Models
{
    public class NotificationViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string AlbumArt { get; set; } = string.Empty;
        public long Duration { get; set; } = default;
        public float Position { get; set; } = default;
    }
}
