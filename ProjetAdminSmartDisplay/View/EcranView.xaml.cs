using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ProjetAdminSmartDisplay.Model;
using System.Linq;
using System.ComponentModel;

namespace ProjetAdminSmartDisplay
{
    public partial class EcranView : UserControl
    {
        

        private bool isFullScreenOpen = false;
        private HttpClient _httpClient = new HttpClient();
        private List<Batiment> _batiments = new List<Batiment>();
        private List<Etage> _etages = new List<Etage>();
        private List<Classe> _classes = new List<Classe>();
        private FtpConfig _ftpConfig;

        public EcranView()
        {
            InitializeComponent();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization",
                "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE3Mjc2MzA0ODMsImV4cCI6MTAxNzI3NjMwNDgzLCJkYXRhIjp7ImlkIjoxLCJ1c2VybmFtZSI6IlF1ZW50aW4ifX0.k7m0hTQ4-6H7mEI9IPcwvtGdjxqk7q_vip-dRCjwavk");
            LoadFtpConfig();
            LoadBatiments();
        }

        private void LoadFtpConfig()
        {
            string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!File.Exists(configFilePath))
            {
                MessageBox.Show("Le fichier de configuration appsettings.json est introuvable.");
                return;
            }

            string configJson = File.ReadAllText(configFilePath);
            _ftpConfig = JsonConvert.DeserializeObject<FtpConfig>(configJson);
        }

        private async void LoadBatiments()
        {
            string url = "https://quentinvrns.alwaysdata.net/getAllBatiment";
            try
            {
                var response = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
                _batiments = JsonConvert.DeserializeObject<List<Batiment>>(response);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    BatimentComboBox.ItemsSource = _batiments;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des bâtiments : {ex.Message}");
            }
        }

        private void ToggleFullScreen()
        {
            if (isFullScreenOpen)
            {
                // Fermer le mode plein écran
                FullScreenOverlay.Visibility = Visibility.Collapsed;
                FullScreenImage.Source = null;
                FullScreenVideo.Source = null;
            }
            else
            {
                // Afficher en mode plein écran
                FullScreenOverlay.Visibility = Visibility.Visible;
            }
            isFullScreenOpen = !isFullScreenOpen; // Inverser l'état du plein écran
        }

        private void OnImage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is MediaItem mediaItem)
            {
                if (!isFullScreenOpen)
                {
                    // Afficher l'image en plein écran
                    FullScreenImage.Source = new BitmapImage(new Uri(mediaItem.FileUrl));
                    FullScreenImage.Visibility = Visibility.Visible;
                    FullScreenVideo.Visibility = Visibility.Collapsed;
                }
                ToggleFullScreen();
            }
        }

        private void OnVideo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is MediaItem mediaItem)
            {
                if (!isFullScreenOpen)
                {
                    // Afficher la vidéo en plein écran
                    FullScreenVideo.Source = new Uri(mediaItem.FileUrl);
                    FullScreenVideo.Visibility = Visibility.Visible;
                    FullScreenImage.Visibility = Visibility.Collapsed;
                }
                ToggleFullScreen();
            }
        }

        private void CloseFullScreen_Click(object sender, RoutedEventArgs e)
        {
            ToggleFullScreen();
        }

        private async void OnBatimentSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BatimentComboBox.SelectedValue is int batimentId)
            {
                string url = $"https://quentinvrns.alwaysdata.net/getAllEtage";
                try
                {
                    var response = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
                    _etages = JsonConvert.DeserializeObject<List<Etage>>(response);
                    var filteredEtages = _etages.FindAll(etage => etage.BatimentId == batimentId);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        EtageComboBox.ItemsSource = filteredEtages;
                        RoomsControl.ItemsSource = null;
                        ImagesControl.ItemsSource = null;
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors du chargement des étages : {ex.Message}");
                }
            }
        }

        private async void OnEtageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EtageComboBox.SelectedValue is int etageId)
            {
                string url = $"https://quentinvrns.alwaysdata.net/getAllClasse";
                try
                {
                    var response = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
                    _classes = JsonConvert.DeserializeObject<List<Classe>>(response);
                    var filteredClasses = _classes.Where(classe => classe.EtageId == etageId).ToList();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (filteredClasses.Count > 0)
                        {
                            RoomsControl.ItemsSource = filteredClasses;
                        }
                        else
                        {
                            MessageBox.Show("Aucune salle disponible pour cet étage.");
                            RoomsControl.ItemsSource = null;
                        }
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors du chargement des salles : {ex.Message}");
                }
            }
        }

        private async void OnRoomSelected_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Classe selectedClasse)
            {
                await LoadMediaForClasse(selectedClasse.NomSalle);
            }
        }

        private async Task LoadMediaForClasse(string nomSalle)
        {
            string baseMediaUrl = "http://quentinvrns.fr/Document/";
            string salleUrl = $"{baseMediaUrl}{nomSalle}/";
            string listUrl = $"{salleUrl}image.json";

            List<string> availableFiles = new List<string>();

            try
            {
                var response = await _httpClient.GetStringAsync(listUrl).ConfigureAwait(false);
                availableFiles = JsonConvert.DeserializeObject<List<string>>(response);

                if (availableFiles == null || availableFiles.Count == 0)
                {
                    MessageBox.Show("Aucun fichier disponible pour la salle sélectionnée.");
                    ImagesControl.ItemsSource = null;
                    VideosControl.ItemsSource = null;
                    return;
                }

                availableFiles = availableFiles
                    .Where(fileName => !string.IsNullOrWhiteSpace(fileName) && fileName != "." && fileName != "..")
                    .ToList();

                List<MediaItem> mediaItems = availableFiles
                    .Where(fileName => new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".mp4", ".avi", ".mov", ".wmv" }
                    .Contains(Path.GetExtension(fileName).ToLower()))
                    .Select(fileName =>
                    {
                        string fileUrl = $"{salleUrl}{fileName}";
                        return new MediaItem(fileUrl, nomSalle);
                    })
                    .ToList();

                if (mediaItems.Count == 0)
                {
                    MessageBox.Show("Aucun fichier valide disponible.");
                    ImagesControl.ItemsSource = null;
                    VideosControl.ItemsSource = null;
                    return;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Séparez les médias en images et vidéos
                    var images = mediaItems.Where(m => new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" }.Contains(Path.GetExtension(m.FileUrl).ToLower())).ToList();
                    var videos = mediaItems.Where(m => new[] { ".mp4", ".avi", ".mov", ".wmv" }.Contains(Path.GetExtension(m.FileUrl).ToLower())).ToList();

                    ImagesControl.ItemsSource = images;
                    VideosControl.ItemsSource = videos;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des fichiers : {ex.Message}");
            }
        }


        private async void DeleteImage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is MediaItem mediaItem && mediaItem.FileUrl != null)
            {
                string fileName = Path.GetFileName(mediaItem.FileUrl);
                var result = MessageBox.Show($"Voulez-vous vraiment supprimer le fichier '{fileName}' ?", "Confirmer la suppression", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    bool deleteSuccess = await DeleteMediaFromFtpAsync(mediaItem.NomSalle, fileName);

                    if (deleteSuccess)
                    {
                        MessageBox.Show($"Le fichier '{fileName}' a été supprimé avec succès.");
                        await RemoveMediaFromJsonAsync(mediaItem.NomSalle, fileName);
                        await LoadMediaForClasse(mediaItem.NomSalle);
                    }
                }
            }
        }

        private async Task<bool> DeleteMediaFromFtpAsync(string nomSalle, string fileName)
        {
            try
            {
                string ftpUrl = $"ftp://quentinvrns.fr/Document/{nomSalle}/{fileName}";
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.DeleteFile;

                request.Credentials = new NetworkCredential(_ftpConfig.FtpCredentials.Username, _ftpConfig.FtpCredentials.Password);
                request.UsePassive = true;
                request.KeepAlive = false;

                using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
                {
                    Console.WriteLine($"Delete status: {response.StatusDescription}");
                }

                return true;
            }
            catch (WebException ex)
            {
                if (ex.Response is FtpWebResponse response)
                {
                    MessageBox.Show($"Erreur lors de la suppression du fichier du serveur FTP : {response.StatusDescription}");
                }
                else
                {
                    MessageBox.Show($"Erreur FTP inattendue : {ex.Message}");
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression du fichier du serveur FTP : {ex.Message}");
                return false;
            }
        }

        private async Task RemoveMediaFromJsonAsync(string nomSalle, string fileName)
        {
            string jsonUrl = $"http://quentinvrns.fr/Document/{nomSalle}/image.json";

            try
            {
                var response = await _httpClient.GetStringAsync(jsonUrl).ConfigureAwait(false);
                var files = JsonConvert.DeserializeObject<List<string>>(response);

                if (files != null && files.Contains(fileName))
                {
                    files.Remove(fileName);
                    string updatedJson = JsonConvert.SerializeObject(files, Formatting.Indented);
                    byte[] fileContents = Encoding.UTF8.GetBytes(updatedJson);

                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"ftp://quentinvrns.fr/Document/{nomSalle}/image.json");
                    request.Method = WebRequestMethods.Ftp.UploadFile;
                    request.Credentials = new NetworkCredential(_ftpConfig.FtpCredentials.Username, _ftpConfig.FtpCredentials.Password);
                    request.UsePassive = true;
                    request.KeepAlive = false;
                    request.ContentLength = fileContents.Length;

                    using (Stream requestStream = request.GetRequestStream())
                    {
                        await requestStream.WriteAsync(fileContents, 0, fileContents.Length);
                    }

                    MessageBox.Show("Le fichier JSON a été mis à jour avec succès.");
                }
                else
                {
                    MessageBox.Show("Le fichier n'a pas été trouvé dans le fichier JSON.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la mise à jour du fichier JSON : {ex.Message}");
            }
        }

        // Class to represent a media item (image or video) with asynchronous loading
        public class MediaItem : INotifyPropertyChanged
        {
            private BitmapImage _imageSource;
            public string FileUrl { get; set; }
            public string NomSalle { get; set; }

            public BitmapImage ImageSource
            {
                get { return _imageSource; }
                set
                {
                    _imageSource = value;
                    OnPropertyChanged(nameof(ImageSource));
                }
            }

            public MediaItem(string fileUrl, string nomSalle)
            {
                FileUrl = fileUrl;
                NomSalle = nomSalle;

                // Detect media type and load image if it's an image file
                string extension = Path.GetExtension(fileUrl).ToLower();
                if (new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" }.Contains(extension))
                {
                    LoadImageAsync();
                }
                // You can also handle videos here if needed
            }

            



            private async void LoadImageAsync()
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        var response = await client.GetAsync(FileUrl).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();

                        using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                try
                                {
                                    BitmapImage bitmap = new BitmapImage();
                                    bitmap.BeginInit();
                                    bitmap.StreamSource = stream;
                                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // Load the entire image
                                    bitmap.EndInit();

                                    if (bitmap.CanFreeze)
                                    {
                                        bitmap.Freeze();
                                        ImageSource = bitmap;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Erreur lors du chargement de l'image : {ex.Message}\nImage URL: {FileUrl}");
                                }
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Erreur lors du chargement des images : {ex.Message}\nURL: {FileUrl}");
                    });
                }
            }



            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
