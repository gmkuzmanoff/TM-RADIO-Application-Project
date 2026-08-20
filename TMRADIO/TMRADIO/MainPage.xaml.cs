using Android;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Java.Net;
using Sharpcaster;
using Sharpcaster.Models;
using Sharpcaster.Models.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;
using TMRADIO.Interfaces;
using TMRADIO.Models;
using TMRADIO.Services;
using Xamarin.Essentials;
using Xamarin.Forms;
using static TMRADIO.Constants.Digits;
using static TMRADIO.Constants.Links;
using static TMRADIO.Constants.Text;

namespace TMRADIO
{
    public partial class MainPage : ContentPage
    {
        private readonly Context context;
        private readonly ILaunchActivity launchActivity;
        private readonly IPlayerConnector session;
        private readonly Timer timer;
        private TimeSpan time;
        private readonly IRadioService radioService;
        private readonly XspfViewModel radioViewModel;
        private readonly NotificationViewModel notificationViewModel;
        private readonly MetadataViewModel metadataViewModel;
        private ShowViewModel show;
        private string nowPlayingTarget;
        private string selectedShow;
        private bool isRadioSelected;
        private bool isShowsCalled;
        private bool isSheduleCalled;
        private bool isStateEnded;
        private bool isStateError;
        private bool isStateOpening;
        private const int maxCountOfRecentlyPlayedEpisodes = 15;
        private readonly ObservableCollection<Shedule> sheduleViewModels;
        private readonly ObservableCollection<ShowViewModel> mainShowViewModels;
        private readonly ObservableCollection<ShowViewModel> oldShowViewModels;
        private readonly ObservableCollection<PlaylistEntity> playlistEpisodes;
        private readonly ObservableCollection<PlaylistEntity> recentlyPlayedEpisodes;
        private readonly ObservableCollection<PlaylistEntity> favouriteEpisodes;
        private readonly ObservableCollection<GroupedCollection<string, ShowViewModel>> allShowsViewModels;
        
        //Chromecast requirements
        private readonly ObservableCollection<ChromecastReceiver> listCastDevices = new ObservableCollection<ChromecastReceiver>();
        private IEnumerable<ChromecastReceiver> chromecasts = new ObservableCollection<ChromecastReceiver>();
        private ChromecastClient client;
        private Media chromecastMedia;

        public MainPage()
        {
            DependencyService.Register<ILaunchActivity>();
            DependencyService.Register<IPlayerConnector>();
            launchActivity = DependencyService.Get<ILaunchActivity>();
            session = DependencyService.Get<IPlayerConnector>();

            context = Android.App.Application.Context;
            time = new TimeSpan(0, 0, 0);
            isRadioSelected = false;
            isShowsCalled = false;
            isSheduleCalled = false;
            selectedShow = string.Empty;
            radioService = new RadioService();
            radioViewModel = new XspfViewModel();
            notificationViewModel = new NotificationViewModel();
            metadataViewModel = new MetadataViewModel();
            sheduleViewModels = new ObservableCollection<Shedule>();
            mainShowViewModels = new ObservableCollection<ShowViewModel>();
            oldShowViewModels = new ObservableCollection<ShowViewModel>();
            playlistEpisodes = new ObservableCollection<PlaylistEntity>();
            recentlyPlayedEpisodes = new ObservableCollection<PlaylistEntity>();
            favouriteEpisodes = new ObservableCollection<PlaylistEntity>();
            allShowsViewModels = new ObservableCollection<GroupedCollection<string, ShowViewModel>>();

            InitializeComponent();

            //Command: Pull to refresh the list of chromecast devices 
            lv_castDevices.RefreshCommand = new Command(async () =>
            {
                chromecasts = await DiscoverChromecastDevicesAsync(chromecasts);
                lv_castDevices.IsRefreshing = false;
            });

            #region 'Load Favourite episodes'
            //Load and Show recently played episodes
            if (!System.IO.File.Exists(XML_FAVOURITES_FILE))
            {
                XDocument xdoc = new XDocument();
                XElement root = new XElement("Episodes");
                xdoc.Add(root);
                xdoc.Save(XML_FAVOURITES_FILE);
            }
            GetFavourites();
            lv_favourites.ItemsSource = favouriteEpisodes;
            #endregion

            #region 'Load and Show recently played episodes'
            //Load and Show recently played episodes
            if (!System.IO.File.Exists(XML_RECENTLYPLAYED_FILE))
            {
                XDocument xdoc = new XDocument();
                XElement root = new XElement("Episodes");
                xdoc.Add(root);
                //XElement newEpisode = new XElement("Episode");
                //newEpisode.Add(
                //    new XElement("Title", "Citric Waves 113 Ugly Cat"),
                //    new XElement("Image", "https://www.tm-radio.com/pic/djs/UglyCatMusic/WhatsApp_Image_2025-09-05_at_11.46.58_AM.jpeg"),
                //    new XElement("Show", "Citric Waves"),
                //    new XElement("Url", "https://www.tm-radio.com/access_mp3.php?mp3=qarh97"),
                //    new XElement("Description", "Citric Waves 113 Ugly Cat (from January 29th)"));
                //xdoc.Element("Episodes").AddFirst(newEpisode);
                xdoc.Save(XML_RECENTLYPLAYED_FILE);
            }
            GetRecentlyPlayed();
            cview_recently.ItemsSource = recentlyPlayedEpisodes;
            #endregion

            try
            {
                GetRadioMetadataLoop(radioViewModel);

                lbl_aboutText.Text = ABOUT_TMRADIO;

                //Get Main Shows and insert models into list as source
                GetMainShows();
                lv_mainShows.ItemsSource = mainShowViewModels;

                //Main Shows Scroll Animation
                AutoScroll(mainShowViewModels);
            }
            catch
            {
                Task.Run(async () =>
                {
                    bool isOK = await DisplayAlert("TMRADIO", "This application REQUIRE network connection!", "", "exit", FlowDirection.LeftToRight);
                    if (!isOK)
                    {
                        radioService.CleanTempDir();
                        session.StopSession();
                        Process.KillProcess(Process.MyPid());
                    }
                });
            }

            //Player Timer
            timer = new Timer
            {
                Interval = 1
            };

            timer.Elapsed += Timer_Tick;

            timer.Start();
        }


        #region "Timer and Progress"
        private void Timer_Tick(object sender, ElapsedEventArgs e)
        {
            isStateOpening = session.GetCurrentState() == LibVLCSharp.Shared.VLCState.Opening;
            isStateEnded = session.GetCurrentState() == LibVLCSharp.Shared.VLCState.Ended;
            isStateError = session.GetCurrentState() == LibVLCSharp.Shared.VLCState.Error;

            time = TimeSpan.FromMilliseconds(session.GetCurrentTime());

            Device.BeginInvokeOnMainThread(() =>
            {
                lbl_current_time.Text = time.ToString(@"h\:mm\:ss");

                #region Event listener 'playing' GLOBAL (for radio and sessions)
                if (session.IsPlaying() || isStateOpening)
                {
                    btn_playTmRadio.IsVisible = false;
                }
                else
                {
                    btn_playTmRadio.IsVisible = true;
                }
                #endregion

                if (!isRadioSelected)
                {
                    #region Event listener 'playing'
                    //pb_progress_bar.Progress = time.TotalMilliseconds / player.MediaDuration() * 1;
                    slider.Value = time.TotalMilliseconds / session.GetMediaDuration() * 1;
                    notificationViewModel.Position = session.GetCurrentPosition();
                    lbl_duration.Text = TimeSpan.FromMilliseconds(session.GetMediaDuration()).ToString(@"h\:mm\:ss");
                    #endregion

                    #region Event listener 'ended (End Reached)'
                    if (isStateEnded)
                    {
                        session.Stop();
                        StartNextEpisode(playlistEpisodes);
                    }
                    #endregion

                    #region Event listener 'error'
                    if (isStateError)
                    {

                    }
                    #endregion
                }
                else
                {
                    //pb_progress_bar.Progress = 0;
                    slider.Value = 0;
                }

            });
        }
        #endregion

        protected override bool OnBackButtonPressed()
        {
            if (grid_playlist.IsVisible)
            {
                grid_playlist.Opacity = 0;
                grid_playlist.IsVisible = false;
            }
            else if (grid_favourites.IsVisible)
            {
                grid_favourites.Opacity = 0;
                grid_favourites.IsVisible = false;
            }
            else if (grid_castDevices.IsVisible)
            {
                grid_castDevices.Opacity = 0;
                grid_castDevices.IsVisible = false;
                //lv_castDevices.IsRefreshing = false;
            }
            else if (grid_nowPlaying.IsVisible)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await grid_nowPlaying.TranslateTo(0, 800, 500, Easing.SpringIn);
                    grid_nowPlaying.IsVisible = false;
                });
            }
            else if (grid_shedule.IsVisible)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await grid_shedule.TranslateTo(0, 800, 500, Easing.SpringIn);
                    grid_shedule.IsVisible = false;
                });
            }
            else if (grid_browser.IsVisible)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await grid_browser.TranslateTo(0, 800, 500, Easing.SpringIn);
                    grid_browser.IsVisible = false;
                });
            }
            else if (grid_about.IsVisible)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await grid_about.TranslateTo(0, 800, 500, Easing.SpringIn);
                    grid_about.IsVisible = false;
                });
            }
            else
            {
                launchActivity.StartNativeIntentOnBackButtonPressed();
            }

            return true;
        }

        public async Task DownloadAndResizeFileAsync(string fileUrl, string downloadedFilePath, string name)
        {
            string file = $"{downloadedFilePath}/{name}.jpg";
            try
            {
                var client = new HttpClient();

                var downloadStream = await client.GetStreamAsync(fileUrl);

                using (var fileStream = System.IO.File.Create(file))
                {
                    await downloadStream.CopyToAsync(fileStream);
                }

                byte[] newImage = radioService.ResizeImageAndroid(System.IO.File.ReadAllBytes(file));
                System.IO.File.WriteAllBytes(file, newImage);
            }
            catch { }
        }

        private void GetShedule()
        {
            foreach (var item in radioService.GetSheduleMonthly())
            {
                sheduleViewModels.Add(item);
            }
        }

        private void GetOldShows()
        {
            try
            {
                foreach (var item in radioService.GetOldShows())
                {
                    oldShowViewModels.Add(item);
                }
                allShowsViewModels
                    .Add(new GroupedCollection<string, ShowViewModel>("Other shows on this radio...", oldShowViewModels));
            }
            catch
            {
                allShowsViewModels.Add(new GroupedCollection<string, ShowViewModel>("Connection error!", oldShowViewModels));
            }

        }

        private void GetFavourites()
        {
            foreach (var ep in radioService.Favourites())
            {
                favouriteEpisodes.Add(ep);
            }
        }

        private void GetRecentlyPlayed()
        {
            foreach (var ep in radioService.RecentlyPlayed())
            {
                recentlyPlayedEpisodes.Add(ep);
            }
        }

        private async void GetMainShows()
        {
            try
            {
                foreach (var item in radioService.GetMainShows())
                {
                    await DownloadAndResizeFileAsync(item.ImageUrl, EXTERNAL_CACHE_DIR, item.Title);

                    mainShowViewModels.Add(item);
                }
                allShowsViewModels
                    .Add(new GroupedCollection<string, ShowViewModel>("MAIN SHOWS", mainShowViewModels));

                //Copy empty episode image to cache folder of the app
                await DownloadAndResizeFileAsync("https://www.tm-radio.com/pic/tm-radio-episode.png", EXTERNAL_CACHE_DIR, "tm_radio_episode");
            }
            catch
            {
                allShowsViewModels.Add(new GroupedCollection<string, ShowViewModel>("Connection error!", mainShowViewModels));
            }

        }

        private void GetRadioMetadataLoop(XspfViewModel radioViewModel)
        {
            isRadioSelected = true;

            radioViewModel = radioService.LoadXspf();

            nowPlayingTarget = radioViewModel.Location;

            session.LoadMedia(nowPlayingTarget);
            //session.LoadMedia("https://www.tm-radio.com/access_mp3.php?mp3=9d7m1n"); //kintar

            session.MediaParse();

            //Create Chromecast metadata
            chromecastMedia = new Media
            {
                ContentUrl = nowPlayingTarget,
                ContentType = "audio/mpeg",
                Metadata = new MusicTrackMetadata
                {
                    Title = "TM-Radio live Stream",
                    AlbumName = radioViewModel.Info,
                    Artist = radioViewModel.StreamDescription,
                    Images = new[]
                        {
                            new Sharpcaster.Models.Media.Image() { Url = "https://www.tm-radio.com/pic/logo/MasterLogo_h250w435_png24.png" }
                        },
                    MetadataType = MetadataType.Music
                }
            };
            //Create notification data
            notificationViewModel.Title = radioViewModel.Title;
            notificationViewModel.Artist = radioViewModel.StreamTitle;
            notificationViewModel.Album = radioViewModel.Info;
            notificationViewModel.Duration = 1000;
            notificationViewModel.Position = session.GetCurrentPosition();
            //Create metadata
            byte[] tmlogo = new HttpClient().GetByteArrayAsync(TMRADIO_LOGO).Result;
            metadataViewModel.Title = radioViewModel.Title;
            metadataViewModel.Artist = radioViewModel.StreamTitle;
            metadataViewModel.Album = radioViewModel.Info;
            metadataViewModel.AlbumArt = BitmapFactory.DecodeByteArray(tmlogo, 0, tmlogo.Length);
            metadataViewModel.Duration = 1000;

            ShowNotification();
            ShowMetadata();

            img_logo.Source = radioViewModel.Logo;
            lbl_tmradiolive_title.Text = radioViewModel.Title.Replace(" - ", " | ");
            lbl_title.Text = lbl_tmradiolive_title.Text;
            lbl_album.Text = radioViewModel.Info;
            lbl_currListeners.Text = $"[Listeners: {radioViewModel.StreamCurrentListeners}]";
            lbl_bitrate.Text = "[Bitrate: 256kbps]";
            lbl_descriptiion.Text = radioViewModel.StreamDescription;
            lbl_duration.Text = "0:00:00";

            Device.StartTimer(TimeSpan.FromSeconds(20), () =>
            {
                radioViewModel = radioService.LoadXspf();

                Device.BeginInvokeOnMainThread(() =>
                {
                    if (isRadioSelected)
                    {
                        lbl_tmradiolive_title.Text = radioViewModel.Title.Replace(" - ", " | ");
                        lbl_title.Text = lbl_tmradiolive_title.Text;
                        lbl_currListeners.Text = $"[Listeners: {radioViewModel.StreamCurrentListeners}]";


                        //Create notification data
                        notificationViewModel.Title = radioViewModel.Title;
                        notificationViewModel.Artist = radioViewModel.StreamTitle;
                        notificationViewModel.Album = radioViewModel.Info;
                        //Create metadata
                        metadataViewModel.Title = radioViewModel.Title;
                        metadataViewModel.Artist = radioViewModel.StreamTitle;
                        metadataViewModel.Album = radioViewModel.Info;

                        ShowNotification();
                        ShowMetadata();
                    }
                    else
                    {
                        lbl_tmradiolive_title.Text = radioViewModel.Title.Replace(" - ", " | ");
                        lbl_currListeners.Text = $"[Listeners: {radioViewModel.StreamCurrentListeners}]";
                    }
                });

                return true;
            });
        }

        private void ShowNotification()
        {
            if (IsPermissionPostNotificationGranted())
            {
                //Show notification
                session.ShowMediaNotification(notificationViewModel);
            }
        }

        private void ShowMetadata()
        {
            //Set metadata
            Device.StartTimer(TimeSpan.FromSeconds(WAIT_FOR_METADATA), () =>
            {
                session.SetPlaybackState();
                session.SetMetadata(metadataViewModel);
                session.InitializeSession();

                return false;
            });
        }

        private bool IsPermissionPostNotificationGranted()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) // Android 13+
            {
                if (context.CheckSelfPermission(Manifest.Permission.PostNotifications)
                    != Permission.Granted)
                {
                    return false;
                }
            }

            return true;
        }

        #region "Player Buttons"
        public void PlayClicked(object sender, EventArgs e)
        {
            session.Play();

            ShowNotification();
            ShowMetadata();
        }

        public void PauseClicked(object sender, EventArgs e)
        {
            if (isRadioSelected)
            {
                session.Stop();
            }
            else
            {
                session.Pause();
            }

            ShowNotification();
            ShowMetadata();
        }

        public void RewindClicked(object sender, EventArgs e)
        {
            //if (!isRadioSelected)
            //{
            //    session.Rewind();
            //    ShowMetadata();
            //}
        }

        private void RewindPressed(object sender, EventArgs e)
        {
            if (!isRadioSelected)
                session.RewindPressed();
        }

        private void RewindReleased(object sender, EventArgs e)
        {
            session.RewindReleased();
            ShowMetadata();
        }

        public void FastForwardClicked(object sender, EventArgs e)
        {
            //if (!isRadioSelected)
            //{
            //    session.FastForward();
            //    ShowMetadata();
            //}
        }
        
        private void FastForwardPressed(object sender, EventArgs e)
        {
            if(!isRadioSelected)
            session.FastForwardPressed();
        }
        
        private void FastForwardReleased(object sender, EventArgs e)
        {
            session.FastForwardReleased();
            ShowMetadata();
        }

        private void SkipToNextClicked(object sender, EventArgs e)
        {
            var episode = playlistEpisodes.Where(x => x.Url == nowPlayingTarget).FirstOrDefault();

            if (episode != null)
            {
                int indx = playlistEpisodes.IndexOf(episode);
                bool isLastEpisode = episode == playlistEpisodes.Last();

                if (!isLastEpisode)
                {
                    session.Stop();

                    var nextEpisode = playlistEpisodes[indx + 1];

                    nowPlayingTarget = nextEpisode.Url;
                    session.LoadMedia(nowPlayingTarget);
                    session.MediaParse();

                    //Create Chromecast metadata
                    chromecastMedia = new Media
                    {
                        ContentUrl = nowPlayingTarget,
                        ContentType = "audio/mpeg",
                        Metadata = new MusicTrackMetadata
                        {
                            Title = nextEpisode.Title,
                            AlbumName = selectedShow,
                            Artist = nextEpisode.Description,
                            Images = new[]
                                {
                            new Sharpcaster.Models.Media.Image() { Url = nextEpisode.ImageArt }
                        },
                            MetadataType = MetadataType.Music
                        }
                    };
                    //Create notification data
                    notificationViewModel.Title = nextEpisode.Title;
                    notificationViewModel.Artist = nextEpisode.Description;
                    notificationViewModel.Album = selectedShow;
                    notificationViewModel.AlbumArt = nextEpisode.ImageSource;
                    notificationViewModel.Duration = session.GetMediaDuration();
                    //Create metadata
                    metadataViewModel.Title = nextEpisode.Title;
                    metadataViewModel.Artist = nextEpisode.Description;
                    metadataViewModel.Album = selectedShow;
                    metadataViewModel.AlbumArt = BitmapFactory.DecodeFile(nextEpisode.ImageSource);
                    metadataViewModel.Duration = session.GetMediaDuration();

                    ShowMetadata();

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        lbl_title.Text = nextEpisode.Title;
                        img_logo.Source = nextEpisode.ImageSource;
                        lbl_duration.Text = TimeSpan.FromMilliseconds(session.GetMediaDuration()).ToString(@"h\:mm\:ss");
                    });

                    session.Play();

                    ShowNotification();
                    ShowMetadata();
                    CreateXmlRecentPlayed(nextEpisode);
                }
            }
        }

        private void SkipToPrevClicked(object sender, EventArgs e)
        {
            var episode = playlistEpisodes.Where(x => x.Url == nowPlayingTarget).FirstOrDefault();

            if (episode != null)
            {
                int indx = playlistEpisodes.IndexOf(episode);
                bool isFirstEpisode = episode == playlistEpisodes.First();

                if (!isFirstEpisode)
                {
                    session.Stop();

                    var prevEpisode = playlistEpisodes[indx - 1];

                    nowPlayingTarget = prevEpisode.Url;
                    session.LoadMedia(nowPlayingTarget);
                    session.MediaParse();

                    //Create Chromecast metadata
                    chromecastMedia = new Media
                    {
                        ContentUrl = nowPlayingTarget,
                        ContentType = "audio/mpeg",
                        Metadata = new MusicTrackMetadata
                        {
                            Title = prevEpisode.Title,
                            AlbumName = selectedShow,
                            Artist = prevEpisode.Description,
                            Images = new[]
                                {
                            new Sharpcaster.Models.Media.Image() { Url = prevEpisode.ImageArt }
                        },
                            MetadataType = MetadataType.Music
                        }
                    };
                    //Create notification data
                    notificationViewModel.Title = prevEpisode.Title;
                    notificationViewModel.Artist = prevEpisode.Description;
                    notificationViewModel.Album = selectedShow;
                    notificationViewModel.AlbumArt = prevEpisode.ImageSource;
                    notificationViewModel.Duration = session.GetMediaDuration();
                    //Create metadata
                    metadataViewModel.Title = prevEpisode.Title;
                    metadataViewModel.Artist = prevEpisode.Description;
                    metadataViewModel.Album = selectedShow;
                    metadataViewModel.AlbumArt = BitmapFactory.DecodeFile(prevEpisode.ImageSource);
                    metadataViewModel.Duration = session.GetMediaDuration();

                    ShowMetadata();

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        lbl_title.Text = prevEpisode.Title;
                        img_logo.Source = prevEpisode.ImageSource;
                        lbl_duration.Text = TimeSpan.FromMilliseconds(session.GetMediaDuration()).ToString(@"h\:mm\:ss");
                    });

                    session.Play();

                    ShowNotification();
                    ShowMetadata();
                    CreateXmlRecentPlayed(prevEpisode);
                }
            }
        }

        private void StartNextEpisode(ObservableCollection<PlaylistEntity> episodes)
        {
            var episode = episodes.Where(x => x.Url == nowPlayingTarget).FirstOrDefault();

            if (episode != null)
            {
                int indx = episodes.IndexOf(episode);
                bool isLastEpisode = episode == episodes.Last();

                if (!isLastEpisode)
                {
                    var nextEpisode = episodes[indx + 1];

                    nowPlayingTarget = nextEpisode.Url;
                    session.LoadMedia(nowPlayingTarget);
                    session.MediaParse();

                    //Create Chromecast metadata
                    chromecastMedia = new Media
                    {
                        ContentUrl = nowPlayingTarget,
                        ContentType = "audio/mpeg",
                        Metadata = new MusicTrackMetadata
                        {
                            Title = nextEpisode.Title,
                            AlbumName = selectedShow,
                            Artist = nextEpisode.Description,
                            Images = new[]
                                {
                            new Sharpcaster.Models.Media.Image() { Url = nextEpisode.ImageArt }
                        },
                            MetadataType = MetadataType.Music
                        }
                    };
                    //Create notification data
                    notificationViewModel.Title = nextEpisode.Title;
                    notificationViewModel.Artist = nextEpisode.Description;
                    notificationViewModel.Album = selectedShow;
                    notificationViewModel.AlbumArt = nextEpisode.ImageSource;
                    notificationViewModel.Duration = session.GetMediaDuration();
                    //Create metadata
                    metadataViewModel.Title = nextEpisode.Title;
                    metadataViewModel.Artist = nextEpisode.Description;
                    metadataViewModel.Album = selectedShow;
                    metadataViewModel.AlbumArt = BitmapFactory.DecodeFile(nextEpisode.ImageSource);
                    metadataViewModel.Duration = session.GetMediaDuration();

                    ShowMetadata();

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        lbl_title.Text = nextEpisode.Title;
                        img_logo.Source = nextEpisode.ImageSource;
                        lbl_duration.Text = TimeSpan.FromMilliseconds(session.GetMediaDuration()).ToString(@"h\:mm\:ss");
                    });

                    session.Play();

                    ShowNotification();
                    ShowMetadata();
                    CreateXmlRecentPlayed(nextEpisode);
                }
            }

            ShowNotification();
            ShowMetadata();
        }
        #endregion

        #region "Main Menu Buttons"
        private async void PlayerMenuClicked(object sender, EventArgs e)
        {
            grid_nowPlaying.IsVisible = true;
            await grid_nowPlaying.TranslateTo(0, 0, 500, Easing.SpringOut);
        }

        private async void SheduleMenuClicked(object sender, EventArgs e)
        {
            grid_shedule.IsVisible = true;
            await grid_shedule.TranslateTo(0, 0, 500, Easing.SpringOut);

            if (!isSheduleCalled)
            {
                Device.StartTimer(TimeSpan.FromSeconds(1), () =>
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        lbl_UTCtime.Text = DateTime.UtcNow.ToString(@"UTC NOW: dddd, MMM dd, HH:mm:ss");
                    });

                    return true;
                });

                list_shedule.ItemsSource = sheduleViewModels;

                await Task.Run(() =>
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        list_shedule.IsRefreshing = true;
                    });

                    GetShedule();
                });

                list_shedule.IsRefreshing = false;
                isSheduleCalled = true;
            }

            if (sheduleViewModels.Any(x => x.Date == DateTime.Now.ToString("dddd, MMM dd")))
            {
                Shedule targetdDate = sheduleViewModels
                    .Where(x => x.Date == DateTime.Now.ToString("dddd, MMM dd"))
                    .FirstOrDefault();
                list_shedule.ScrollTo(targetdDate, ScrollToPosition.Start, true);
            }
        }

        private async void HystoryMenuClicked(object sender, EventArgs e)
        {
            grid_browser.IsVisible = true;
            await grid_browser.TranslateTo(0, 0, 500, Easing.SpringOut);

            if (!isShowsCalled)
            {
                list_browser.ItemsSource = allShowsViewModels;

                await Task.Run(() =>
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        list_browser.IsRefreshing = true;
                    });

                    GetOldShows();
                });

                list_browser.IsRefreshing = false;
                isShowsCalled = true;
            }
        }

        private async void AboutMenuClicked(object sender, EventArgs e)
        {
            grid_about.IsVisible = true;
            await grid_about.TranslateTo(0, 0, 500, Easing.SpringOut);
        }

        private async void PowerOffMenuClicked(object sender, EventArgs e)
        {
            bool isOkToTurnOff = await DisplayAlert("TMRADIO", "You are about to leave the application. Are you sure?", "Leave", "Not now");

            if (isOkToTurnOff)
            {
                radioService.CleanTempDir();
                session.StopSession();
                Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
            }
        }
        #endregion

        #region "Back Arrows Buttons"
        private void SheduleBackArrowClicked(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await grid_shedule.TranslateTo(0, 800, 500, Easing.SpringIn);
                grid_shedule.IsVisible = false;
            });

        }

        private void PlayerBackArrowClicked(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await grid_nowPlaying.TranslateTo(0, 800, 500, Easing.SpringIn);
                grid_nowPlaying.IsVisible = false;
            });
        }

        private void HistoryBackArrowClicked(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await grid_browser.TranslateTo(0, 800, 500, Easing.SpringIn);
                grid_browser.IsVisible = false;
            });
        }

        private void AboutBackArrowClicked(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await grid_about.TranslateTo(0, 800, 500, Easing.SpringIn);
                grid_about.IsVisible = false;
            });
        }
        #endregion

        private void AutoScroll(ObservableCollection<ShowViewModel> mainShowViewModels)
        {
            int _currentIndex = 0;
            bool _isRunning = true;
            lv_mainShows.Position = _currentIndex;

            // Start auto-scroll
            Device.StartTimer(TimeSpan.FromSeconds(10), () =>
            {
                if (!_isRunning || mainShowViewModels.Count == 0) return false;

                _currentIndex = (_currentIndex + 1) % mainShowViewModels.Count;
                lv_mainShows.Position = _currentIndex;

                return true; // keep timer running
            });
        }

        private async void ListenNowClicked(object sender, EventArgs e)
        {
            bool isOk = await DisplayAlert($"TMRADIO", $"Play live stream now?", "play", "cancel");

            if (isOk)
            {
                session.Stop();

                GetRadioMetadataLoop(radioViewModel);

                grid_nowPlaying.IsVisible = true;
                await grid_nowPlaying.TranslateTo(0, 0, 500, Easing.SpringOut);

                session.Play();

                ShowNotification();
            }
        }

        #region "On Demand"
        private async void HistoryListItemTapped(object sender, ItemTappedEventArgs e)
        {
            show = (ShowViewModel)e.Item;
            lbl_playlistCount.Text = $"Sessions: 0";

            bool isOk = await DisplayAlert($"TMRADIO", $"Load episodes from {show.Title}?", "load", "cancel");

            if (isOk)
            {
                grid_playlist.IsVisible = true;
                await grid_playlist.FadeTo(0.9, 300);

                Device.BeginInvokeOnMainThread(async () =>
                {
                    await grid_browser.TranslateTo(0, 800, 500, Easing.SpringIn);
                    grid_browser.IsVisible = false;
                });

                grid_nowPlaying.IsVisible = true;
                await grid_nowPlaying.TranslateTo(0, 0, 500, Easing.SpringOut);

                if (selectedShow != show.Title)
                {
                    selectedShow = show.Title;

                    playlistEpisodes.Clear();
                    lv_playlist.ItemsSource = playlistEpisodes;

                    await Task.Run(() =>
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            lv_playlist.IsRefreshing = true;
                        });

                        GetPlaylistEntities(show);
                    });

                    lv_playlist.IsRefreshing = false;
                }
            }
        }
        #endregion

        #region "Search Box"
        IEnumerable<GroupedCollection<string, ShowViewModel>> SearchedShows(string searchText = null)
        {
            var models = new ObservableCollection<ShowViewModel>();
            foreach (var item in allShowsViewModels)
            {
                foreach (var model in item.Where(x => x.Title.ToLower().Contains(searchText.ToLower())))
                {
                    models.Add(model);
                }
            }

            var group = new GroupedCollection<string, ShowViewModel>($"Results: {models.Count}", models);
            var collection = new ObservableCollection<GroupedCollection<string, ShowViewModel>>
            {
                group
            };

            if (string.IsNullOrEmpty(searchText))
            {
                return allShowsViewModels;
            }
            else
            {
                return collection;
            }
        }
        private void SearchbarTextChanged(object sender, TextChangedEventArgs e)
        {
            list_browser.ItemsSource = SearchedShows(e.NewTextValue);
        }
        #endregion

        #region "Playlist"
        private async void PlaylistClicked(object sender, EventArgs e)
        {
            grid_playlist.IsVisible = true;
            await grid_playlist.FadeTo(0.9, 300);
        }

        private void PlaylistCloseClicked(object sender, EventArgs e)
        {
            grid_playlist.Opacity = 0;
            grid_playlist.IsVisible = false;
        }

        private async void PlaylistItemTapped(object sender, ItemTappedEventArgs e)
        {
            var episode = (PlaylistEntity)e.Item;

            bool isOk = await DisplayAlert($"{selectedShow}", $"{episode.Title}", "play", "cancel");

            if (isOk)
            {
                session.Stop();

                isRadioSelected = false;
                nowPlayingTarget = episode.Url;

                session.LoadMedia(nowPlayingTarget);

                session.MediaParse();

                long episodeDuration = session.GetMediaDuration();

                //Create Chromecast metadata
                chromecastMedia = new Media
                {
                    ContentUrl = nowPlayingTarget,
                    ContentType = "audio/mpeg",
                    Metadata = new MusicTrackMetadata
                    {
                        Title = episode.Title,
                        AlbumName = selectedShow,
                        Artist = episode.Description,
                        Images = new[]
                            {
                            new Sharpcaster.Models.Media.Image() { Url = episode.ImageArt }
                        },
                        MetadataType = MetadataType.Music
                    }
                };
                //Create notification data
                notificationViewModel.Title = episode.Title;
                notificationViewModel.Artist = episode.Description;
                notificationViewModel.Album = selectedShow;
                notificationViewModel.AlbumArt = episode.ImageSource;
                notificationViewModel.Duration = episodeDuration;

                //Create metadata
                metadataViewModel.Title = episode.Title;
                metadataViewModel.Artist = episode.Description;
                metadataViewModel.Album = selectedShow;
                metadataViewModel.AlbumArt = BitmapFactory.DecodeFile(episode.ImageSource);
                metadataViewModel.Duration = episodeDuration;

                ShowMetadata();

                Device.BeginInvokeOnMainThread(() =>
                {
                    lbl_title.Text = episode.Title;
                    img_logo.Source = episode.ImageSource;
                    lbl_album.Text = selectedShow;
                    lbl_duration.Text = TimeSpan.FromMilliseconds(episodeDuration).ToString(@"h\:mm\:ss");
                });

                session.Play();

                ShowNotification();

                if (recentlyPlayedEpisodes.Any(x => x.Url == nowPlayingTarget))
                {
                    var target = recentlyPlayedEpisodes.First(x => x.Url == nowPlayingTarget);
                    //Add episode to first place of the list and remove it from the old place
                    var xdoc = XDocument.Load(XML_RECENTLYPLAYED_FILE);
                    var targetedNode = xdoc.Descendants("Episode").ToList()[recentlyPlayedEpisodes.IndexOf(target)];
                    targetedNode.Remove();
                    xdoc.Root.AddFirst(targetedNode);
                    xdoc.Save(XML_RECENTLYPLAYED_FILE);
                    //Refresh view
                    frame_recentlyPlayed.IsVisible = true;
                    recentlyPlayedEpisodes.Clear();
                    GetRecentlyPlayed();
                    cview_recently.ItemsSource = recentlyPlayedEpisodes;
                }
                else
                {
                    CreateXmlRecentPlayed(episode);
                }
            }
        }

        private void CreateXmlRecentPlayed(PlaylistEntity episode)
        {
            //Add episode to XML file (recently played)
            XDocument xdoc = XDocument.Load(XML_RECENTLYPLAYED_FILE);
            XElement newEpisode = new XElement("Episode");
            newEpisode.Add(
                new XElement("Title", episode.Title),
                new XElement("Image", episode.ImageArt),
                new XElement("Show", string.IsNullOrEmpty(selectedShow) ? episode.Show : selectedShow),
                new XElement("Url", episode.Url),
                new XElement("Description", episode.Description));
            xdoc.Element("Episodes").AddFirst(newEpisode);
            //Delete the last episode from the file if all episodes > 15
            if (recentlyPlayedEpisodes.Count > maxCountOfRecentlyPlayedEpisodes)
            {
                xdoc.Element("Episodes").LastNode.Remove();
            }
            xdoc.Save(XML_RECENTLYPLAYED_FILE);
            //Refresh view
            frame_recentlyPlayed.IsVisible = true;
            recentlyPlayedEpisodes.Clear();
            GetRecentlyPlayed();
            cview_recently.ItemsSource = recentlyPlayedEpisodes;
        }

        private void PlaylistItemAppearing(object sender, ItemVisibilityEventArgs e)
        {
            lbl_playlistCount.Text = $"Sessions: {playlistEpisodes.Count}";
        }

        private async void GetPlaylistEntities(ShowViewModel show)
        {
            try
            {
                foreach (var episode in radioService.GetPlaylistEntities(show.Id))
                {
                    var image = episode.ImageArt.StartsWith("/") ? $"https://www.tm-radio.com{episode.ImageArt}" : episode.ImageArt;
                    episode.ImageArt = image;

                    await DownloadAndResizeFileAsync(image, $"{EXTERNAL_CACHE_DIR}/Temp", episode.Title);

                    playlistEpisodes.Add(episode);
                }
            }
            catch
            {

            }

            if (!playlistEpisodes.Any())
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    lbl_playlistCount.Text = "Sessions: 0 (Nothing found!)";
                });
            }
        }
        #endregion

        #region "Social Media Buttons"
        private async void FacebookLinkClicked(object sender, EventArgs e)
        {
            var isOk = await DisplayAlert("TMRADIO", "Open link?", "ok", "cancel");
            if (isOk)
            {
                await Browser.OpenAsync(TM_FACEBOOK, BrowserLaunchMode.SystemPreferred);
            }
        }

        private async void TwitterLinkClicked(object sender, EventArgs e)
        {
            var isOk = await DisplayAlert("TMRADIO", "Open link?", "ok", "cancel");
            if (isOk)
            {
                await Browser.OpenAsync(TM_TWITTER, BrowserLaunchMode.SystemPreferred);
            }
        }

        private async void WebsiteLinkClicked(object sender, EventArgs e)
        {
            var isOk = await DisplayAlert("TMRADIO", "Open link?", "ok", "cancel");
            if (isOk)
            {
                await Browser.OpenAsync(TM_WEBSITE, BrowserLaunchMode.SystemPreferred);
            }
        }

        #endregion

        #region "Slider"
        private void SliderValueChanged(object sender, ValueChangedEventArgs e)
        {

        }

        private void SliderDragCompleted(object sender, EventArgs e)
        {
            session.SetCurrentTime(slider.Value);
            session.Play();
            timer.Start();

            ShowMetadata();
        }

        private void SliderDragStarted(object sender, EventArgs e)
        {
            session.Pause();
            timer.Stop();
        }
        #endregion

        private async void RecentlyPlayedListItemTapped(object sender, SelectionChangedEventArgs e)
        {
            var episode = (PlaylistEntity)e.CurrentSelection.FirstOrDefault();

            if (episode != null)
            {
                bool isOk = await DisplayAlert($"{episode.Show}", $"{episode.Title}", "play", "cancel");

                if (isOk)
                {
                    var image = episode.ImageArt.StartsWith("/") ? $"https://www.tm-radio.com{episode.ImageArt}" : episode.ImageArt;
                    episode.ImageArt = image;

                    await DownloadAndResizeFileAsync(image, $"{EXTERNAL_CACHE_DIR}/Temp", episode.Title);

                    session.Stop();

                    grid_nowPlaying.IsVisible = true;
                    await grid_nowPlaying.TranslateTo(0, 0, 500, Easing.SpringOut);

                    isRadioSelected = false;
                    nowPlayingTarget = episode.Url;

                    session.LoadMedia(nowPlayingTarget);

                    session.MediaParse();

                    long episodeDuration = session.GetMediaDuration();

                    //Create Chromecast metadata
                    chromecastMedia = new Media
                    {
                        ContentUrl = nowPlayingTarget,
                        ContentType = "audio/mpeg",
                        Metadata = new MusicTrackMetadata
                        {
                            Title = episode.Title,
                            AlbumName = episode.Show,
                            Artist = episode.Description,
                            Images = new[]
                                {
                            new Sharpcaster.Models.Media.Image() { Url = image }
                        },
                            MetadataType = MetadataType.Music
                        }
                    };
                    //Create notification data
                    notificationViewModel.Title = episode.Title;
                    notificationViewModel.Artist = episode.Description;
                    notificationViewModel.Album = episode.Show;
                    notificationViewModel.AlbumArt = episode.ImageSource;
                    notificationViewModel.Duration = episodeDuration;

                    //Create metadata
                    metadataViewModel.Title = episode.Title;
                    metadataViewModel.Artist = episode.Description;
                    metadataViewModel.Album = episode.Show;
                    metadataViewModel.AlbumArt = BitmapFactory.DecodeFile(episode.ImageSource);
                    metadataViewModel.Duration = episodeDuration;

                    ShowMetadata();

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        lbl_title.Text = episode.Title;
                        img_logo.Source = episode.ImageSource;
                        lbl_album.Text = episode.Show;
                        lbl_duration.Text = TimeSpan.FromMilliseconds(episodeDuration).ToString(@"h\:mm\:ss");
                    });

                    session.Play();

                    ShowNotification();

                    //Add episode to first place of the list and remove it from the old place
                    var xdoc = XDocument.Load(XML_RECENTLYPLAYED_FILE);
                    var targetedNode = xdoc.Descendants("Episode").ToList()[recentlyPlayedEpisodes.IndexOf(episode)];
                    targetedNode.Remove();
                    xdoc.Root.AddFirst(targetedNode);
                    xdoc.Save(XML_RECENTLYPLAYED_FILE);
                    //Refresh view
                    frame_recentlyPlayed.IsVisible = true;
                    recentlyPlayedEpisodes.Clear();
                    GetRecentlyPlayed();
                    cview_recently.ItemsSource = recentlyPlayedEpisodes;
                }

                cview_recently.SelectedItem = null;
            }

        }

        #region "Favourites"
        private async void FavouritesClicked(object  sender, EventArgs e)
        {
            grid_favourites.IsVisible = true;
            await grid_favourites.FadeTo(0.9, 300);
        }

        private void FavouriteCloseClicked(object sender, EventArgs e)
        {
            grid_favourites.Opacity = 0;
            grid_favourites.IsVisible = false;
        }

        private void AddToFavouritesClicked(object sender, EventArgs e)
        {
            var episode = playlistEpisodes.Where(x => x.Url == nowPlayingTarget).FirstOrDefault() ?? recentlyPlayedEpisodes.Where(x => x.Url == nowPlayingTarget).FirstOrDefault();
            
            if (!isRadioSelected)
            {
                if (!favouriteEpisodes.Any(x => x.Url == episode.Url))
                {
                    launchActivity.AddedToFavourites(episode.Title);
                    CreateXmlFavouriteEntity(episode);
                }
                else
                {
                    launchActivity.ExistInFavourites(episode.Title);
                }
            }
        }

        private async void FavouritesItemTapped(object sender, ItemTappedEventArgs e)
        {
            var episode = (PlaylistEntity)e.Item;

            bool isOk = await DisplayAlert($"{episode.Show}", $"{episode.Title}", "play", "cancel");

            if (isOk)
            {
                var image = episode.ImageArt.StartsWith("/") ? $"https://www.tm-radio.com{episode.ImageArt}" : episode.ImageArt;
                episode.ImageArt = image;

                await DownloadAndResizeFileAsync(image, $"{EXTERNAL_CACHE_DIR}/Temp", episode.Title);

                session.Stop();

                grid_nowPlaying.IsVisible = true;
                await grid_nowPlaying.TranslateTo(0, 0, 500, Easing.SpringOut);

                isRadioSelected = false;
                nowPlayingTarget = episode.Url;

                session.LoadMedia(nowPlayingTarget);

                session.MediaParse();

                long episodeDuration = session.GetMediaDuration();

                //Create Chromecast metadata
                chromecastMedia = new Media
                {
                    ContentUrl = nowPlayingTarget,
                    ContentType = "audio/mpeg",
                    Metadata = new MusicTrackMetadata
                    {
                        Title = episode.Title,
                        AlbumName = episode.Show,
                        Artist = episode.Description,
                        Images = new[]
                            {
                            new Sharpcaster.Models.Media.Image() { Url = image }
                        },
                        MetadataType = MetadataType.Music
                    }
                };
                //Create notification data
                notificationViewModel.Title = episode.Title;
                notificationViewModel.Artist = episode.Description;
                notificationViewModel.Album = episode.Show;
                notificationViewModel.AlbumArt = episode.ImageSource;
                notificationViewModel.Duration = episodeDuration;

                //Create metadata
                metadataViewModel.Title = episode.Title;
                metadataViewModel.Artist = episode.Description;
                metadataViewModel.Album = episode.Show;
                metadataViewModel.AlbumArt = BitmapFactory.DecodeFile(episode.ImageSource);
                metadataViewModel.Duration = episodeDuration;

                ShowMetadata();

                Device.BeginInvokeOnMainThread(() =>
                {
                    lbl_title.Text = episode.Title;
                    img_logo.Source = episode.ImageSource;
                    lbl_album.Text = episode.Show;
                    lbl_duration.Text = TimeSpan.FromMilliseconds(episodeDuration).ToString(@"h\:mm\:ss");
                });

                session.Play();

                ShowNotification();

                if (recentlyPlayedEpisodes.Any(x => x.Url == nowPlayingTarget))
                {
                    var target = recentlyPlayedEpisodes.First(x => x.Url == nowPlayingTarget);
                    //Add episode to first place of the list and remove it from the old place
                    var xdoc = XDocument.Load(XML_RECENTLYPLAYED_FILE);
                    var targetedNode = xdoc.Descendants("Episode").ToList()[recentlyPlayedEpisodes.IndexOf(target)];
                    targetedNode.Remove();
                    xdoc.Root.AddFirst(targetedNode);
                    xdoc.Save(XML_RECENTLYPLAYED_FILE);
                    //Refresh view
                    frame_recentlyPlayed.IsVisible = true;
                    recentlyPlayedEpisodes.Clear();
                    GetRecentlyPlayed();
                    cview_recently.ItemsSource = recentlyPlayedEpisodes;
                }
                else
                {
                    CreateXmlRecentPlayed(episode);
                }
            }

            cview_recently.SelectedItem = null;
        }

        private void FavouritesItemAppearing(object sender, ItemVisibilityEventArgs e)
        {
            lbl_favouriteEpisodesCount.Text = $"Sessions: {favouriteEpisodes.Count}";
        }

        private async void FavouriteEntityDeleteClicked(object sender, EventArgs e)
        {
            MenuItem url = (MenuItem)sender;
            var episodeToRemove = favouriteEpisodes.First(x => x.Url == url.CommandParameter.ToString());

            bool isOk = await DisplayAlert($"TMRADIO", $"Remove {episodeToRemove.Title} from Favourites?", "remove", "cancel");
            if (isOk)
            {
                XDocument xdoc = XDocument.Load(XML_FAVOURITES_FILE);
                var targetNode = xdoc.Descendants("Episode").ToList()[favouriteEpisodes.IndexOf(episodeToRemove)];
                targetNode.Remove();
                xdoc.Save(XML_FAVOURITES_FILE);
                //Refresh view
                favouriteEpisodes.Clear();
                GetFavourites();
                lv_favourites.ItemsSource = favouriteEpisodes;

                if (!favouriteEpisodes.Any())
                {
                    lbl_favouriteEpisodesCount.Text = $"Sessions: 0";
                }
            }
        }

        private void CreateXmlFavouriteEntity(PlaylistEntity episode)
        {
            //Add episode to XML file (favourites)
            XDocument xdoc = XDocument.Load(XML_FAVOURITES_FILE);
            XElement newEpisode = new XElement("Episode");
            newEpisode.Add(
                new XElement("Title", episode.Title),
                new XElement("Image", episode.ImageArt),
                new XElement("Show", string.IsNullOrEmpty(selectedShow) ? episode.Show : selectedShow),
                new XElement("Url", episode.Url),
                new XElement("Description", episode.Description));
            xdoc.Element("Episodes").AddFirst(newEpisode);
            xdoc.Save(XML_FAVOURITES_FILE);
            //Refresh view
            //frame_recentlyPlayed.IsVisible = true;
            favouriteEpisodes.Clear();
            GetFavourites();
            lv_favourites.ItemsSource = favouriteEpisodes;
        }
        #endregion

        #region "Author info"
        private async void GithubRepositoryClicked(object sender, EventArgs e)
        {
            var isOk = await DisplayAlert("TMRADIO", "Open github repository?", "ok", "cancel");
            if (isOk)
            {
                await Browser.OpenAsync(GITHUB_REPOSITORY, BrowserLaunchMode.SystemPreferred);
            }
        }

        private async void AuthorEmailClicked(object sender, EventArgs e)
        {
            try
            {
                var email = "gmkuzmanoff@gmail.com";
                var subject = Uri.EscapeDataString("Support Request");
                var body = Uri.EscapeDataString("Hello, I need help with...");
                var mailtoUri = $"mailto:{email}?subject={subject}&body={body}";

                await Xamarin.Essentials.Launcher.OpenAsync(mailtoUri);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Unable to open email app: {ex.Message}", "OK");
            }
        }

        #endregion

        #region "Chromecast"
        private async void ChromecastDevicesDiscoveryClicked(object sender, EventArgs e)
        {
            //Open dialog
            grid_castDevices.IsVisible = true;
            await grid_castDevices.FadeTo(0.9, 300);

            chromecasts = await DiscoverChromecastDevicesAsync(chromecasts);
        }

        private async Task<IEnumerable<ChromecastReceiver>> DiscoverChromecastDevicesAsync(IEnumerable<ChromecastReceiver> chromecasts)
        {
            listCastDevices.Clear();
            lbl_castDevicesCount.Text = $"Chromecast Devices: 0";
            lv_castDevices.ItemsSource = listCastDevices;

            await Task.Run(async () =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    lv_castDevices.IsRefreshing = true;
                });

                // Discover Chromecast devices
                var locator = new ChromecastLocator();
                chromecasts = await locator.FindReceiversAsync(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5));

                if (chromecasts.Any())
                {
                    foreach (var device in chromecasts)
                    {
                        listCastDevices.Add(device);
                    }
                }
            });

            lv_castDevices.IsRefreshing = false;

            if (!chromecasts.Any())
            {
                await DisplayAlert("Chromecast Not Found",
                    "Your device couldn't find any Chromecast receivers.\n\n" +
                    "• Ensure Chromecast is powered on\n" +
                    "• Ensure Chromecast is on the same Wi-Fi network\n" +
                    "• Disable AP/client isolation on your router\n" +
                    "• Restart your router or Chromecast\n\n",
                    "close");
            }

            return chromecasts;
        }

        private void ChromecastDevicesAppearing(object sender, ItemVisibilityEventArgs e)
        {
            lbl_castDevicesCount.Text = $"Chromecast Devices: {listCastDevices.Count}";
        }

        private async void ChromecastDeviceTapped(object sender, ItemTappedEventArgs e)
        {
            client = new ChromecastClient();

            var targetReceiver = (ChromecastReceiver)e.Item;

            bool isReadyToCast = await DisplayAlert($"Chromecast to", 
                $"Device: {targetReceiver.Name}\n" +
                $"Model: {targetReceiver.Model}\n" +
                $"Address: {targetReceiver.DeviceUri.Host}:{targetReceiver.Port}", 
                "cast", "cancel");

            if (isReadyToCast)
            {
                try
                {
                    //Connect to chromecast device
                    await client.ConnectChromecast(targetReceiver);
                }
                catch (Exception x)
                {
                    await DisplayAlert("TMRADIO - Cast dialog", $"{x.Message}", "close");
                    return;
                }

                // Subscribe to events
                client.MediaChannel.StatusChanged += OnMediaStatusChanged;
                client.Disconnected += OnDisconnected;

                // Launch the default media receiver app
                await client.LaunchApplicationAsync(CHROMECAST_RECEIVER_ID); // Default Media Receiver

                //Error Handling Best Practices
                try
                {
                    await client.MediaChannel.LoadAsync(chromecastMedia);
                }
                catch (TimeoutException)
                {
                    await DisplayAlert("TMRADIO - Cast dialog", $"Request timed out - check network connection", "close");
                }
                catch (InvalidOperationException ex)
                {
                    await DisplayAlert("TMRADIO - Cast dialog", $"Invalid operation: {ex.Message}", "close");
                }
                catch (Exception x)
                {
                    await DisplayAlert("TMRADIO - Cast dialog", $"Unexpected error: {x.Message}", "close");
                }
                
            }
        }

        private async void OnDisconnected(object sender, EventArgs e)
        {
            await DisplayAlert("TMRADIO - Cast dialog", $"Disconnected from Chromecast", "close");
        }

        private void OnMediaStatusChanged(object sender, MediaStatus status)
        {
            
        }

        private void ChromecastDialogCloseClicked(object sender, EventArgs e)
        {
            grid_castDevices.Opacity = 0;
            grid_castDevices.IsVisible = false;
        }

        private async void CastDeviceDisconnectClicked(object sender, EventArgs e)
        {
            //MenuItem menuItem = (MenuItem)sender;

            //var deviceToDisconnect = listCastDevices.First(x => x.Name == menuItem.CommandParameter.ToString());

            try
            {
                await client.DisconnectAsync();
            }
            catch (Exception x)
            {
                await DisplayAlert("TMRADIO - Cast dialog", $"{x.Message}", "close");
            }

        }
        #endregion


    }
}
