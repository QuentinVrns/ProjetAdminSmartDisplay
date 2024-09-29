using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ProjetAdminSmartDisplay.Model;
using System.Linq;

namespace ProjetAdminSmartDisplay
{
    public partial class EcranView : UserControl
    {
        private HttpClient _httpClient = new HttpClient();
        private List<Batiment> _batiments = new List<Batiment>();
        private List<Etage> _etages = new List<Etage>();
        private List<Classe> _classes = new List<Classe>();

        public EcranView()
        {
            InitializeComponent();

            // Configurer le token d'autorisation pour toutes les requêtes HTTP
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE3Mjc2MzA0ODMsImV4cCI6MTAxNzI3NjMwNDgzLCJkYXRhIjp7ImlkIjoxLCJ1c2VybmFtZSI6IlF1ZW50aW4ifX0.k7m0hTQ4-6H7mEI9IPcwvtGdjxqk7q_vip-dRCjwavk");

            LoadBatiments();
        }

        // Méthode pour charger les bâtiments
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
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Erreur lors du chargement des bâtiments : {ex.Message}");
                });
            }
        }

        // Méthode appelée lors de la sélection d'un bâtiment
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
                        ClasseComboBox.ItemsSource = null;  // Réinitialiser les classes
                        InfoClasseTextBlock.Text = "";
                        ImagesControl.ItemsSource = null;
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Erreur lors du chargement des étages : {ex.Message}");
                    });
                }
            }
        }

        // Méthode appelée lors de la sélection d'un étage
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
                        ClasseComboBox.ItemsSource = filteredClasses;
                        InfoClasseTextBlock.Text = "";
                        ImagesControl.ItemsSource = null;
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Erreur lors du chargement des classes : {ex.Message}");
                    });
                }
            }
        }

        // Méthode appelée lors de la sélection d'une classe
        private async void OnClasseSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClasseComboBox.SelectedItem is Classe selectedClasse)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    InfoClasseTextBlock.Text = $"L'écran de la classe {selectedClasse.NomSalle} s'affiche.";
                });

                // Récupérer les images associées à la salle
                await LoadImagesForClasse(selectedClasse.NomSalle);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    InfoClasseTextBlock.Text = "";
                    ImagesControl.ItemsSource = null;
                });
            }
        }

        // Méthode pour charger les images d'une salle spécifique
        private async Task LoadImagesForClasse(string nomSalle)
        {
            string baseImageUrl = "http://quentinvrns.fr/Document/"; // Utiliser HTTP si possible
            string salleUrl = $"{baseImageUrl}{nomSalle}/";
            string listUrl = $"{salleUrl}image.json"; // Chemin HTTP du fichier JSON pour obtenir la liste des images

            List<string> availableImages = new List<string>();

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetStringAsync(listUrl).ConfigureAwait(false);
                    availableImages = JsonConvert.DeserializeObject<List<string>>(response);
                }

                if (availableImages == null || availableImages.Count == 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Aucune image disponible pour la salle sélectionnée.");
                        ImagesControl.ItemsSource = null;
                    });
                    return;
                }

                // Filtrer les entrées invalides
                availableImages = availableImages
                    .Where(imageName => !string.IsNullOrWhiteSpace(imageName) && imageName != "." && imageName != "..")
                    .ToList();

                if (availableImages.Count == 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Aucune image valide disponible pour la salle sélectionnée.");
                        ImagesControl.ItemsSource = null;
                    });
                    return;
                }

                // Créer une liste d'ImageItem
                List<ImageItem> imageItems = new List<ImageItem>();
                foreach (var imageName in availableImages)
                {
                    // Vérifier que le fichier est une image valide (par extension)
                    string[] validExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
                    string extension = Path.GetExtension(imageName).ToLower();
                    if (!validExtensions.Contains(extension))
                    {
                        // Ignorer les fichiers qui ne sont pas des images
                        continue;
                    }

                    string imageUrl = $"{salleUrl}{imageName}";
                    imageItems.Add(new ImageItem(imageUrl));
                }

                if (imageItems.Count == 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Aucune image valide disponible pour la salle sélectionnée.");
                        ImagesControl.ItemsSource = null;
                    });
                    return;
                }

                // Mettre à jour le ItemsControl avec les objets ImageItem
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ImagesControl.ItemsSource = imageItems;
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Erreur lors du chargement des images : {ex.Message}");
                });
            }
        }

        // Gestionnaire d'événement pour le clic sur le bouton émoji pour revenir à la MainWindow
        private void RetourAccueil_Click(object sender, RoutedEventArgs e)
        {
            // Supposons que le UserControl soit chargé dans un ContentControl
            if (this.Parent is ContentControl contentControl)
            {
                contentControl.Content = null; // Vous pouvez revenir à une vue par défaut ou à la page d'accueil
            }
        }

        // Gestionnaire d'événement pour ouvrir le ComboBox lors du clic n'importe où
        private void ComboBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Trouver le ComboBox parent
            ComboBox comboBox = FindParent<ComboBox>((DependencyObject)e.OriginalSource);
            if (comboBox != null)
            {
                comboBox.IsDropDownOpen = true;
            }
        }

        // Méthode utilitaire pour trouver le parent d'un type spécifique
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            if (child == null) return null;

            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            if (parentObject is T parent)
            {
                return parent;
            }
            else
            {
                return FindParent<T>(parentObject);
            }
        }

        // Gestionnaire d'événement pour le clic sur le bouton de suppression
        private async void DeleteImage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ImageItem imageItem && imageItem.ImageUrl != null)
            {
                string imageName = Path.GetFileName(imageItem.ImageUrl);

                // Obtenir le nom de la salle depuis la classe sélectionnée
                if (ClasseComboBox.SelectedItem is Classe selectedClasse)
                {
                    string salleName = selectedClasse.NomSalle;

                    // Charger les informations d'identification depuis appsettings.json
                    string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                    if (!File.Exists(configFilePath))
                    {
                        MessageBox.Show("Le fichier de configuration appsettings.json est introuvable.");
                        return;
                    }
                    var configJson = File.ReadAllText(configFilePath);
                    var config = JsonConvert.DeserializeObject<FtpConfig>(configJson);

                    string ftpUsername = config.FtpCredentials.Username;
                    string ftpPassword = config.FtpCredentials.Password;

                    // Confirmer la suppression
                    var result = MessageBox.Show($"Voulez-vous vraiment supprimer l'image '{imageName}' ?", "Confirmer la suppression", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes)
                        return;

                    // Supprimer le fichier sur le serveur FTP
                    bool deleteSuccess = await DeleteFileFromFtpAsync(salleName, imageName, ftpUsername, ftpPassword);

                    if (deleteSuccess)
                    {
                        // Mettre à jour image.json en récupérant la nouvelle liste de fichiers
                        List<string> updatedFiles = await Task.Run(() => ListFilesInFtpDirectory(salleName, ftpUsername, ftpPassword));

                        // Mettre à jour image.json sur le serveur FTP
                        bool updateSuccess = await Task.Run(() => UpdateImageJsonOnFtp(salleName, updatedFiles, ftpUsername, ftpPassword));

                        if (updateSuccess)
                        {
                            // Rafraîchir l'affichage des images
                            await LoadImagesForClasse(salleName);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Aucune salle sélectionnée.");
                }
            }
        }

        // Méthode pour supprimer un fichier du serveur FTP de manière asynchrone
        private async Task<bool> DeleteFileFromFtpAsync(string salleName, string fileName, string ftpUsername, string ftpPassword)
        {
            try
            {
                string ftpUrl = $"ftp://quentinvrns.fr/Document/{salleName}/{fileName}";
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                request.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
                request.UsePassive = true;
                request.KeepAlive = false;

                using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
                {
                    Console.WriteLine($"Delete status: {response.StatusDescription}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Erreur lors de la suppression de l'image {fileName} : {ex.Message}");
                });
                return false;
            }
        }

        // Méthode pour lister les fichiers dans le répertoire FTP d'une salle
        private List<string> ListFilesInFtpDirectory(string salleName, string ftpUsername, string ftpPassword)
        {
            List<string> files = new List<string>();
            try
            {
                string ftpDirectoryUrl = $"ftp://quentinvrns.fr/Document/{salleName}/";
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpDirectoryUrl);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
                request.UsePassive = true;
                request.KeepAlive = false;

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string line = reader.ReadLine();
                    while (line != null)
                    {
                        // Exclure '.', '..' et 'image.json' de la liste
                        if (line != "." && line != ".." && line != "image.json")
                        {
                            files.Add(line);
                        }
                        line = reader.ReadLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Erreur lors de la liste des fichiers pour {salleName} : {ex.Message}");
                });
            }
            return files;
        }

        // Méthode pour mettre à jour le fichier image.json sur le serveur FTP
        private bool UpdateImageJsonOnFtp(string salleName, List<string> fileList, string ftpUsername, string ftpPassword)
        {
            try
            {
                string ftpUrl = $"ftp://quentinvrns.fr/Document/{salleName}/image.json";
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
                request.UsePassive = true;
                request.KeepAlive = false;

                // Générer le contenu JSON
                string jsonContent = JsonConvert.SerializeObject(fileList, Formatting.Indented);
                byte[] fileContents = Encoding.UTF8.GetBytes(jsonContent);
                request.ContentLength = fileContents.Length;

                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    Console.WriteLine($"Upload image.json status: {response.StatusDescription}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Erreur lors de la mise à jour de image.json pour {salleName} : {ex.Message}");
                });
                return false;
            }
        }
    }

    // Classe pour représenter un élément d'image avec chargement asynchrone
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
                    // Télécharger l'image en tant que flux
                    var response = await client.GetAsync(ImageUrl).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze(); // Pour améliorer les performances
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ImageSource = bitmap;
                        });
                    }
                }
            }
            catch
            {
                // Gestion des erreurs de chargement
                // Définir une image par défaut ou laisser null
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ImageSource = null;
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
