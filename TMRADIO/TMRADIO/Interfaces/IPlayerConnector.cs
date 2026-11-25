using System.Collections.ObjectModel;
using TMRADIO.Models;

namespace TMRADIO.Interfaces
{
    public interface IPlayerConnector
    {
        void InitializeSession();
        void SetPlaylist(ObservableCollection<PlaylistEntity> playlist, string playlistTitle);
        void LoadMedia(string uri);
        long GetCurrentTime();
        long SetCurrentTime(double sliderValue);
        void SetVolume(int percents);
        int GetVolume();
        float GetCurrentPosition();
        string MediaSource();
        string GetTitle();
        string GetArtist();
        string GetAlbum();
        string GetAlbumArt();
        int GetCurrentState();
        long GetMediaDuration();
        void OnSeekTo(long pos);
        void MediaParse();
        bool IsPlaying();
        void Play();
        void Pause();
        void Stop();
        void Rewind();
        void RewindPressed();
        void RewindReleased();
        void FastForward();
        void FastForwardPressed();
        void FastForwardReleased();
        void StopSession();
        bool IsActive();
        void SetPlaybackState();
        void SetMetadata(MetadataViewModel metadataViewModel);
        void ShowMediaNotification(NotificationViewModel notificationViewModel);
    }
}
