using Android.Bluetooth;
using Android.Content;
using Android.Support.V4.Media.Session;
using Android.Util;
using Android.Views;
using Android.Widget;
using Google.Android.Material.Snackbar;
using System;
using TMRADIO.Interfaces;
using TMRADIO.Models;
using Xamarin.Forms;
using static TMRADIO.Constants.Links;

namespace TMRADIO.Droid.MediaSession
{
    public class MySessionCallback : MediaSessionCompat.Callback
    {
        private readonly Android.Content.Context context = Android.App.Application.Context;
        private readonly IPlayerConnector session;
        
        public MySessionCallback()
        {
            DependencyService.Register<IPlayerConnector>();
            session = DependencyService.Get<IPlayerConnector>();
        }

        [System.Obsolete]
        public override bool OnMediaButtonEvent(Intent mediaButtonEvent)
        {
            var mediaSource = session.MediaSource();
            var title = session.GetTitle();
            var artist = session.GetArtist();
            var album = session.GetAlbum();
            var albumArt = session.GetAlbumArt();
            
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
                        if (mediaSource != TMRADIO_URL)
                            session.Pause();
                        else
                            session.Stop();
                        break;
                    case Keycode.MediaStop:
                        if (mediaSource != TMRADIO_URL)
                            session.Pause();
                        else
                            session.Stop();
                        break;
                    case Keycode.MediaRewind:
                        if (mediaSource != TMRADIO_URL)
                            session.Rewind();
                        break;
                    case Keycode.MediaFastForward:
                        if (mediaSource != TMRADIO_URL)
                            session.FastForward();
                        break;
                    default:
                        break;
                }

                NotificationViewModel model = new NotificationViewModel()
                {
                    Title = title,
                    Artist = artist,
                    AlbumArt = albumArt,
                    Album = album,
                    Duration = session.GetMediaDuration(),
                    Position = session.GetCurrentPosition()
                };
                session.ShowMediaNotification(model);
                Toast.MakeText(context, $"Callback:  {keycode.KeyCode} pressed!", ToastLength.Long).Show();
                //Log.Debug($"[{context.PackageName}]", $"Intent: {keycode}");
            }

            return base.OnMediaButtonEvent(mediaButtonEvent);
        }
    }
}