using Android.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace TMRADIO.Models
{
    public class MetadataViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public Bitmap AlbumArt { get; set; }
        public long Duration { get; set; }
    }
}
