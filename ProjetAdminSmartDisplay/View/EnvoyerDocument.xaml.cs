using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Windows;
using ProjetAdminSmartDisplay.Model;
using System.Net;

namespace ProjetAdminSmartDisplay
{
    public partial class EnvoyerDocumentView : Window
    {
        private List<Classe> _classes = new List<Classe>();
        private List<string> _selectedFiles = new List<string>(); // Liste des fichiers sélectionnés

        public EnvoyerDocumentView()
        {
            InitializeComponent();
            LoadSalles(); // Charge la liste des salles
        }

        // Charge les salles dans le ItemsControl pour la sélection
        private async void LoadSalles()
        {
            string url = "https://quentinvrns.alwaysdata.net/getAllClasse"; // API pour récupérer les salles
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetStringAsync(url);
                _classes = JsonConvert.DeserializeObject<List<Classe>>(response);

                // Lier les salles à l'ItemsControl pour afficher les CheckBox
                SalleSelectionControl.ItemsSource = _classes;
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

            foreach (var salle in selectedSalles)
            {
                foreach (var file in _selectedFiles)
                {
                    UploadFileToFtp(salle.NomSalle, file);
                }
            }

            MessageBox.Show("Tous les fichiers ont été envoyés avec succès.");
            _selectedFiles.Clear();
            FilesListControl.ItemsSource = null; // Réinitialise la liste des fichiers
        }

        private void UploadFileToFtp(string salleName, string filePath)
        {
            try
            {
                // Construire le chemin complet vers appsettings.json
                string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

                // Vérifier si le fichier de configuration existe
                if (!File.Exists(configFilePath))
                {
                    MessageBox.Show("Le fichier de configuration appsettings.json est introuvable.");
                    return;
                }

                // Charger les informations d'identification depuis le fichier de configuration
                var configJson = File.ReadAllText(configFilePath);
                var config = JsonConvert.DeserializeObject<FtpConfig>(configJson);

                string ftpUsername = config.FtpCredentials.Username;
                string ftpPassword = config.FtpCredentials.Password;

                // Vérifier que les informations ont été chargées
                if (string.IsNullOrEmpty(ftpUsername) || string.IsNullOrEmpty(ftpPassword))
                {
                    MessageBox.Show("Les identifiants FTP ne sont pas définis dans le fichier de configuration.");
                    return;
                }

                // Construire l'URL FTP complète
                string ftpUrl = $"ftp://quentinvrns.fr/Document/{salleName}/{Path.GetFileName(filePath)}";

                // Créer une requête FTP
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.UploadFile;

                // Fournir les informations d'identification
                request.Credentials = new NetworkCredential(ftpUsername, ftpPassword);

                // Activer le mode passif si nécessaire
                request.UsePassive = true;

                // Lire le contenu du fichier
                byte[] fileContents;
                using (FileStream sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fileContents = new byte[sourceStream.Length];
                    sourceStream.Read(fileContents, 0, fileContents.Length);
                }

                // Obtenir le flux de la requête
                request.ContentLength = fileContents.Length;
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }

                // Obtenir la réponse du serveur
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    MessageBox.Show($"Upload du fichier terminé, statut : {response.StatusDescription}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'envoi du fichier {Path.GetFileName(filePath)} : {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void RetourAccueil_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}
