using Newtonsoft.Json;
using ProjetAdminSmartDisplay.Model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ProjetAdminSmartDisplay.View
{
    public partial class SupprimerLesSalles : UserControl
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private FtpCredentials _ftpCredentials;

        public SupprimerLesSalles()
        {
            InitializeComponent();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization",
                "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE3Mjc2MzA0ODMsImV4cCI6MTAxNzI3NjMwNDgzLCJkYXRhIjp7ImlkIjoxLCJ1c2VybmFtZSI6IlF1ZW50aW4ifX0.k7m0hTQ4-6H7mEI9IPcwvtGdjxqk7q_vip-dRCjwavk");

            LoadFtpConfig();
            RefreshData();
        }

        private void LoadFtpConfig()
        {
            string configFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!System.IO.File.Exists(configFilePath))
            {
                MessageBox.Show("Le fichier de configuration appsettings.json est introuvable.");
                return;
            }

            try
            {
                var configJson = System.IO.File.ReadAllText(configFilePath);
                var config = JsonConvert.DeserializeObject<FtpConfig>(configJson);
                _ftpCredentials = config.FtpCredentials;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement de la configuration FTP : {ex.Message}");
            }
        }

        private async void RefreshData()
        {
            await LoadBatiments();
            await LoadEtages();
            await LoadClasses();
        }

        private async Task LoadBatiments()
        {
            string url = "https://quentinvrns.alwaysdata.net/getAllBatiment";
            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var batiments = JsonConvert.DeserializeObject<List<Batiment>>(response);
                BatimentComboBox.ItemsSource = batiments;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des bâtiments : {ex.Message}");
            }
        }

        private async Task LoadEtages()
        {
            string url = "https://quentinvrns.alwaysdata.net/getAllEtage";
            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var etages = JsonConvert.DeserializeObject<List<Etage>>(response);
                EtageComboBox.ItemsSource = etages;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des étages : {ex.Message}");
            }
        }

        private async Task LoadClasses()
        {
            string url = "https://quentinvrns.alwaysdata.net/getAllClasse";
            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var classes = JsonConvert.DeserializeObject<List<Classe>>(response);
                ClasseComboBox.ItemsSource = classes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des classes : {ex.Message}");
            }
        }

        private async void OnDeleteBatiment_Click(object sender, RoutedEventArgs e)
        {
            if (BatimentComboBox.SelectedValue == null)
            {
                MessageBox.Show("Veuillez sélectionner un bâtiment à supprimer.");
                return;
            }

            int batimentId = (int)BatimentComboBox.SelectedValue;

            try
            {
                // Vérifiez s'il y a des classes associées au bâtiment
                var hasClasses = await CheckClassesForBatiment(batimentId);
                if (hasClasses)
                {
                    MessageBox.Show("Impossible de supprimer le bâtiment car il contient des classes liées.");
                    return;
                }

                // Supprime le bâtiment si aucune classe n'y est liée
                string url = $"https://quentinvrns.alwaysdata.net/deleteBatiment/{batimentId}";
                var response = await _httpClient.DeleteAsync(url);
                response.EnsureSuccessStatusCode();
                MessageBox.Show("Bâtiment supprimé avec succès !");
                RefreshData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression du bâtiment : {ex.Message}");
            }
        }

        private async void OnDeleteEtage_Click(object sender, RoutedEventArgs e)
        {
            if (EtageComboBox.SelectedValue == null)
            {
                MessageBox.Show("Veuillez sélectionner un étage à supprimer.");
                return;
            }

            int etageId = (int)EtageComboBox.SelectedValue;

            try
            {
                // Vérifiez s'il y a des classes associées à l'étage
                var hasClasses = await CheckClassesForEtage(etageId);
                if (hasClasses)
                {
                    MessageBox.Show("Impossible de supprimer l'étage car il contient des classes liées.");
                    return;
                }

                // Supprime l’étage si aucune classe n'y est liée
                string url = $"https://quentinvrns.alwaysdata.net/deleteEtage/{etageId}";
                var response = await _httpClient.DeleteAsync(url);
                response.EnsureSuccessStatusCode();
                MessageBox.Show("Étage supprimé avec succès !");
                RefreshData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression de l'étage : {ex.Message}");
            }
        }

        private async Task<bool> CheckClassesForBatiment(int batimentId)
        {
            // Cette méthode vérifie si le bâtiment contient des classes associées
            var etages = await GetEtagesByBatiment(batimentId);
            foreach (var etageId in etages)
            {
                var classes = await GetClassesByEtage(etageId);
                if (classes.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        private async Task<bool> CheckClassesForEtage(int etageId)
        {
            // Cette méthode vérifie si l'étage contient des classes associées
            var classes = await GetClassesByEtage(etageId);
            return classes.Count > 0;
        }

        private async Task<List<int>> GetEtagesByBatiment(int batimentId)
        {
            try
            {
                string url = "https://quentinvrns.alwaysdata.net/getEtagesByBatiment";
                var content = new StringContent(JsonConvert.SerializeObject(new { batimentId }), System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var etages = JsonConvert.DeserializeObject<List<Etage>>(await response.Content.ReadAsStringAsync());
                return etages.ConvertAll(e => e.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la récupération des étages pour le bâtiment : {ex.Message}");
                return new List<int>();
            }
        }

        private async Task<List<Classe>> GetClassesByEtage(int etageId)
        {
            try
            {
                string url = "https://quentinvrns.alwaysdata.net/getClassesByEtage";
                var content = new StringContent(JsonConvert.SerializeObject(new { etageId }), System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var classes = JsonConvert.DeserializeObject<List<Classe>>(await response.Content.ReadAsStringAsync());
                return classes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la récupération des classes pour l'étage : {ex.Message}");
                return new List<Classe>();
            }
        }
        private async void OnRefreshFtpDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (_ftpCredentials == null)
            {
                MessageBox.Show("Impossible de récupérer les identifiants FTP.");
                return;
            }

            string ftpUrl = "ftp://quentinvrns.fr/Document/";
            string ftpUsername = _ftpCredentials.Username;
            string ftpPassword = _ftpCredentials.Password;

            try
            {
                // Clear existing items
                FtpDirectoryListBox.Items.Clear();

                // List directories on the FTP server
                FtpWebRequest listRequest = (FtpWebRequest)WebRequest.Create(ftpUrl);
                listRequest.Method = WebRequestMethods.Ftp.ListDirectory;
                listRequest.Credentials = new NetworkCredential(ftpUsername, ftpPassword);

                using (FtpWebResponse listResponse = (FtpWebResponse)listRequest.GetResponse())
                using (System.IO.StreamReader reader = new System.IO.StreamReader(listResponse.GetResponseStream()))
                {
                    string line = reader.ReadLine();
                    while (!string.IsNullOrEmpty(line))
                    {
                        FtpDirectoryListBox.Items.Add(line);
                        line = reader.ReadLine();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la récupération des dossiers FTP : {ex.Message}");
            }
        }

        private async void OnDeleteSelectedFtpDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (FtpDirectoryListBox.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un dossier à supprimer.");
                return;
            }

            string selectedDirectory = FtpDirectoryListBox.SelectedItem.ToString();

            try
            {
                // Supprimer le dossier FTP sélectionné
                DeleteFtpDirectory(selectedDirectory);

                // Supprimer la classe associée dans la base de données
                await DeleteClassByName(selectedDirectory);

                MessageBox.Show("Dossier FTP et classe associés supprimés avec succès !");
                RefreshData(); // Actualisation après suppression
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression du dossier et de la classe : {ex.Message}");
            }
        }

        // Supprimer la classe associée en fonction du nom
        private async Task DeleteClassByName(string className)
        {
            try
            {
                // Récupérer toutes les classes pour trouver celle qui correspond
                string url = "https://quentinvrns.alwaysdata.net/getAllClasse";
                var response = await _httpClient.GetStringAsync(url);
                var classes = JsonConvert.DeserializeObject<List<Classe>>(response);

                var classe = classes.Find(c => c.NomSalle.Equals(className, StringComparison.OrdinalIgnoreCase));

                if (classe != null)
                {
                    // Supprimer la classe dans la base de données
                    string deleteUrl = $"https://quentinvrns.alwaysdata.net/deleteClasse/{classe.Id}";
                    var deleteResponse = await _httpClient.DeleteAsync(deleteUrl);
                    deleteResponse.EnsureSuccessStatusCode();
                }
                else
                {
                    MessageBox.Show($"Classe avec le nom '{className}' introuvable dans la base de données.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression de la classe associée : {ex.Message}");
            }
        }





        private async void OnDeleteClasse_Click(object sender, RoutedEventArgs e)
        {
            if (ClasseComboBox.SelectedValue == null)
            {
                MessageBox.Show("Veuillez sélectionner une classe à supprimer.");
                return;
            }

            int classeId = (int)ClasseComboBox.SelectedValue;
            string classeName = (ClasseComboBox.SelectedItem as Classe)?.NomSalle;

            try
            {
                // Supprimer le dossier FTP associé à la salle
                if (!string.IsNullOrEmpty(classeName))
                {
                    DeleteFtpDirectory(classeName);
                }

                // Supprimer la salle de la base de données
                string url = $"https://quentinvrns.alwaysdata.net/deleteClasse/{classeId}";
                var response = await _httpClient.DeleteAsync(url);
                response.EnsureSuccessStatusCode();
                MessageBox.Show("Classe et dossier FTP associés supprimés avec succès !");

                RefreshData(); // Actualisation après suppression
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression de la classe : {ex.Message}");
            }
        }


        private void DeleteFtpDirectory(string directoryName)
        {
            if (_ftpCredentials == null)
            {
                MessageBox.Show("Impossible de récupérer les identifiants FTP.");
                return;
            }

            string ftpUrl = $"ftp://quentinvrns.fr/Document/{directoryName}/";
            string ftpUsername = _ftpCredentials.Username;
            string ftpPassword = _ftpCredentials.Password;

            try
            {
                FtpWebRequest listRequest = (FtpWebRequest)WebRequest.Create(ftpUrl);
                listRequest.Method = WebRequestMethods.Ftp.ListDirectory;
                listRequest.Credentials = new NetworkCredential(ftpUsername, ftpPassword);

                List<string> files = new List<string>();
                try
                {
                    using (FtpWebResponse listResponse = (FtpWebResponse)listRequest.GetResponse())
                    using (System.IO.StreamReader reader = new System.IO.StreamReader(listResponse.GetResponseStream()))
                    {
                        string line = reader.ReadLine();
                        while (!string.IsNullOrEmpty(line))
                        {
                            files.Add(line);
                            line = reader.ReadLine();
                        }
                    }
                }
                catch (WebException ex) when ((ex.Response as FtpWebResponse)?.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    Console.WriteLine($"Le dossier FTP '{directoryName}' n'existe pas.");
                    return;
                }

                foreach (string file in files)
                {
                    string fileUrl = ftpUrl + file;
                    FtpWebRequest deleteRequest = (FtpWebRequest)WebRequest.Create(fileUrl);
                    deleteRequest.Method = WebRequestMethods.Ftp.DeleteFile;
                    deleteRequest.Credentials = new NetworkCredential(ftpUsername, ftpPassword);

                    try
                    {
                        using (FtpWebResponse deleteResponse = (FtpWebResponse)deleteRequest.GetResponse())
                        {
                            Console.WriteLine($"Fichier supprimé : {fileUrl}, statut : {deleteResponse.StatusDescription}");
                        }
                    }
                    catch (WebException webEx)
                    {
                        Console.WriteLine($"Erreur lors de la suppression du fichier '{file}': {webEx.Message}");
                        MessageBox.Show($"Erreur lors de la suppression du fichier '{file}' dans le dossier FTP '{directoryName}' : {webEx.Message}");
                    }
                }

                FtpWebRequest deleteDirRequest = (FtpWebRequest)WebRequest.Create(ftpUrl);
                deleteDirRequest.Method = WebRequestMethods.Ftp.RemoveDirectory;
                deleteDirRequest.Credentials = new NetworkCredential(ftpUsername, ftpPassword);

                try
                {
                    using (FtpWebResponse deleteDirResponse = (FtpWebResponse)deleteDirRequest.GetResponse())
                    {
                        Console.WriteLine($"Dossier supprimé : {directoryName}, statut : {deleteDirResponse.StatusDescription}");
                    }
                }
                catch (WebException webEx)
                {
                    Console.WriteLine($"Erreur lors de la suppression du dossier : {webEx.Message}");
                    MessageBox.Show($"Erreur lors de la suppression du dossier FTP '{directoryName}' : {webEx.Message}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression du dossier FTP pour {directoryName} : {ex.Message}");
            }
        }
    }
}
