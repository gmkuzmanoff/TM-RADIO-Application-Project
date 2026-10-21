namespace TMRADIO.Interfaces
{
    public interface ILaunchActivity
    {
        void StartNativeIntentOnBackButtonPressed();
        void AddedToFavourites(string title);
        void RemovedFromFavourites(string title);
        void ExistInFavourites(string title);
    }
}
