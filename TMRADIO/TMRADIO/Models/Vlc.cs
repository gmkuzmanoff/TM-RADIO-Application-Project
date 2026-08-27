using LibVLCSharp.Shared;
using System;
using System.ComponentModel;
using System.Timers;

namespace TMRADIO.Models
{
    public class Vlc
    {
        private static Timer seekTimer;
        private static bool isHolding = false;

        //VLC Lib
        public event PropertyChangedEventHandler PropertyChanged;
        private Media media;
        private LibVLC libVLC;
        private MediaPlayer player;
        private readonly string[] userAgents =
        {
            "Mozilla/5.0 (Linux; Android 10; Pixel 3) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36",
            "Safari/537.36",
            "Chrome/120.0.0.0 Mobile",
            "AppleWebKit/537.36 (KHTML, like Gecko)",
            "Mozilla/5.0 (Linux; Android 10; Pixel 3)"
        };

        public Vlc()
        {
            Core.Initialize();
            LibVLC = new LibVLC(enableDebugLogs: true);
            Player = new MediaPlayer(LibVLC) { EnableHardwareDecoding = false };
        }

        public Media Media
        {
            get => media;
            private set => Set(nameof(Media), ref media, value);
        }

        /// <summary>
        /// Gets the <see cref="LibVLCSharp.Shared.LibVLC"/> instance.
        /// </summary>
        public LibVLC LibVLC
        {
            get => libVLC;
            private set => Set(nameof(LibVLC), ref libVLC, value);
        }
        
        /// <summary>
        /// Gets the <see cref="LibVLCSharp.Shared.MediaPlayer"/> instance.
        /// </summary>
        public LibVLCSharp.Shared.MediaPlayer Player
        {
            get => player;
            private set => Set(nameof(Player), ref player, value);
        }

        private void Set<T>(string propertyName, ref T field, T value)
        {
            if (field == null && value != null || field != null && !field.Equals(value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public long CurrentTime { get => Player.Time; set => Player.Time = value; }

        public float CurrentPosition { get => Player.Position; set => Player.Position = value; }

        public VLCState PlayerState()
        {
            try
            {
                return Player.State;
            }
            catch
            {
                return VLCState.NothingSpecial;
            }
            
        }

        public void LoadMedia(string url)
        {
            try
            {
                Media = new Media(LibVLC, url, FromType.FromLocation);
                //Media.AddOption(":network-caching=5000");
                Media.AddOption(":no-video");
                //Media.AddOption(":avcodec-hw=dxva2");//Hardware accelleration - directX
                //Media.AddOption("-vvv");
                //Media.AddOption($":http-user-agent={userAgents[0]}");
                //Media.AddOption(":http-forward-cookies=true");
                //Media.AddOption(":http-continuous=true");
                //Media.AddOption(":http-header=Range:bytes=0-");
                //Media.AddOption("--sout=#transcode{vcodec=h264,acodec=avcodec}:std{access=file,mux=mp3}");
                //Media.AddOption("--demux=avcodec");
                //Media.AddOption($":start-time={Player.Time / 1000}");
                //Media.AddOption(":avcodec-fast");
                //Media.AddOption(":file-caching=2000");
                //Media.AddOption(":http-reconnect");
                
                Player.Media = Media;
            }
            catch
            {
                Media = null;
            }
            
        }

        public void PlayerDispose()
        {
            try
            {
                Player.Dispose();
                LibVLC.Dispose();
            }
            catch
            {

            }
        }

        public void PlayMedia()
        {
            try
            {
                Player.Play();
            }
            catch { }
        }

        public long MediaDuration()
        {
            try
            {
                return Media.Duration;
            }
            catch
            {
                return 0;
            }
        }

        public void MediaParse()
        {
            try
            {
                Media.Parse(MediaParseOptions.ParseNetwork);
                while (media.ParsedStatus != MediaParsedStatus.Done)
                {
                    System.Threading.Thread.Sleep(50);
                }
            }
            catch
            {
                
            }
        }

        public void StopMedia()
        {
            try
            {
                Player.Stop();
            }
            catch { }
        }

        public void PauseMedia()
        {
            try
            {
                Player.Pause();
            }
            catch { }
        }

        public void Skip30sec()
        {
            try
            {
                if (Player.Time + 30000 < Media.Duration)
                {
                    Player.Time += 30000;
                }
                else
                {
                    Player.Time = Media.Duration;
                    Player.Stop();
                }
            }
            catch { }
        }

        public void FastForwardPressed()
        {
            StartSeekForwardTimer();
        }

        public void FastForwardReleased()
        {
            StopSeekTimer();
        }

        public void Back30sec()
        {
            try
            {
                Player.Time -= 30000;
            }
            catch { }
        }
        
        public void RewindPressed()
        {
            StartSeekRewindTimer();
        }

        public void RewindReleased()
        {
            StopSeekTimer();
        }

        public bool IsPlayingMedia()
        {
            try
            {
                if (Player.IsPlaying)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
            
        }

        public bool IsMediaLoaded()
        {
            if (Media == null)
            {
                return false;
            }
            return true;
        }

        public float PlayerRate()
        {
            try
            {
                return Player.Rate;
            }
            catch (Exception)
            {
                return 1f;
            }
        }

        private void StartSeekForwardTimer()
        {
            if (isHolding) return;
            isHolding = true;

            seekTimer = new Timer(300);//every 300ms
            seekTimer.Elapsed += (s, e) =>
            {
                try
                {
                    if (Player.Time + 3000 < Media.Duration)
                    {
                        Player.Time += 3000;
                    }
                    else
                    {
                        Player.Time = Media.Duration;
                        Player.Stop();
                    }
                }
                catch { }
            };
            seekTimer.Start();
        }

        private void StartSeekRewindTimer()
        {
            if (isHolding) return;
            isHolding = true;

            seekTimer = new Timer(300);//every 300ms
            seekTimer.Elapsed += (s, e) =>
            {
                try
                {
                    Player.Time -= 3000;
                }
                catch { }
            };
            seekTimer.Start();
        }

        private void StopSeekTimer()
        {
            isHolding = false;
            seekTimer?.Stop();
            seekTimer?.Dispose();
            seekTimer = null;
        }
    }
}
