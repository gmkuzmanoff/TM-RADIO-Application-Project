namespace TMRADIO.Interfaces
{
    public interface ILaunchActivity
    {
        void StartNativeIntentOnBackButtonPressed();
        void AddedToFavouritesToast(string title);
        void RemovedFromFavouritesToast(string title);
        void ExistInFavouritesToast(string title);
    }
}
