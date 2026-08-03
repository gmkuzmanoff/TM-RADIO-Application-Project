using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using LibVLCSharp.Shared;
using System;
using System.Net.Http;
using TMRADIO.Interfaces;
using TMRADIO.Models;
using Xamarin.Forms;
using static TMRADIO.Constants.Links;
using static TMRADIO.Constants.Digits;

namespace TMRADIO.Droid
{
    [BroadcastReceiver(Exported = true, Enabled = true)]
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
            var title = string.Empty;
            var artist = string.Empty;
            var album = string.Empty;
            var albumArt = string.Empty;

            string state = intent.Extras.GetString("PlayerState");
            string mediaSource = intent.Extras.GetString("MediaSource");
            
            //Notification data
            var notifData = intent.Extras.GetStringArrayList("NotificationData");
            if (notifData != null)
            {
                title = notifData[0];
                artist = notifData[1];
                album = notifData[2];
                albumArt = notifData[3];
            }


            //KeyCodes
            var keycode = (KeyEvent)intent.Extras.GetParcelable(Intent.ExtraKeyEvent);

            if (keycode != null)
            {
                switch (keycode.KeyCode)
                {
                    case Keycode.MediaPlayPause:

                        if (session.IsPlaying() || state == VLCState.Opening.ToString())
                        { session.Pause(); }
                        else { session.Play(); }
                            
                        break;
                    case Keycode.MediaPlay:

                        session.Play();

                        break;
                    case Keycode.MediaPause:
                        if (mediaSource == TMRADIO_STREAM_URL)
                        { session.Stop(); }
                        else { session.Pause(); }

                        break;
                    case Keycode.MediaStop:

                        session.Stop();
                        
                        break;
                    case Keycode.MediaRewind:

                        if (mediaSource != TMRADIO_STREAM_URL)
                            session.Rewind();
                        
                        break;
                    case Keycode.MediaFastForward:

                        if (mediaSource != TMRADIO_STREAM_URL)
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
                session.InitializeSession();

                return false;
            });

            Toast.MakeText(context, $"Receiver: {keycode.KeyCode} pressed!", ToastLength.Long).Show();
        }

    }
}
