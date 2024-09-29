using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Input;
using ProjetAdminSmartDisplay.Model;
using System.Windows.Controls;

namespace ProjetAdminSmartDisplay
{
    public partial class EnvoyerDocumentView : UserControl
    {
        private HttpClient _httpClient = new HttpClient();
        private List<Classe> _classes = new List<Classe>();
        private List<string> _selectedFiles = new List<string>(); // Liste des fichiers sélectionnés

        public EnvoyerDocumentView()
        {
            InitializeComponent();

            // Configurer le token d'autorisation pour toutes les requêtes HTTP
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE3Mjc2MzA0ODMsImV4cCI6MTAxNzI3NjMwNDgzLCJkYXRhIjp7ImlkIjoxLCJ1c2VybmFtZSI6IlF1ZW50aW4ifX0.k7m0hTQ4-6H7mEI9IPcwvtGdjxqk7q_vip-dRCjwavk");

            LoadSalles(); // Charge la liste des salles
        }

        // Charge les salles dans le ItemsControl pour la sélection
        private async void LoadSalles()
        {
            string url = "https://quentinvrns.alwaysdata.net/getAllClasse"; // API pour récupérer les salles
            try
            {
                var response = await _httpClient.GetStringAsync(url);
                _classes = JsonConvert.DeserializeObject<List<Classe>>(response);

                // Lier les salles à l'ItemsControl pour afficher les CheckBox
                SalleSelectionControl.ItemsSource = _classes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des salles : {ex.Message}");
            }
        }

        // Gérer le Drag-and-Drop des fichiers
        private void OnFileDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void OnFileDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                _selectedFiles.AddRange(files);

                // Afficher les fichiers sélectionnés dans l'ItemsControl
                FilesListControl.ItemsSource = null;
                FilesListControl.ItemsSource = _selectedFiles;
            }
        }

        // Envoi des fichiers aux salles sélectionnées
        private void OnSendFilesClick(object sender, RoutedEventArgs e)
        {
            var selectedSalles = _classes.FindAll(salle => salle.IsSelected);

            if (selectedSalles.Count == 0 || _selectedFiles.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner au moins une salle et un fichier.");
                return;
            }

            // Charger les informations d'identification une seule fois
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

            foreach (var salle in selectedSalles)
            {
                foreach (var file in _selectedFiles)
                {
                    UploadFileToFtp(salle.NomSalle, file, ftpUsername, ftpPassword);
                }

                // Après avoir téléchargé tous les fichiers pour la salle, mettre à jour image.json
                List<string> filesInDirectory = ListFilesInFtpDirectory(salle.NomSalle, ftpUsername, ftpPassword);
                UpdateImageJsonOnFtp(salle.NomSalle, filesInDirectory, ftpUsername, ftpPassword);
            }

            MessageBox.Show("Tous les fichiers ont été envoyés avec succès.");
            _selectedFiles.Clear();
            FilesListControl.ItemsSource = null; // Réinitialise la liste des fichiers
        }

        private void UploadFileToFtp(string salleName, string filePath, string ftpUsername, string ftpPassword)
        {
            try
            {
                // Construire l'URL FTP complète pour le fichier à télécharger
                string ftpUrl = $"ftp://quentinvrns.fr/Document/{salleName}/{Path.GetFileName(filePath)}";

                // Créer une requête FTP pour le téléchargement du fichier
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
                request.UsePassive = true;

                // Lire le contenu du fichier local
                byte[] fileContents;
                using (FileStream sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fileContents = new byte[sourceStream.Length];
                    sourceStream.Read(fileContents, 0, fileContents.Length);
                }

                // Envoyer le fichier au serveur FTP
                request.ContentLength = fileContents.Length;
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    MessageBox.Show($"Fichier {Path.GetFileName(filePath)} envoyé à {salleName}, statut : {response.StatusDescription}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'envoi du fichier {Path.GetFileName(filePath)} à {salleName} : {ex.Message}\n{ex.StackTrace}");
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

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string line = reader.ReadLine();
                    while (!string.IsNullOrEmpty(line))
                    {
                        // Exclure le fichier image.json de la liste
                        if (line != "image.json")
                        {
                            files.Add(line);
                        }
                        line = reader.ReadLine();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la liste des fichiers pour {salleName} : {ex.Message}");
            }
            return files;
        }

        // Méthode pour mettre à jour le fichier image.json sur le serveur FTP
        private void UpdateImageJsonOnFtp(string salleName, List<string> fileList, string ftpUsername, string ftpPassword)
        {
            try
            {
                string ftpUrl = $"ftp://quentinvrns.fr/Document/{salleName}/image.json";
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
                request.UsePassive = true;

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
                    MessageBox.Show($"image.json mis à jour pour {salleName}, statut : {response.StatusDescription}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la mise à jour de image.json pour {salleName} : {ex.Message}");
            }
        }

        private void RetourAccueil_Click(object sender, RoutedEventArgs e)
        {
            // Supposons que le UserControl soit chargé dans un ContentControl
            if (this.Parent is ContentControl contentControl)
            {
                contentControl.Content = null; // Vous pouvez revenir à une vue par défaut ou à la page d'accueil
            }
        }
    }
}
