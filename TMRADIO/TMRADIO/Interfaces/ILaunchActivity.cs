using System;
using System.Collections.Generic;
using System.Text;
using TMRADIO.Models;

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
