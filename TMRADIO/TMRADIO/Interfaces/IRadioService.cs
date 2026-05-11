using System.Collections.ObjectModel;
using TMRADIO.Models;

namespace TMRADIO.Interfaces
{
    public interface IRadioService
    {
        XspfViewModel LoadXspf();
        string SheduleOnString();
        ObservableCollection<Shedule> GetSheduleMonthly();
        ObservableCollection<ShowViewModel> GetMainShows();
        ObservableCollection<ShowViewModel> GetOldShows();
        ObservableCollection<PlaylistEntity> GetPlaylistEntities(string id);
        byte[] ResizeImageAndroid(byte[] imageData);
        void CleanTempDir();
        ObservableCollection<PlaylistEntity> RecentlyPlayed();
    }
}
