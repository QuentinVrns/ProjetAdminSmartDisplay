using Newtonsoft.Json;
using ProjetAdminSmartDisplay.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ProjetAdminSmartDisplay.View
{
    public partial class AddClasse : UserControl
    {
        private HttpClient _httpClient = new HttpClient();

        public AddClasse()
        {
            InitializeComponent();

            // Configurer le token d'autorisation pour toutes les requêtes HTTP
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization",
                "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE3Mjc2MzA0ODMsImV4cCI6MTAxNzI3NjMwNDgzLCJkYXRhIjp7ImlkIjoxLCJ1c2VybmFtZSI6IlF1ZW50aW4ifX0.k7m0hTQ4-6H7mEI9IPcwvtGdjxqk7q_vip-dRCjwavk");

            LoadBatiments(); // Charger les bâtiments existants pour les ComboBox.
            LoadEtages(); // Charger tous les étages existants pour la ComboBox.
        }

        // Charger la liste des bâtiments dans le ComboBox pour l'ajout d'un étage
        private async void LoadBatiments()
        {
            string url = "https://quentinvrns.alwaysdata.net/getAllBatiment";
            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var batiments = JsonConvert.DeserializeObject<List<Batiment>>(response);
                BatimentComboBox.ItemsSource = batiments; // Charger la liste des bâtiments dans la ComboBox
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des bâtiments : {ex.Message}");
            }
        }

        // Charger tous les étages dans le ComboBox
        private async void LoadEtages()
        {
            string url = $"https://quentinvrns.alwaysdata.net/getAllEtage"; // URL pour récupérer tous les étages
            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var etages = JsonConvert.DeserializeObject<List<Etage>>(response);
                EtageComboBox.ItemsSource = etages; // Charger tous les étages dans la ComboBox
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des étages : {ex.Message}");
            }
        }

        // Ajouter un bâtiment
        private async void OnAddBatiment_Click(object sender, RoutedEventArgs e)
        {
            string batimentName = BatimentNameTextBox.Text;
            if (string.IsNullOrEmpty(batimentName))
            {
                MessageBox.Show("Veuillez entrer un nom de bâtiment.");
                return;
            }

            string url = "https://quentinvrns.alwaysdata.net/addBatiment";
            var data = new { NomBatiment = batimentName };

            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                MessageBox.Show("Bâtiment ajouté avec succès !");

                // Réinitialiser la TextBox et recharger les bâtiments
                BatimentNameTextBox.Clear();
                LoadBatiments();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ajout du bâtiment : {ex.Message}");
            }
        }

        // Ajouter un étage
        private async void OnAddEtage_Click(object sender, RoutedEventArgs e)
        {
            string etageName = EtageNameTextBox.Text;
            if (string.IsNullOrEmpty(etageName) || BatimentComboBox.SelectedValue == null)
            {
                MessageBox.Show("Veuillez entrer un nom d'étage et sélectionner un bâtiment.");
                return;
            }

            string url = "https://quentinvrns.alwaysdata.net/addEtage";
            var data = new
            {
                NomEtage = etageName,
                BatimentId = BatimentComboBox.SelectedValue
            };

            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                MessageBox.Show("Étage ajouté avec succès !");

                // Réinitialiser la TextBox et recharger les étages
                EtageNameTextBox.Clear();
                LoadEtages();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ajout de l'étage : {ex.Message}");
            }
        }

        // Ajouter une salle et créer un dossier FTP
        private async void OnAddSalle_Click(object sender, RoutedEventArgs e)
        {
            string salleName = SalleNameTextBox.Text;

            if (string.IsNullOrEmpty(salleName))
            {
                MessageBox.Show("Veuillez entrer un nom de salle.");
                return;
            }

            int? etageId = EtageComboBox.SelectedValue as int?;

            // Si aucun étage n'est sélectionné, on attribue un EtageId par défaut
            if (etageId == null)
            {
                etageId = 1; // Par exemple, id de l'étage "Non spécifié"
            }

            string url = "https://quentinvrns.alwaysdata.net/addClasse";
            var data = new
            {
                NomSalle = salleName,
                EtageId = etageId // Peut être null ou valeur par défaut
            };

            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                MessageBox.Show("Salle ajoutée avec succès !");

                // Créer un dossier FTP pour la salle et y ajouter un fichier image.json
                CreateFtpDirectoryAndJson(salleName);

                // Réinitialiser les champs après l'ajout
                SalleNameTextBox.Clear();
                EtageComboBox.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ajout de la salle : {ex.Message}");
            }
        }

        // Créer un dossier sur le FTP et y ajouter un fichier image.json
        private void CreateFtpDirectoryAndJson(string salleName)
        {
            string ftpUrl = $"ftp://quentinvrns.fr/Document/{salleName}/";
            string ftpUsername = "FTP_USERNAME"; // Remplacer par votre username
            string ftpPassword = "FTP_PASSWORD"; // Remplacer par votre password

            try
            {
                // Créer un dossier sur le serveur FTP
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.MakeDirectory;
                request.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    Console.WriteLine($"Dossier créé : {salleName}, statut : {response.StatusDescription}");
                }

                // Créer le fichier image.json dans le nouveau dossier
                string imageUrl = $"{ftpUrl}image.json";
                FtpWebRequest jsonRequest = (FtpWebRequest)WebRequest.Create(imageUrl);
                jsonRequest.Method = WebRequestMethods.Ftp.UploadFile;
                jsonRequest.Credentials = new NetworkCredential(ftpUsername, ftpPassword);

                // Contenu initial du fichier image.json
                string initialJson = "[]";
                byte[] fileContents = Encoding.UTF8.GetBytes(initialJson);
                jsonRequest.ContentLength = fileContents.Length;

                using (Stream requestStream = jsonRequest.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }

                using (FtpWebResponse jsonResponse = (FtpWebResponse)jsonRequest.GetResponse())
                {
                    Console.WriteLine($"image.json créé pour {salleName}, statut : {jsonResponse.StatusDescription}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la création du dossier ou du fichier image.json pour {salleName} : {ex.Message}");
            }
        }
    }
}
