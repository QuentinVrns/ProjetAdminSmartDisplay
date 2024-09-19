using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using ProjetAdminSmartDisplay.Model;

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
        private async void OnSendFilesClick(object sender, RoutedEventArgs e)
        {
            // Récupérer les salles sélectionnées (celles dont IsSelected est true)
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
                    try
                    {
                        // Envoie le fichier vers le dossier de la salle sélectionnée
                        await UploadFileToSalle(salle.NomSalle, file);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors de l'envoi du fichier : {ex.Message}");
                    }
                }
            }

            MessageBox.Show("Tous les fichiers ont été envoyés avec succès.");
            _selectedFiles.Clear();
            FilesListControl.ItemsSource = null; // Réinitialise la liste des fichiers
        }



        // Méthode pour envoyer le fichier vers la salle via HTTP POST
        private async Task UploadFileToSalle(string salleName, string filePath)
        {
            string uploadUrl = $"https://taha.alwaysdata.net/image/{salleName}"; // URL pour envoyer le fichier vers la salle

            using (HttpClient client = new HttpClient())
            {
                // Ajouter les informations d'authentification de base
                var byteArray = System.Text.Encoding.ASCII.GetBytes("username:password");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                using (var formContent = new MultipartFormDataContent())
                {
                    try
                    {
                        // Lire le contenu du fichier à partir du chemin local
                        var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("multipart/form-data");

                        // Ajouter le fichier au formulaire
                        formContent.Add(fileContent, "file", Path.GetFileName(filePath));

                        // Affiche l'URL d'envoi pour vérifier qu'elle est correcte
                        MessageBox.Show($"Envoi du fichier vers : {uploadUrl}");

                        // Envoyer la requête POST vers le serveur
                        HttpResponseMessage response = await client.PostAsync(uploadUrl, formContent);

                        // Afficher le code de statut pour voir la réponse du serveur
                        MessageBox.Show($"Réponse du serveur : {response.StatusCode} - {response.ReasonPhrase}");

                        if (response.IsSuccessStatusCode)
                        {
                            MessageBox.Show($"Fichier {Path.GetFileName(filePath)} envoyé avec succès à {salleName}.");
                        }
                        else
                        {
                            // Affiche le contenu de la réponse en cas d'erreur
                            string responseContent = await response.Content.ReadAsStringAsync();
                            throw new Exception($"Erreur lors de l'envoi du fichier : {response.ReasonPhrase}\nDétails : {responseContent}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Affiche l'erreur complète avec le message et la pile d'exécution
                        MessageBox.Show($"Erreur lors de l'envoi du fichier {Path.GetFileName(filePath)} à {salleName} : {ex.Message}\n\n{ex.StackTrace}");
                    }
                }
            }
        }
    }
}


