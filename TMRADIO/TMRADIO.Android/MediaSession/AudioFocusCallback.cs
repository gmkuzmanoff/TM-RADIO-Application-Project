using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Runtime;
using System;
using TMRADIO.Interfaces;
using TMRADIO.Models;
using Xamarin.CommunityToolkit.Core;
using Xamarin.Forms;
using static TMRADIO.Constants.Links;
using Context = Android.Content.Context;

namespace TMRADIO.Droid.MediaSession
{
    public class AudioFocusCallback : Java.Lang.Object, AudioManager.IOnAudioFocusChangeListener
    {
        private readonly Context context;
        private AudioFocusRequest audioFocusRequest;
        private readonly IPlayerConnector session;
        private readonly AudioManager audioManager;
        private AudioFocusRequestClass audioFocusRequestClass;

        private string mediaSource = string.Empty;
        private string title = string.Empty;
        private string artist = string.Empty;
        private string album = string.Empty;
        private string albumArt = string.Empty;
        private long mediaDuration;
        private float mediaPosition;

        public AudioFocusCallback()
        {
            context = Android.App.Application.Context;
            DependencyService.Register<IPlayerConnector>();
            session = DependencyService.Get<IPlayerConnector>();
            audioManager = (AudioManager)context.GetSystemService(Context.AudioService);
        }

        [Obsolete]
        public bool RequestAudioFocus()
        {
            //Audio attributes
            var audioAttributes = new AudioAttributes.Builder()
                .SetContentType(AudioContentType.Music)
                .SetLegacyStreamType(Stream.Music)
                .SetUsage(AudioUsageKind.Media)
                .Build();

            audioFocusRequestClass = new AudioFocusRequestClass.Builder(AudioFocus.Gain)
                .SetAudioAttributes(audioAttributes)
                .SetOnAudioFocusChangeListener(this)
                .Build();

            if (Build.VERSION.SdkInt > BuildVersionCodes.O)
            {
                audioFocusRequest = audioManager.RequestAudioFocus(audioFocusRequestClass);
            }
            else
            {
                audioFocusRequest = audioManager.RequestAudioFocus(this, Stream.Music, AudioFocus.Gain);
            }

            if (audioFocusRequest.Equals(AudioFocusRequest.Granted))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void OnAudioFocusChange([GeneratedEnum] AudioFocus focusChange)
        {
            switch (focusChange)
            {
                case AudioFocus.Gain:
                    session.SetVolume(100);
                    session.Play();
                    SetMediaSessionReqs();
                    // Resume or increase volume
                    break;

                case AudioFocus.Loss:
                    session.Pause();
                    SetMediaSessionReqs();
                    // Stop playback completely
                    break;

                case AudioFocus.LossTransient:
                    session.Pause();
                    SetMediaSessionReqs();
                    // Pause playback
                    break;

                case AudioFocus.LossTransientCanDuck:
                    session.SetVolume(20);
                    // Lower volume
                    break;
            }

            //Toast.MakeText(context, $"AudioFocus: {focusChange}", ToastLength.Long).Show();
        }

        private void SetMediaSessionReqs()
        {
            mediaSource = session.MediaSource();
            title = session.GetTitle();
            artist = session.GetArtist();
            album = session.GetAlbum();
            albumArt = session.GetAlbumArt();
            mediaDuration = session.GetMediaDuration();
            mediaPosition = session.GetCurrentPosition();

            //Create notification
            NotificationViewModel notificationViewModel = new NotificationViewModel()
            {
                Title = title,
                Artist = artist,
                AlbumArt = albumArt,
                Album = album,
                Duration = mediaDuration,
                Position = mediaPosition
            };
            session.ShowMediaNotification(notificationViewModel);

            //Create metadata
            MetadataViewModel metadataViewModel = new MetadataViewModel()
            {
                Title = title,
                Artist = mediaSource == TMRADIO_STREAM_URL ? album : artist,
                AlbumArt = mediaSource == TMRADIO_STREAM_URL ? BitmapFactory.DecodeResource(context.Resources, Resource.Drawable.logo) : (albumArt.Equals("tm_radio_episode") ? BitmapFactory.DecodeResource(context.Resources, Resource.Drawable.tm_radio_episode) : BitmapFactory.DecodeFile(albumArt)),
                Album = mediaSource == TMRADIO_STREAM_URL ? artist : album,
                Duration = mediaDuration
            };
            session.SetMetadata(metadataViewModel);

            session.SetPlaybackState();
        }

        public void AbondonAudioFocus()
        {
            if (audioFocusRequest.Equals(AudioFocusRequest.Granted))
            {
                audioManager.AbandonAudioFocusRequest(audioFocusRequestClass);
            }
        }
    }
}