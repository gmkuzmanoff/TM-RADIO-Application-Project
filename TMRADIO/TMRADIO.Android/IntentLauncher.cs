using Android.App;
using Android.Content;
using Android.Widget;
using AndroidX.Core.Content;
using Java.Util;
using TMRADIO.Droid;
using TMRADIO.Interfaces;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;

[assembly: Dependency(typeof(IntentLauncher))]
namespace TMRADIO.Droid
{
    public class IntentLauncher : ILaunchActivity
    {
        private readonly Context context;

        public IntentLauncher()
        {
            context = Android.App.Application.Context;
        }

        public void AddedToFavouritesToast(string title)
        {
            Toast.MakeText(context, $"Favourites: + {title}", ToastLength.Long).Show();
        }

        public void ExistInFavouritesToast(string title)
        {
            Toast.MakeText(context, $"{title} already in Favourites!", ToastLength.Long).Show();
        }

        public void RemovedFromFavouritesToast(string title)
        {
            Toast.MakeText(context, $"Favourites: - {title}", ToastLength.Long).Show();
        }

        public void StartNativeIntentOnBackButtonPressed()
        {
            Intent intent = new Intent();
            intent.SetFlags(ActivityFlags.NewTask);
            intent.SetAction(Intent.ActionMain);
            intent.AddCategory(Intent.CategoryHome);
            context.StartActivity(intent);
        }

    }
}