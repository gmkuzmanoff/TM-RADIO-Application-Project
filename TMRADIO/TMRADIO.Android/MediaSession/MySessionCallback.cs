using Android.Content;
using Android.Graphics;
using Android.Support.V4.Media.Session;
using Android.Views;
using LibVLCSharp.Shared;
using System;
using TMRADIO.Interfaces;
using TMRADIO.Models;
using Xamarin.Forms;
using static TMRADIO.Constants.Links;

namespace TMRADIO.Droid.MediaSession
{
    public class MySessionCallback : MediaSessionCompat.Callback
    {
        private readonly Context context = Android.App.Application.Context;
        private readonly IPlayerConnector session;

        private string mediaSource = string.Empty;
        private string title = string.Empty;
        private string artist = string.Empty;
        private string album = string.Empty;
        private string albumArt = string.Empty;
        private long mediaDuration;
        private float mediaPosition;

        [Obsolete]
        public MySessionCallback()
        {
            DependencyService.Register<IPlayerConnector>();
            session = DependencyService.Get<IPlayerConnector>();
        }

        [Obsolete]
        public override bool OnMediaButtonEvent(Intent mediaButtonEvent)
        {
            mediaSource = session.MediaSource();
            
            var keycode = (KeyEvent)mediaButtonEvent.Extras.GetParcelable(Intent.ExtraKeyEvent);

            if (keycode != null && keycode.Action == KeyEventActions.Down)
            {
                switch (keycode.KeyCode)
                {
                    case Keycode.MediaPlayPause:
                        if (session.IsPlaying() || session.GetCurrentState().Equals(VLCState.Opening))
                        {
                            session.Pause();
                        }
                        else
                            session.Play();
                        
                        break;
                    case Keycode.MediaPlay:
                            session.Play();
                        
                        break;
                    case Keycode.MediaPause:
                        if (TMRADIO_STREAM_URL.Equals(mediaSource))
                        {
                            session.Stop();
                        }
                        else
                            session.Pause();
                        break;
                    case Keycode.MediaStop:
                        if (TMRADIO_STREAM_URL.Equals(mediaSource))
                        {
                            session.Stop();
                        }
                        else
                            session.Pause();
                        break;
                    case Keycode.MediaRewind:
                        if (!TMRADIO_STREAM_URL.Equals(mediaSource))
                            session.RewindPressed();
                        break;
                    case Keycode.MediaFastForward:
                        if (!TMRADIO_STREAM_URL.Equals(mediaSource))
                            session.FastForwardPressed();
                        break;
                    default:
                        break;
                }

                //Toast.MakeText(context, $"Callback: {keycode.KeyCode} pressed! | AF: {isAudioFocusGained}", ToastLength.Long).Show();
            }

            if (keycode != null && keycode.Action == KeyEventActions.Up)
            {
                switch (keycode.KeyCode)
                {
                    case Keycode.MediaFastForward:
                        session.FastForwardReleased();
                        break;
                    case Keycode.MediaRewind:
                        session.RewindReleased();
                        break;
                    default:
                        break;
                }
                //Toast.MakeText(context, $"Callback: {keycode.KeyCode} released!", ToastLength.Short).Show();
            }

            SetMediaSessionReqs();

            return base.OnMediaButtonEvent(mediaButtonEvent);
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

            //Set metadata
            session.SetMetadata(metadataViewModel);

            session.SetPlaybackState();
            
        }

        
        public override void OnPlay()
        {
            session.Play();
            
            //Toast.MakeText(context, $"OnPlay()", ToastLength.Short).Show();
            SetMediaSessionReqs();
        }

        public override void OnPause()
        {
            session.Pause();

            //Toast.MakeText(context, $"OnPause()", ToastLength.Short).Show();
            SetMediaSessionReqs();
        }

        public override void OnSeekTo(long pos)
        {
            session.OnSeekTo(pos);
            
            //Toast.MakeText(context, $"OnSeekTo()", ToastLength.Short).Show();
            SetMediaSessionReqs();
        }
    }
}