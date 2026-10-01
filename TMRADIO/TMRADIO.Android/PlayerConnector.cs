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
using System.Collections.ObjectModel;
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
    [Service(
        Exported = true,
        ForegroundServiceType = ForegroundService.TypeMediaPlayback,
        Label = "TMRADIO_MediaBrowserService",
        Icon = "@mipmap/icon",
        RoundIcon = "@mipmap/ic_launcher_round")]
    [IntentFilter(new[] { "android.media.browse.MediaBrowserService" })]
    public class PlayerConnector : MediaBrowserServiceCompat, IPlayerConnector
    {
        private readonly IAndroidAutoRadioService androidAutoRadioService;
        
        private readonly PlaybackStateCompat.Builder playbackState;
        private readonly MediaMetadataCompat.Builder mediaMetadata;
        private NotificationManager notificationManager;
        private readonly Vlc vlcPlayer;
        private Android.Content.Intent actionIntent;
        private Notification notification;
        private MediaSessionCompat mediaSession;
        private NotificationChannel channel;
        private readonly Context context;
        private const int NOTIFICATION_ID = 1001;
        private const string CHANNEL_ID = "Media_playback_channel";

        private string mediaSource;
        private string title, artist, album, albumArt;

        //Android Auto Requirements
        private MediaDescriptionCompat mediaDescription;
        private JavaList<MediaBrowserCompat.MediaItem> mediaItems;
        MediaBrowserCompat.MediaItem mediaItem;

        public PlayerConnector()
        {
            androidAutoRadioService = new AndroidAutoRadioService();
            playbackState = new PlaybackStateCompat.Builder();
            mediaMetadata = new MediaMetadataCompat.Builder();
            context = Android.App.Application.Context;
            vlcPlayer = new Vlc();
        }

        public void InitializeSession()
        {
            mediaSession = new MediaSessionCompat(context, "TMRADIO_MediaSession");
            mediaSession.SetCallback(new MySessionCallback());
            mediaSession.SetFlags((int)MediaSessionFlags.HandlesMediaButtons | (int)MediaSessionFlags.HandlesTransportControls);
            mediaSession.SetMediaButtonReceiver(ButtonReceiverIntent());
            mediaSession.Active = true;
        }

        public void SetPlaylist(ObservableCollection<PlaylistEntity> playlist, string playlistTitle)
        {
            IList<MediaSessionCompat.QueueItem> list = new ObservableCollection<MediaSessionCompat.QueueItem>();
            mediaSession.SetQueue(null);
            
            foreach (var item in playlist)
            {
                var mediaDescription = new MediaDescriptionCompat.Builder()
                    .SetMediaId(playlist.IndexOf(item).ToString())
                    .SetTitle(item.Title)
                    .SetSubtitle(title)
                    .SetDescription(item.Description)
                    .SetIconUri(Android.Net.Uri.Parse(item.ImageArt))
                    .SetMediaUri(Android.Net.Uri.Parse(item.Url))
                    .Build();

                list.Add(new MediaSessionCompat.QueueItem(mediaDescription, playlist.IndexOf(item)));
                
            }

            mediaSession.SetQueueTitle(title);
            mediaSession.SetQueue(list);
        }

        public IList<MediaSessionCompat.QueueItem> SessionPlaylist { get => mediaSession.Controller.Queue; }

        public PendingIntent ButtonReceiverIntent()
        {
            // Android 12+ safe media button receiver
            var mediaButtonIntent = new Intent(Intent.ActionMediaButton);
            mediaButtonIntent.SetClass(context, typeof(MediaButtonReceiver));
            mediaButtonIntent.SetComponent(new ComponentName(context, nameof(MediaButtonReceiver)));

            return PendingIntent.GetBroadcast(
                context,
                mediaButtonIntent.GetHashCode(),
                mediaButtonIntent,
                PendingIntentFlags.Mutable
            );
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
                .SetLargeIcon(mediaSource == TMRADIO_STREAM_URL ? BitmapFactory.DecodeResource(context.Resources ,Resource.Drawable.logo) : (albumArt.Equals("tm_radio_episode") ? BitmapFactory.DecodeResource(context.Resources, Resource.Drawable.tm_radio_episode) : BitmapFactory.DecodeFile(albumArt)) )
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
                    mediaSession.Release();
                    vlcPlayer.PlayerDispose();
                    context.StopService(new Intent(this, typeof(PlayerConnector)));
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


        // Media Framework Content Style constants
        private static readonly string CONTENT_STYLE_SUPPORTED =
            "android.media.browse.CONTENT_STYLE_SUPPORTED";
    public static string CONTENT_STYLE_PLAYABLE_HINT =
            "android.media.browse.CONTENT_STYLE_PLAYABLE_HINT";
    public static string CONTENT_STYLE_BROWSABLE_HINT =
            "android.media.browse.CONTENT_STYLE_BROWSABLE_HINT";
    public static int CONTENT_STYLE_LIST_ITEM_HINT_VALUE = 1;
        public static int CONTENT_STYLE_GRID_ITEM_HINT_VALUE = 2;

        private Bundle GetDefaultStyle()
        {
            Bundle style = new Bundle();
            style.PutBoolean(CONTENT_STYLE_SUPPORTED, true);
            style.PutInt(CONTENT_STYLE_BROWSABLE_HINT, CONTENT_STYLE_GRID_ITEM_HINT_VALUE);
            style.PutInt(CONTENT_STYLE_PLAYABLE_HINT, CONTENT_STYLE_LIST_ITEM_HINT_VALUE);
            return style;
        }

        public override BrowserRoot OnGetRoot(string clientPackageName, int clientUid, Bundle rootHints)
        {
            return new BrowserRoot("root", null);
        }

        public override void OnLoadChildren(string parentId, Result result)
        {
            if (parentId == "root")
            {
                result.SendResult("live");
            }
            else
            {
                MediaBrowserCompat.MediaItem[] array = {
                new MediaBrowserCompat.MediaItem(
                new MediaDescriptionCompat.Builder()
                .SetMediaId("live")
                .SetTitle("Live Stream")
                .SetDescription("Best of Underground :: Techno, Tech House, Progressive & Deep House")
                .Build(),
                (int)Android.Media.Browse.MediaItemFlags.Browsable)};

                mediaItems.Add(new MediaBrowserCompat.MediaItem(
                    new MediaDescriptionCompat.Builder()
                    .SetMediaId("on-demand")
                    .SetTitle("On Demand")
                    .SetDescription("All Shows on TM Radio, Show Archives, On-Demand Streaming")
                    .Build(),
                    (int)Android.Media.Browse.MediaItemFlags.Browsable));


                result.SendResult(array);
            }
            
            
            //else
            //{
            //    mediaDescription = new MediaDescriptionCompat.Builder()
            //            .SetMediaId("")
            //            .SetTitle("TM-RADIO Live Stream")
            //            .SetSubtitle("www.tm-radio.com")
            //            .SetIconUri(Android.Net.Uri.Parse(TMRADIO_LOGO))
            //            .SetDescription("")
            //            .Build();

            //    mediaItem = new MediaBrowserCompat.MediaItem(mediaDescription, (int)Android.Media.Browse.MediaItemFlags.Playable);

            //    result.SendResult(mediaItem);
            //    result.Detach();
            //}
        }

        public override void OnLoadItem(string itemId, Result result)
        {
            
        }
    }
}