using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Views;
using LibVLCSharp.Shared;
using System;
using TMRADIO.Interfaces;
using TMRADIO.Models;
using Xamarin.Forms;
using static TMRADIO.Constants.Digits;
using static TMRADIO.Constants.Links;

namespace TMRADIO.Droid
{
    [BroadcastReceiver(Exported = true)]
    [IntentFilter(new[] { Intent.ActionMediaButton })]
    public class MediaButtonReceiver : BroadcastReceiver
    {
        private readonly IPlayerConnector session;

        public MediaButtonReceiver()
        {
            DependencyService.Register<IPlayerConnector>();
            session = DependencyService.Get<IPlayerConnector>();
        }

        [System.Obsolete]
        public override void OnReceive(Context context, Intent intent)
        {
            var mediaSource = session.MediaSource();
            var title = session.GetTitle();
            var artist = session.GetArtist();
            var album = session.GetAlbum();
            var albumArt = session.GetAlbumArt();

            //KeyCodes
            var keycode = (KeyEvent)intent.Extras.GetParcelable(Intent.ExtraKeyEvent);

            if (keycode != null)
            {
                switch (keycode.KeyCode)
                {
                    case Keycode.MediaPlayPause:
                        if (session.IsPlaying() || session.GetCurrentState().Equals(VLCState.Opening))
                            session.Pause();
                        else
                            session.Play();
                        break;
                    case Keycode.MediaPlay:
                        session.Play();
                        break;
                    case Keycode.MediaPause:
                        if (TMRADIO_STREAM_URL.Equals(mediaSource))
                            session.Stop();
                        else
                            session.Pause();
                        break;
                    case Keycode.MediaStop:
                        if (TMRADIO_STREAM_URL.Equals(mediaSource))
                            session.Stop();
                        else
                            session.Pause();
                        break;
                    case Keycode.MediaRewind:
                        if (!TMRADIO_STREAM_URL.Equals(mediaSource))
                            session.Rewind();
                        break;
                    case Keycode.MediaFastForward:
                        if (!TMRADIO_STREAM_URL.Equals(mediaSource))
                            session.FastForward();
                        break;
                    default:
                        break;
                }
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

                return false;
            });

            //Toast.MakeText(context, $"Receiver: {keycode.KeyCode} pressed!", ToastLength.Long).Show();
        }

    }
}
