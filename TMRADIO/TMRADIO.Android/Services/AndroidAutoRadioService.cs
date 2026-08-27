using Android.Runtime;
using Android.Support.V4.Media;
using HtmlAgilityPack;
using System.Net;
using System.Text.RegularExpressions;
using TMRADIO.Droid.Interfaces;
using TMRADIO.Models;
using static TMRADIO.Constants.Links;

namespace TMRADIO.Droid.Services
{
    public class AndroidAutoRadioService : IAndroidAutoRadioService
    {
        public JavaList<PlaylistEntity> GetEpisodesFromSelectedShow(string id)
        {
            HtmlWeb web = new HtmlWeb();
            int pageItems = 1;
            var list = new JavaList<PlaylistEntity>();
            string url = $"https://www.tm-radio.com/shows.php?id={id}&loadFrom={pageItems}";
            var htmlDoc = web.Load(url);

            while (!string.IsNullOrEmpty(htmlDoc.ParsedText))
            {
                var audioNodes = htmlDoc.DocumentNode.SelectNodes("//audio");

                if (audioNodes == null)
                {
                    pageItems += 5;
                    url = $"https://www.tm-radio.com/shows.php?id={id}&loadFrom={pageItems}";
                    htmlDoc = web.Load(url);
                    continue;
                }

                for (int i = 0; i < audioNodes.Count; i++)
                {
                    string title = string.Empty;
                    string img;
                    string description = string.Empty;
                    string webAddress;

                    try
                    {
                        webAddress = $"https://www.tm-radio.com{audioNodes[i].GetAttributeValue("src", string.Empty)}";
                    }
                    catch
                    {
                        continue;
                    }

                    try
                    {
                        img = audioNodes[i].ParentNode.ParentNode.ParentNode.ParentNode.ParentNode.FirstChild.FirstChild.NextSibling.FirstChild.GetAttributeValue("src", string.Empty);

                        if (img == "/pic/tm-radio-episode.png")
                        {
                            img = EMPTY_EPISODE_IMAGE;
                        }
                    }
                    catch { img = string.Empty; }

                    try
                    {
                        description = audioNodes[i].ParentNode.ParentNode.ParentNode.ParentNode.ParentNode.FirstChild.NextSibling.FirstChild.FirstChild.FirstChild.InnerHtml;
                    }
                    catch { }

                    try
                    {
                        title = audioNodes[i].ParentNode.ParentNode.ParentNode.FirstChild.InnerHtml.Replace(".mp3", "");
                    }
                    catch { }

                    MediaDescriptionCompat mediaDescription = new MediaDescriptionCompat.Builder()
                        .SetMediaId(id)
                        .SetTitle(title)
                        .SetIconUri(Android.Net.Uri.Parse(img))
                        .SetMediaUri(Android.Net.Uri.Parse(webAddress))
                        .SetSubtitle("Subtitle")
                        .SetDescription(description)
                        .Build();

                    var mediaItem = new MediaBrowserCompat.MediaItem(mediaDescription, (int)Android.Media.Browse.MediaItemFlags.Playable);

                    list.Add(mediaItem);
                }

                pageItems += 5;
                url = $"https://www.tm-radio.com/shows.php?id={id}&loadFrom={pageItems}";
                htmlDoc = web.Load(url);
            }

            return list;
        }

        public JavaList<ShowViewModel> GetRadioShows()
        {
            var list = new JavaList<ShowViewModel>();
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument htmlDoc;
            HtmlNodeCollection mainShows;

            try
            {
                htmlDoc = htmlWeb.Load(SHOWS);
                mainShows = htmlDoc.DocumentNode.SelectNodes("//div[@class='panel-body panel-body-show']");

                //Get Main Shows
                foreach (var show in mainShows)
                {
                    string description = show.ChildNodes[1].InnerText;

                    var title = WebUtility.HtmlDecode(show.FirstChild.FirstChild.InnerText);
                    var id = show.FirstChild.FirstChild.GetAttributeValue("href", string.Empty).Replace("/shows.php?id=", "");
                    var imageUrl = show.PreviousSibling.GetAttributeValue("style", "").Replace("background-image:url('", "").Replace("');", "");
                    var descript = string.Join(" ", Regex.Split(description.Trim(), @"(?=[A-Z])")).Replace("  ", " ").Replace("Airs", "| Airs");

                    list.Add(new ShowViewModel()
                    {
                        Id = id,
                        Title = title,
                        Description = descript,
                        ImageUrl = imageUrl
                    });
                }
            }
            catch
            {

            }

            return list;
        }
    }
}