using TMRADIO.Models;

namespace TMRADIO.Interfaces
{
    public interface IMediaService
    {
        void Stop(Vlc player);
        void Play(Vlc player);
    }
}
