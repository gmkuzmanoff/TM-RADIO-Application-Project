using Android.Content;
using Android.Graphics;
using Android.Media.Session;
using Android.OS;
using Android.Support.V4.Media.Session;
using Android.Views;
using Android.Widget;
using System;
using TMRADIO.Interfaces;
using TMRADIO.Models;
using Xamarin.Forms;
using static TMRADIO.Constants.Digits;
using static TMRADIO.Constants.Links;

namespace TMRADIO.Droid.MediaSession
{
    public class MySessionCallback : MediaSessionCompat.Callback
    {
        private readonly Android.Content.Context context = Android.App.Application.Context;
        private readonly IPlayerConnector session;

        private string mediaSource = string.Empty;
        private string title = string.Empty;
        private string artist = string.Empty;
        private string album = string.Empty;
        private string albumArt = string.Empty;

        public MySessionCallback()
        {
            DependencyService.Register<IPlayerConnector>();
            session = DependencyService.Get<IPlayerConnector>();
        }

        [System.Obsolete]
        public override bool OnMediaButtonEvent(Intent mediaButtonEvent)
        {
            mediaSource = session.MediaSource();
            title = session.GetTitle();
            artist = session.GetArtist();
            album = session.GetAlbum();
            albumArt = session.GetAlbumArt();
            
            var keycode = (KeyEvent)mediaButtonEvent.Extras.GetParcelable(Intent.ExtraKeyEvent);

            if (keycode != null && keycode.Action == KeyEventActions.Down)
            {
                switch (keycode.KeyCode)
                {
                    case Keycode.MediaPlayPause:
                        if (session.IsPlaying())
                            session.Pause();
                        else
                            session.Play();
                        break;
                    case Keycode.MediaPlay:
                        session.Play();
                        break;
                    case Keycode.MediaPause:
                        if (mediaSource != TMRADIO_STREAM_URL)
                            session.Pause();
                        else
                            session.Stop();
                        break;
                    case Keycode.MediaStop:
                        if (mediaSource != TMRADIO_STREAM_URL)
                            session.Pause();
                        else
                            session.Stop();
                        break;
                    case Keycode.MediaRewind:
                        if (mediaSource != TMRADIO_STREAM_URL)
                            session.RewindPressed();
                        break;
                    case Keycode.MediaFastForward:
                        if (mediaSource != TMRADIO_STREAM_URL)
                            session.FastForwardPressed();
                        break;
                    default:
                        break;
                }
                //Toast.MakeText(context, $"Callback: {keycode.KeyCode} pressed!", ToastLength.Long).Show();

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

            //Create notification
            NotificationViewModel notificationViewModel = new NotificationViewModel()
            {
                Title = title,
                Artist = artist,
                AlbumArt = albumArt,
                Album = album,
                Duration = session.GetMediaDuration(),
                Position = session.GetCurrentPosition()
            };
            session.ShowMediaNotification(notificationViewModel);

            //Create metadata
            //byte[] image = new HttpClient().GetByteArrayAsync(albumArt).Result;
            MetadataViewModel metadataViewModel = new MetadataViewModel()
            {
                Title = title,
                Artist = artist,
                AlbumArt = mediaSource == TMRADIO_STREAM_URL ? BitmapFactory.DecodeResource(context.Resources, Resource.Drawable.logo) : BitmapFactory.DecodeFile(albumArt),
                Album = album,
                Duration = session.GetMediaDuration()
            };
            //Set metadata
            Device.StartTimer(TimeSpan.FromSeconds(WAIT_FOR_METADATA), () =>
            {
                session.SetPlaybackState();
                session.SetMetadata(metadataViewModel);
                session.InitializeSession();

                return false;
            });

            //Log.Debug($"[{context.PackageName}]", $"Intent: {keycode}");
        

            return base.OnMediaButtonEvent(mediaButtonEvent);
        }

        public override void OnPlayFromMediaId(string mediaId, Bundle extras)
        {
            base.OnPlayFromMediaId(mediaId, extras);
        }

        public override void OnPlayFromSearch(string query, Bundle extras)
        {
            base.OnPlayFromSearch(query, extras);
        }

        public override void OnPlay()
        {
            base.OnPlay();
        }

        public override void OnPause()
        {
            base.OnPause();
        }

        public override void OnStop()
        {
            base.OnStop();
        }

        
    }
}