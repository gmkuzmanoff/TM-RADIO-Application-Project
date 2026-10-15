using LibVLCSharp.Shared;
using System.Collections.Generic;
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
        float GetCurrentPosition();
        string MediaSource();
        string GetTitle();
        string GetArtist();
        string GetAlbum();
        string GetAlbumArt();
        VLCState GetCurrentState();
        long GetMediaDuration();
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
