using Android;
using Android.Content;
using Android.Content.PM;
using Android.Widget;
using AndroidX.Core.App;
using TMRADIO.Droid;
using TMRADIO.Interfaces;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

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

        public void AddedToFavourites(string title)
        {
            Toast.MakeText(context, $"Favourites: + {title}", ToastLength.Long).Show();
        }

        public void ExistInFavourites(string title)
        {
            Toast.MakeText(context, $"{title} already in Favourites!", ToastLength.Long).Show();
        }

        public void RemovedFromFavourites(string title)
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