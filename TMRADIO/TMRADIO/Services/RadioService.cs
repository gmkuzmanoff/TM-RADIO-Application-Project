using Android.Graphics;
using HtmlAgilityPack;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using TMRADIO.Interfaces;
using TMRADIO.Models;
using static TMRADIO.Constants.Links;

namespace TMRADIO.Services
{
    public class RadioService : IRadioService
    {
        public XspfViewModel LoadXspf()
        {
            XspfViewModel model = new XspfViewModel();
            XmlDocument xmlDocument = new XmlDocument();

            try
            {
                xmlDocument.Load(XSPF);
                XmlNode titleNode = xmlDocument.GetElementsByTagName("title")[1];
                XmlNode locationNode = xmlDocument.GetElementsByTagName("location")[0];
                XmlNode annotationNode = xmlDocument.GetElementsByTagName("annotation")[0];
                XmlNode infoNode = xmlDocument.GetElementsByTagName("info")[0];
                model.Title = WebUtility.HtmlDecode(titleNode.InnerText);
                model.Location = locationNode.InnerText;
                model.Info = infoNode.InnerText;
                string[] annotation = annotationNode.InnerText.Split('\n');

                model.StreamTitle = annotation[0].Split(':')[1].Trim();
                model.StreamDescription = "Best of Underground :: Techno, Tech House, Progressive & Deep House"; //annotation[1].Split(':')[1].Trim() + ": " + annotation[1].Split(':')[2].Trim();
                model.StreamContentType = "audio/mpeg"; //annotation[2].Split(':')[1].Trim();
                model.StreamBitrate = "256 Kbps"; //annotation[3].Split(':')[1].Trim();
                model.StreamCurrentListeners = annotation[4].Split(':')[1].Trim();
                model.StreamPeakListeners = annotation[5].Split(':')[1].Trim();
                model.StreamGenre = "Techno"; //annotation[6].Split(':')[1].Trim();
            }
            catch (Exception)
            {

            }
            
            return model;
        }

        public string SheduleOnString()
        {
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument htmlDoc;
            HtmlNodeCollection week;
            StringBuilder sb = new StringBuilder();

            try
            {
                htmlDoc = htmlWeb.Load(Constants.Links.SHEDULE);
                week = htmlDoc.DocumentNode.SelectNodes("//div[@class='row row-sched']");


                foreach (var html in week)
                {
                    HtmlDocument newDoc = new HtmlDocument();
                    newDoc.LoadHtml(html.InnerHtml);

                    var data = newDoc.DocumentNode.SelectNodes("//div[@class='showbox']");

                    foreach (var node in data)
                    {
                        string date = node.PreviousSibling.InnerText;
                        string time = node.InnerText;

                        if ((char.IsDigit(date[date.Length - 1])))
                        {
                            sb.AppendLine($"\n{string.Join(" ", Regex.Split(date.Trim(), @"(?=[A-Z])"))}");
                        }
                        sb.AppendLine($"{string.Join(" ",Regex.Split(time.Trim(), @"(?=[A-Z])"))}");
                    }
                }
            }
            catch (Exception)
            {
                return "Connection problem!";
            }
            
            return sb.ToString().Trim();
        }

        public ObservableCollection<Shedule> GetSheduleMonthly()
        {
            ObservableCollection<Shedule> list = new ObservableCollection<Shedule>();
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument htmlDoc;
            HtmlNodeCollection week;
            Shedule viewModel;

            try
            {
                htmlDoc = htmlWeb.Load(Constants.Links.SHEDULE);
                week = htmlDoc.DocumentNode.SelectNodes("//div[@class='row row-sched']");

                foreach (var html in week)
                {
                    HtmlDocument newDoc = new HtmlDocument();
                    newDoc.LoadHtml(html.InnerHtml);

                    var data = newDoc.DocumentNode.SelectNodes("//div[@class='showbox']");
                    if (data != null)
                    {
                        foreach (var node in data)
                        {
                            viewModel = new Shedule();
                            string date = node.PreviousSibling.InnerText;
                            string time = node.InnerText;

                            if ((char.IsDigit(date[date.Length - 1])))
                            {
                                var dateTime = DateTime.ParseExact(date, "dddMMMdd", CultureInfo.InvariantCulture);
                                date = dateTime.ToString("dddd, MMM dd");

                                viewModel.Date = date;
                            }
                            
                            viewModel.Shows = string.Join(" ", Regex.Split(time.Trim(), @"(?=[A-Z])")).Replace("  ", " ");

                            list.Add(viewModel);
                        }
                    }
                }
            }
            catch (Exception)
            {
                return new ObservableCollection<Shedule>() { new Shedule() { Date = "Connection error!" } } ;
            }

            return list;
        }

        public ObservableCollection<ShowViewModel> GetMainShows()
        {
            ObservableCollection<ShowViewModel> list = new ObservableCollection<ShowViewModel>();
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument htmlDoc;
            HtmlNodeCollection mainShows;
            ShowViewModel viewModel;

            try
            {
                htmlDoc = htmlWeb.Load(SHOWS);
                mainShows = htmlDoc.DocumentNode.SelectNodes("//div[@class='panel-body panel-body-show']");

                foreach (var show in mainShows)
                {
                    viewModel = new ShowViewModel();
                    string description = show.ChildNodes[1].InnerText;

                    viewModel.Title = WebUtility.HtmlDecode(show.FirstChild.FirstChild.InnerText);
                    viewModel.Id = show.FirstChild.FirstChild.GetAttributeValue("href", string.Empty).Replace("/shows.php?id=", "");
                    viewModel.ImageUrl = show.PreviousSibling.GetAttributeValue("style", "").Replace("background-image:url('", "").Replace("');", "");
                    viewModel.Description = string.Join(" ", Regex.Split(description.Trim(), @"(?=[A-Z])")).Replace("  ", " ").Replace("Airs", "| Airs");
                    viewModel.GroupType = GroupType.Main;
                    list.Add(viewModel);
                }
            }
            catch (Exception)
            {
                
            }

            return list;
        }

        public ObservableCollection<ShowViewModel> GetOldShows()
        {
            ObservableCollection<ShowViewModel> list = new ObservableCollection<ShowViewModel>();
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument htmlDoc;
            HtmlNodeCollection oldShows;
            ShowViewModel viewModel;

            try
            {
                htmlDoc = htmlWeb.Load(SHOWS);
                oldShows = htmlDoc.DocumentNode.SelectNodes("//a[@class='text-nowrap']");

                foreach (var show in oldShows)
                {
                    viewModel = new ShowViewModel
                    {
                        Title = WebUtility.HtmlDecode(show.InnerText).Replace("• ", ""),
                        Id = show.GetAttributeValue("href", string.Empty).Replace("/shows.php?id=", ""),
                        ImageUrl = "compact_disc",
                        GroupType = GroupType.Old
                    };

                    list.Add(viewModel);
                }
            }
            catch (Exception)
            {
                
            }

            return list;
        }

        public ObservableCollection<PlaylistEntity> GetPlaylistEntities(string id)
        {
            HtmlWeb web = new HtmlWeb();
            int pageItems = 1;
            ObservableCollection<PlaylistEntity> list = new ObservableCollection<PlaylistEntity>();
            string url = $"https://www.tm-radio.com/shows.php?id={id}&loadFrom={pageItems}";
            var htmlDoc = web.Load(url);

            while (!string.IsNullOrEmpty(htmlDoc.ParsedText))
            {
                var titles = htmlDoc.DocumentNode.SelectNodes("//div[@class='episode-file']");
                
                var urlsNodes = htmlDoc.DocumentNode.SelectNodes("//audio");

                if (titles == null)
                {
                    break;
                }

                for (int i = 0; i < titles.Count; i++)
                {
                    string title = string.Empty;
                    string img;
                    string description = string.Empty;
                    string webAddress;
                    
                    if (urlsNodes == null)
                    {
                        continue;
                    }

                    try
                    {
                        webAddress = $"https://www.tm-radio.com{urlsNodes[i].GetAttributeValue("src", string.Empty)}";
                    }
                    catch
                    {
                        continue;
                    }

                    try
                    {
                        img = titles[i].ParentNode.PreviousSibling.ChildNodes[1].FirstChild.GetAttributeValue("src", string.Empty);
                        
                        if (img == "/pic/tm-radio-episode.png")
                        {
                            img = string.Empty;
                        }
                    }
                    catch { img = string.Empty; }

                    try
                    {
                        description = titles[i].ParentNode.PreviousSibling.ChildNodes[3].InnerText;
                    }
                    catch { }

                    try
                    {
                        title = titles[i].FirstChild.InnerText.Replace(".mp3", "");
                    }
                    catch { }

                    list.Add(new PlaylistEntity()
                    {
                        ImageArt = img,
                        Title = WebUtility.HtmlDecode(title),             
                        Url = webAddress,
                        Description = description
                    });
                }

                pageItems += 5;
                url = $"https://www.tm-radio.com/shows.php?id={id}&loadFrom={pageItems}";
                htmlDoc = web.Load(url);
            }

            if (!list.Any())
            {
                
            }

            return list;
        }

        public byte[] ResizeImageAndroid(byte[] imageData)
        {
            //int width, height;

            // Load the bitmap
            Bitmap originalImage = BitmapFactory.DecodeByteArray(imageData, 0, imageData.Length);

            //width = (int)(originalImage.Width * scale);
            //height = (int)(originalImage.Height * scale);

            Bitmap resizedImage = Bitmap.CreateScaledBitmap(originalImage, 500, 500, false);
            
            using (MemoryStream ms = new MemoryStream())
            {
                resizedImage.Compress(Bitmap.CompressFormat.Jpeg, 100, ms);
                return ms.ToArray();
            }
        }

        public void CleanTempDir()
        {
            string directoryTemp = $"{ExternalCacheDir}/Temp";
            DirectoryInfo directory = new DirectoryInfo(directoryTemp);

            foreach (var file in directory.GetFiles())
            {
                file.Delete();
            }
        }

        public ObservableCollection<PlaylistEntity> RecentlyPlayed()
        {
            ObservableCollection<PlaylistEntity> list = new ObservableCollection<PlaylistEntity>();

            var xdoc = new XmlDocument();
            xdoc.Load(XmlRecentlyPlayedFile);
            XmlNodeList epNodes = xdoc.GetElementsByTagName("Episode");
            foreach (XmlNode epNode in epNodes)
            {
                var playlistEntity = new PlaylistEntity()
                {
                    Title = epNode.ChildNodes[0].InnerText,
                    ImageArt = string.IsNullOrEmpty(epNode.ChildNodes[1].InnerText) ? $"{ExternalCacheDir}/tm_radio_episode.jpg" : epNode.ChildNodes[1].InnerText,
                    Show = epNode.ChildNodes[2].InnerText,
                    Url = epNode.ChildNodes[3].InnerText,
                    Description = epNode.ChildNodes[4].InnerText
                };

                list.Add(playlistEntity);
            }
            
            return list;
        }
    }
}
