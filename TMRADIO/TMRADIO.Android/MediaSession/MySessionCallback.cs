using Android.Content;
using Android.Graphics;
using Android.Support.V4.Media.Session;
using Android.Views;
using Android.Widget;
using System;
using System.Net.Http;
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
                    AlbumArt = mediaSource == TMRADIO_URL ? BitmapFactory.DecodeResource(context.Resources, Resource.Drawable.logo) : BitmapFactory.DecodeFile(albumArt),
                    Album = album,
                    Duration = session.GetMediaDuration()
                };
                //Set metadata
                Device.StartTimer(TimeSpan.FromSeconds(5), () =>
                {
                    session.SetPlaybackState();
                    session.SetMetadata(metadataViewModel);
                    session.InitializeSession();

                    return false;
                });
                Toast.MakeText(context, $"Callback:  {keycode.KeyCode} pressed!", ToastLength.Long).Show();
                //Log.Debug($"[{context.PackageName}]", $"Intent: {keycode}");
            }

            return base.OnMediaButtonEvent(mediaButtonEvent);
        }
    }
}