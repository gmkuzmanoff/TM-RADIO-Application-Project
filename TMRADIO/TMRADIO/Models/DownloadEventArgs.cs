using System;
using System.Collections.Generic;
using System.Text;

namespace TMRADIO.Models
{
    public class DownloadEventArgs : EventArgs
    {
        private bool fileSaved = false;

        public DownloadEventArgs(bool fileSaved)
        {
            this.fileSaved = fileSaved;
        }
    }
}
