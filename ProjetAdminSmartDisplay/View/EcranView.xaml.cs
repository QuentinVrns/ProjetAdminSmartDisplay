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
        private HttpClient _httpClient = new HttpClient();
        private List<Batiment> _batiments = new List<Batiment>();
        private List<Etage> _etages = new List<Etage>();
        private List<Classe> _classes = new List<Classe>();
        private FtpConfig _ftpConfig;

        public EcranView()
        {
            InitializeComponent();
            // Restore the token for authorization in HTTP requests
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization",
                "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE3Mjc2MzA0ODMsImV4cCI6MTAxNzI3NjMwNDgzLCJkYXRhIjp7ImlkIjoxLCJ1c2VybmFtZSI6IlF1ZW50aW4ifX0.k7m0hTQ4-6H7mEI9IPcwvtGdjxqk7q_vip-dRCjwavk");
            LoadFtpConfig(); // Load FTP credentials
            LoadBatiments();
        }

        // Load buildings
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

        // When a building is selected
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

        // When an etage is selected
        private async void OnEtageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EtageComboBox.SelectedValue is int etageId)
            {
                string url = $"https://quentinvrns.alwaysdata.net/getAllClasse";
                try
                {
                    var response = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
                    _classes = JsonConvert.DeserializeObject<List<Classe>>(response);
                    var filteredClasses = _classes.FindAll(classe => classe.EtageId == etageId);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        RoomsControl.ItemsSource = filteredClasses;
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors du chargement des classes : {ex.Message}");
                }
            }
        }

        // Method to handle room selection
        private async void OnRoomSelected_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Classe selectedClasse)
            {
                await LoadImagesForClasse(selectedClasse.NomSalle);
            }
        }

        // Load images for a specific room
        private async Task LoadImagesForClasse(string nomSalle)
        {
            string baseImageUrl = "http://quentinvrns.fr/Document/";
            string salleUrl = $"{baseImageUrl}{nomSalle}/";
            string listUrl = $"{salleUrl}image.json";

            List<string> availableImages = new List<string>();

            try
            {
                var response = await _httpClient.GetStringAsync(listUrl).ConfigureAwait(false);
                availableImages = JsonConvert.DeserializeObject<List<string>>(response);

                if (availableImages == null || availableImages.Count == 0)
                {
                    MessageBox.Show("Aucune image disponible pour la salle sélectionnée.");
                    ImagesControl.ItemsSource = null;
                    return;
                }

                availableImages = availableImages
                    .Where(imageName => !string.IsNullOrWhiteSpace(imageName) && imageName != "." && imageName != "..")
                    .ToList();

                List<ImageItem> imageItems = availableImages
                    .Where(imageName => new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" }
                    .Contains(Path.GetExtension(imageName).ToLower()))
                    .Select(imageName =>
                    {
                        string imageUrl = $"{salleUrl}{imageName}";
                        return new ImageItem(imageUrl);
                    })
                    .ToList();

                if (imageItems.Count == 0)
                {
                    MessageBox.Show("Aucune image valide disponible.");
                    ImagesControl.ItemsSource = null;
                    return;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ImagesControl.ItemsSource = imageItems;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des images : {ex.Message}");
            }
        }

        // Load FTP credentials
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

        // Event handler for image deletion (deletes from image.json and the folder)
        private async void DeleteImage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ImageItem imageItem && imageItem.ImageUrl != null)
            {
                string imageName = Path.GetFileName(imageItem.ImageUrl);
                string salleName = ""; // You need to get the selected room/salle here

                var result = MessageBox.Show($"Voulez-vous vraiment supprimer l'image '{imageName}' ?", "Confirmer la suppression", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    bool deleteSuccess = await DeleteImageFromFtpAsync(salleName, imageName);

                    if (deleteSuccess)
                    {
                        MessageBox.Show($"L'image '{imageName}' a été supprimée avec succès.");
                        await RemoveImageFromJsonAsync(salleName, imageName); // Remove from JSON
                        await LoadImagesForClasse(salleName); // Refresh the images list
                    }
                }
            }
        }

        // Delete an image from the FTP server
        private async Task<bool> DeleteImageFromFtpAsync(string salleName, string fileName)
        {
            try
            {
                // Build the FTP URL
                string ftpUrl = $"ftp://quentinvrns.fr/Document/{salleName}/{fileName}";
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.DeleteFile;

                // Set the credentials
                request.Credentials = new NetworkCredential(_ftpConfig.FtpCredentials.Username, _ftpConfig.FtpCredentials.Password);
                request.UsePassive = true; // Ensure passive mode is enabled
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
                    MessageBox.Show($"Erreur lors de la suppression de l'image du serveur FTP : {response.StatusDescription}");
                }
                else
                {
                    MessageBox.Show($"Erreur FTP inattendue : {ex.Message}");
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression de l'image du serveur FTP : {ex.Message}");
                return false;
            }
        }

        // Remove image from image.json
        private async Task RemoveImageFromJsonAsync(string salleName, string imageName)
        {
            string jsonUrl = $"http://quentinvrns.fr/Document/{salleName}/image.json";

            try
            {
                var response = await _httpClient.GetStringAsync(jsonUrl).ConfigureAwait(false);
                var images = JsonConvert.DeserializeObject<List<string>>(response);

                images.Remove(imageName);

                string updatedJson = JsonConvert.SerializeObject(images, Formatting.Indented);
                byte[] fileContents = Encoding.UTF8.GetBytes(updatedJson);

                // Upload the updated image.json file back to the FTP server
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"ftp://quentinvrns.fr/Document/{salleName}/image.json");
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(_ftpConfig.FtpCredentials.Username, _ftpConfig.FtpCredentials.Password);
                request.UsePassive = true;
                request.KeepAlive = false;
                request.ContentLength = fileContents.Length;

                using (Stream requestStream = request.GetRequestStream())
                {
                    await requestStream.WriteAsync(fileContents, 0, fileContents.Length);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la mise à jour du fichier image.json : {ex.Message}");
            }
        }
    }

    // Class to represent an image item with asynchronous loading
    public class ImageItem : INotifyPropertyChanged
    {
        private BitmapImage _imageSource;
        public string ImageUrl { get; set; }

        public BitmapImage ImageSource
        {
            get { return _imageSource; }
            set
            {
                _imageSource = value;
                OnPropertyChanged(nameof(ImageSource));
            }
        }

        public ImageItem(string imageUrl)
        {
            ImageUrl = imageUrl;
            LoadImageAsync();
        }

        private async void LoadImageAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetAsync(ImageUrl).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();

                    using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = stream;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze(); // Freeze the bitmap to make it cross-thread accessible

                            ImageSource = bitmap; // Set the image source on the UI thread
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Erreur lors du chargement des images : {ex.Message}");
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
