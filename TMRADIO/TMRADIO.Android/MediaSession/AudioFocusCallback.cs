using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using System;
using TMRADIO.Interfaces;
using Xamarin.Forms;
using Context = Android.Content.Context;

namespace TMRADIO.Droid.MediaSession
{
    public class AudioFocusCallback : Java.Lang.Object, AudioManager.IOnAudioFocusChangeListener
    {
        private readonly Context context;
        private AudioFocusRequest audioFocusRequest;
        private readonly IPlayerConnector session;
        private readonly AudioManager audioManager;
        private AudioFocusRequestClass audioFocusRequestClass;

        public AudioFocusCallback()
        {
            context = Android.App.Application.Context;
            DependencyService.Register<IPlayerConnector>();
            session = DependencyService.Get<IPlayerConnector>();
            audioManager = (AudioManager)context.GetSystemService(Context.AudioService);
        }

        [Obsolete]
        public bool RequestAudioFocus()
        {
            //Audio attributes
            var audioAttributes = new AudioAttributes.Builder()
                .SetContentType(AudioContentType.Music)
                .SetLegacyStreamType(Stream.Music)
                .SetUsage(AudioUsageKind.Media)
                .Build();

            audioFocusRequestClass = new AudioFocusRequestClass.Builder(AudioFocus.Gain)
                .SetAudioAttributes(audioAttributes)
                .SetOnAudioFocusChangeListener(this)
                .Build();

            if (Build.VERSION.SdkInt > BuildVersionCodes.O)
            {
                audioFocusRequest = audioManager.RequestAudioFocus(audioFocusRequestClass);
            }
            else
            {
                audioFocusRequest = audioManager.RequestAudioFocus(this, Stream.Music, AudioFocus.Gain);
            }

            if (audioFocusRequest.Equals(AudioFocusRequest.Granted))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void OnAudioFocusChange([GeneratedEnum] AudioFocus focusChange)
        {
            switch (focusChange)
            {
                case AudioFocus.Gain:
                    session.Play();
                    session.SetPlaybackState();
                    // Resume or increase volume
                    break;

                case AudioFocus.Loss:
                    session.Pause();
                    session.SetPlaybackState();
                    // Stop playback completely
                    break;

                case AudioFocus.LossTransient:
                    session.Pause();
                    session.SetPlaybackState();
                    // Pause playback
                    break;

                case AudioFocus.LossTransientCanDuck:
                    // Lower volume
                    break;
            }

            //Toast.MakeText(context, $"AudioFocus: {focusChange}", ToastLength.Long).Show();
        }

        public void AbondonAudioFocus()
        {
            if (audioFocusRequest.Equals(AudioFocusRequest.Granted))
            {
                audioManager.AbandonAudioFocusRequest(audioFocusRequestClass);
            }
        }
    }
}