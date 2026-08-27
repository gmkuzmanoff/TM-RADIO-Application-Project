using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Media;
using Android.Media.Session;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Media;
using Android.Support.V4.Media.Session;
using Android.Views;
using AndroidX.Core.App;
using AndroidX.Media;
using LibVLCSharp.Shared;
using System.Collections.Generic;
using TMRADIO.Droid;
using TMRADIO.Droid.Interfaces;
using TMRADIO.Droid.MediaSession;
using TMRADIO.Droid.Services;
using TMRADIO.Interfaces;
using TMRADIO.Models;
using Xamarin.Forms;

using static TMRADIO.Constants.Links;

[assembly: Dependency(typeof(PlayerConnector))]
namespace TMRADIO.Droid
{
    [Service(Exported = true, Enabled = true, ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
    [IntentFilter(new[] { "android.media.browse.MediaBrowserService", "android.intent.action.MEDIA_BUTTON", "android.media.AUDIO_BECOMING_NOISY" })]
    public class PlayerConnector : MediaBrowserServiceCompat, IPlayerConnector
    {
        private readonly IAndroidAutoRadioService androidAutoRadioService;

        private readonly PlaybackStateCompat.Builder playbackState;
        private readonly MediaMetadataCompat.Builder mediaMetadata;
        private NotificationManager notificationManager;
        private readonly Vlc vlcPlayer;
        private Android.Content.Intent actionIntent;
        private Notification notification;
        private readonly MediaSessionCompat mediaSession;
        private NotificationChannel channel;
        private readonly Context context;
        private const int NOTIFICATION_ID = 1001;
        private const string CHANNEL_ID = "Media_playback_channel";

        private string mediaSource;
        private string title, artist, album, albumArt;

        public PlayerConnector()
        {
            androidAutoRadioService = new AndroidAutoRadioService();
            playbackState = new PlaybackStateCompat.Builder();
            mediaMetadata = new MediaMetadataCompat.Builder();
            context = Android.App.Application.Context;
            mediaSession = new MediaSessionCompat(context, "TMRADIO Media Session");
            vlcPlayer = new Vlc();
        }

        public void InitializeSession()
        {
            // Initialize MediaSession
            mediaSession.SetFlags((int)MediaSessionFlags.HandlesMediaButtons | (int)MediaSessionFlags.HandlesTransportControls);
            mediaSession.SetCallback(new MySessionCallback());
            
            SetButtonReceiver();
            // Set the session active
            mediaSession.Active = true;
        }

        public void SetButtonReceiver()
        {
            // Android 12+ safe media button receiver
            var mediaButtonIntent = new Intent(Intent.ActionMediaButton);
            mediaButtonIntent.SetClass(context, typeof(MediaButtonReceiver));
            mediaButtonIntent.SetComponent(new ComponentName(context, nameof(MediaButtonReceiver)));

            var pendingIntent = PendingIntent.GetBroadcast(
                context,
                mediaButtonIntent.GetHashCode(),
                mediaButtonIntent,
                PendingIntentFlags.Mutable
            );
            
            mediaSession.SetMediaButtonReceiver(pendingIntent);
        }

        public void SetPlaybackState()
        {
            var currentState = vlcPlayer.PlayerState();
            int sessionState = 0;

            switch (currentState)
            {
                case VLCState.NothingSpecial:
                    sessionState = PlaybackStateCompat.StateNone;
                    break;
                case VLCState.Opening:
                    sessionState = PlaybackStateCompat.StateConnecting;
                    break;
                case VLCState.Buffering:
                    sessionState = PlaybackStateCompat.StateBuffering;
                    break;
                case VLCState.Playing:
                    sessionState = PlaybackStateCompat.StatePlaying;
                    break;
                case VLCState.Paused:
                    sessionState = PlaybackStateCompat.StatePaused;
                    break;
                case VLCState.Stopped:
                    sessionState = PlaybackStateCompat.StateStopped;
                    break;
                case VLCState.Error:
                    sessionState = PlaybackStateCompat.StateError;
                    break;
                default:
                    break;
            }

            playbackState.SetActions(
               PlaybackStateCompat.ActionPlay |
               PlaybackStateCompat.ActionPause |
               PlaybackStateCompat.ActionStop |
               PlaybackStateCompat.ActionFastForward |
               PlaybackStateCompat.ActionRewind |
               PlaybackStateCompat.ActionSkipToNext |
               PlaybackStateCompat.ActionSkipToPrevious |
               PlaybackStateCompat.ActionSeekTo);

            long position = (long)(vlcPlayer.CurrentPosition / 1 * vlcPlayer.MediaDuration());

            playbackState.SetState(sessionState, position, vlcPlayer.PlayerRate());

            mediaSession.SetPlaybackState(playbackState.Build());
        }

        public void ShowMediaNotification(NotificationViewModel notificationViewModel)
        {
            title = notificationViewModel.Title;
            artist = notificationViewModel.Artist;
            album = notificationViewModel.Album;
            albumArt = notificationViewModel.AlbumArt;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channelName = "Media Playback";
                channel = new NotificationChannel(CHANNEL_ID, channelName, NotificationImportance.Low)
                {
                    Description = "Media Playback Controls"
                };

                notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService);
                notificationManager.CreateNotificationChannel(channel);
            }

            //create notification style
            notification = new NotificationCompat.Builder(context, CHANNEL_ID)
                .SetContentTitle(title)
                .SetContentText($"{artist} - {album}")
                .SetContentIntent(PendingOpenApp())
                .SetLargeIcon(mediaSource == "http://stream.tm-radio.com:8000/tribalmixes" ? BitmapFactory.DecodeResource(context.Resources ,Resource.Drawable.logo) : BitmapFactory.DecodeFile(albumArt))
                .SetSmallIcon(Resource.Drawable.logo_transparent)
                .SetVisibility((int)NotificationVisibility.Public)
                .SetPriority((int)NotificationPriority.High)
                .SetProgress(1, (int)notificationViewModel.Position, false)
                .SetShowWhen(false)
                .SetOnlyAlertOnce(true)
                //.AddAction(new NotificationCompat.Action(Resource.Drawable.prev_not, "skip", null))
                .AddAction(new NotificationCompat.Action(Resource.Drawable.rewind_not, "Rewind", PendingRewind()))
                .AddAction(DependencyService.Get<IPlayerConnector>().IsPlaying() || vlcPlayer.PlayerState() == VLCState.Opening ? new NotificationCompat.Action(Resource.Drawable.pause_not, "Pause", PendingPause()) : new NotificationCompat.Action(Resource.Drawable.play_not, "Play", PendingPlay()))
                //.AddAction(Resource.Drawable.stop_not, "Stop", PendingStop())
                .AddAction(new NotificationCompat.Action(Resource.Drawable.fastforward_not, "FastForward", PendingFastForward()))
                //.AddAction(new NotificationCompat.Action(Resource.Drawable.next_not, "next", null))
                .SetStyle(new AndroidX.Media.App.NotificationCompat.MediaStyle().SetMediaSession(mediaSession.SessionToken).SetShowActionsInCompactView(1))
                .SetOngoing(vlcPlayer.IsPlayingMedia() || vlcPlayer.PlayerState() == VLCState.Opening)
                .Build();

            //Notify
            var notificatioManagerCompat = NotificationManagerCompat.From(context);
            notificatioManagerCompat.Notify(NOTIFICATION_ID, notification);
        }

        public void SetMetadata(MetadataViewModel metadataViewModel)
        {
            mediaMetadata.PutString(MediaMetadata.MetadataKeyTitle, metadataViewModel.Title);
            mediaMetadata.PutString(MediaMetadata.MetadataKeyArtist, metadataViewModel.Artist);
            mediaMetadata.PutString(MediaMetadata.MetadataKeyAlbum, metadataViewModel.Album);
            mediaMetadata.PutBitmap(MediaMetadata.MetadataKeyAlbumArt, metadataViewModel.AlbumArt);
            mediaMetadata.PutLong(MediaMetadata.MetadataKeyDuration, metadataViewModel.Duration); // Duration in ms

            mediaSession.SetMetadata(mediaMetadata.Build());
        }

        #region "Pending Intents for notification"
        private PendingIntent PendingOpenApp()
        {
            Intent intent = new Intent(context, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
            return PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Mutable);
        }

        private PendingIntent PendingPlay()
        {
            actionIntent = new Intent(context, typeof(MediaButtonReceiver));
            actionIntent.SetAction(Intent.ActionMediaButton);
            actionIntent.PutExtra("MediaSource", mediaSource);
            actionIntent.PutStringArrayListExtra("NotificationData", new string[] { title, artist, album, albumArt });
            actionIntent.PutExtra("PlayerState", vlcPlayer.PlayerState().ToString());
            actionIntent.PutExtra(Intent.ExtraKeyEvent, new KeyEvent(KeyEventActions.Down, Keycode.MediaPlay));
            return PendingIntent.GetBroadcast(context, (int)Keycode.MediaPlay, actionIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Mutable);
        }

        private PendingIntent PendingPause()
        {
            actionIntent = new Intent(context, typeof(MediaButtonReceiver));
            actionIntent.SetAction(Intent.ActionMediaButton);
            actionIntent.PutExtra("MediaSource", mediaSource);
            actionIntent.PutStringArrayListExtra("NotificationData", new string[] { title, artist, album, albumArt });
            actionIntent.PutExtra(Intent.ExtraKeyEvent, new KeyEvent(KeyEventActions.Down, Keycode.MediaPause));
            return PendingIntent.GetBroadcast(context, (int)Keycode.MediaPause, actionIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Mutable);
        }

        private PendingIntent PendingRewind()
        {
            actionIntent = new Intent(context, typeof(MediaButtonReceiver));
            actionIntent.SetAction(Intent.ActionMediaButton);
            actionIntent.PutExtra("MediaSource", mediaSource);
            actionIntent.PutStringArrayListExtra("NotificationData", new string[] { title, artist, album, albumArt });
            actionIntent.PutExtra(Intent.ExtraKeyEvent, new KeyEvent(KeyEventActions.Down, Keycode.MediaRewind));
            return PendingIntent.GetBroadcast(context, (int)Keycode.MediaRewind, actionIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Mutable);
        }

        private PendingIntent PendingFastForward()
        {
            actionIntent = new Intent(context, typeof(MediaButtonReceiver));
            actionIntent.SetAction(Intent.ActionMediaButton);
            actionIntent.PutExtra("MediaSource", mediaSource);
            actionIntent.PutStringArrayListExtra("NotificationData", new string[] { title, artist, album, albumArt });
            actionIntent.PutExtra(Intent.ExtraKeyEvent, new KeyEvent(KeyEventActions.Down, Keycode.MediaFastForward));
            return PendingIntent.GetBroadcast(context, (int)Keycode.MediaFastForward, actionIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Mutable);
        }
        #endregion

        public bool IsActive()
        {
            return mediaSession.Active;
        }

        public void LoadMedia(string uri)
        {
            vlcPlayer.LoadMedia(uri);
            mediaSource = uri;
        }

        public long GetCurrentTime()
        {
            return vlcPlayer.CurrentTime;
        }

        public long SetCurrentTime(double sliderValue)
        {
            vlcPlayer.CurrentTime = (long)(sliderValue / 1 * vlcPlayer.MediaDuration());
            return vlcPlayer.CurrentTime;
        }

        public long GetMediaDuration()
        {
            return vlcPlayer.MediaDuration();
        }

        public float GetCurrentPosition()
        {
            return vlcPlayer.CurrentPosition;
        }

        public void MediaParse()
        {
            vlcPlayer.MediaParse();
        }

        public bool IsPlaying()
        {
            return vlcPlayer.IsPlayingMedia();
        }

        public void FastForward()
        {
            vlcPlayer.Skip30sec();
        }

        public void FastForwardPressed()
        {
            vlcPlayer.FastForwardPressed();
        }

        public void FastForwardReleased()
        {
            vlcPlayer.FastForwardReleased();
        }

        public void Rewind()
        {
            vlcPlayer.Back30sec();
        }

        public void RewindPressed()
        {
            vlcPlayer.RewindPressed();
        }

        public void RewindReleased()
        {
            vlcPlayer.RewindReleased();
        }

        public void Pause()
        {
            vlcPlayer.PauseMedia();
        }

        public void Play()
        {
            vlcPlayer.PlayMedia();
        }

        public void Stop()
        {
            vlcPlayer.StopMedia();
        }

        public void StopSession()
        {
            try
            {
                if (mediaSession != null)
                {
                    notificationManager.Cancel(NOTIFICATION_ID);
                    mediaSession.Active = false;
                    mediaSession.Dispose();
                    vlcPlayer.PlayerDispose();
                }
            }
            catch
            {

            }
            
        }
        
        public string MediaSource()
        {
            return mediaSource;
        }

        public VLCState GetCurrentState()
        {
            return vlcPlayer.PlayerState();
        }

        public string GetTitle()
        {
            return title;
        }

        public string GetArtist()
        {
            return artist;
        }

        public string GetAlbum()
        {
            return album;
        }

        public string GetAlbumArt()
        {
            return albumArt;
        }

        public override BrowserRoot OnGetRoot(string clientPackageName, int clientUid, Bundle rootHints)
        {
            return new BrowserRoot("SHOWS", null);
        }

        public override void OnLoadChildren(string parentId, Result result)
        {
            MediaDescriptionCompat mediaDescription;
            JavaList<MediaBrowserCompat.MediaItem> mediaItems;
            MediaBrowserCompat.MediaItem mediaItem;

            if (parentId.Equals("SHOWS"))
            {
                mediaItems = new JavaList<MediaBrowserCompat.MediaItem>();

                foreach (var show in androidAutoRadioService.GetRadioShows())
                {
                    mediaDescription = new MediaDescriptionCompat.Builder()
                        .SetMediaId(show.Id)
                        .SetTitle(show.Title)
                        .SetSubtitle("")
                        //.SetIconUri(Android.Net.Uri.Parse(show.ImageUrl))
                        .SetDescription(show.Description)
                        .Build();

                    mediaItem = new MediaBrowserCompat.MediaItem(mediaDescription, (int)Android.Media.Browse.MediaItemFlags.Browsable);
                    mediaItems.Add(mediaItem);
                }

                result.SendResult(mediaItems);
            }
            else
            {
                mediaDescription = new MediaDescriptionCompat.Builder()
                        .SetMediaId("")
                        .SetTitle("TM-RADIO Live Stream")
                        .SetSubtitle("www.tm-radio.com")
                        //.SetIconUri(Android.Net.Uri.Parse(TMRADIO_LOGO))
                        .SetDescription("")
                        .Build();

                mediaItem = new MediaBrowserCompat.MediaItem(mediaDescription, (int)Android.Media.Browse.MediaItemFlags.Playable);

                result.SendResult(mediaItem);
            }
            

        }
    }
}