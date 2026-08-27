using Android.Runtime;
using TMRADIO.Models;

namespace TMRADIO.Droid.Interfaces
{
    public interface IAndroidAutoRadioService
    {
        JavaList<ShowViewModel> GetRadioShows();
        JavaList<PlaylistEntity> GetEpisodesFromSelectedShow(string id);
    }
}