using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ProjetAdminSmartDisplay.Model;  // Assurez-vous que vos modèles sont dans ce namespace

namespace ProjetAdminSmartDisplay
{
    public partial class EcranView : Window
    {
        private HttpClient _httpClient = new HttpClient();
        private List<Batiment> _batiments = new List<Batiment>();
        private List<Etage> _etages = new List<Etage>();
        private List<Classe> _classes = new List<Classe>();

        public EcranView()
        {
            InitializeComponent();
            LoadBatiments();
        }

        // Méthode pour charger les bâtiments
        private async void LoadBatiments()
        {
            string url = "https://quentinvrns.alwaysdata.net/getAllBatiment";
            var response = await _httpClient.GetStringAsync(url);
            _batiments = JsonConvert.DeserializeObject<List<Batiment>>(response);
            BatimentComboBox.ItemsSource = _batiments;
        }

        // Méthode appelée lors de la sélection d'un bâtiment
        private async void OnBatimentSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BatimentComboBox.SelectedValue is int batimentId)
            {
                string url = $"https://quentinvrns.alwaysdata.net/getAllEtage";
                var response = await _httpClient.GetStringAsync(url);
                _etages = JsonConvert.DeserializeObject<List<Etage>>(response);
                EtageComboBox.ItemsSource = _etages.FindAll(etage => etage.BatimentId == batimentId);
                ClasseComboBox.ItemsSource = null;  // Réinitialiser les classes
            }
        }

        // Méthode appelée lors de la sélection d'un étage
        private async void OnEtageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EtageComboBox.SelectedValue is int etageId)
            {
                string url = $"https://quentinvrns.alwaysdata.net/getAllClasse";
                var response = await _httpClient.GetStringAsync(url);
                _classes = JsonConvert.DeserializeObject<List<Classe>>(response);
                ClasseComboBox.ItemsSource = _classes.FindAll(classe => classe.EtageId == etageId);
            }
        }

        // Méthode appelée lors de la sélection d'une classe
        private async void OnClasseSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClasseComboBox.SelectedItem is Classe selectedClasse)
            {
                InfoClasseTextBlock.Text = $"L'écran de la classe {selectedClasse.NomSalle} s'affiche.";

                // Récupérer les images associées à la salle
                await LoadImagesForClasse(selectedClasse.NomSalle);
            }
        }

        // Méthode pour charger les images d'une salle spécifique
        private async Task LoadImagesForClasse(string nomSalle)
        {
            string baseImageUrl = "https://quentinvrns.fr/Document/";
            string salleUrl = $"{baseImageUrl}{nomSalle}/";
            string listUrl = $"{salleUrl}image.json"; // URL du fichier JSON pour obtenir la liste des images

            List<string> availableImages = new List<string>();

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(listUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        availableImages = JsonConvert.DeserializeObject<List<string>>(responseBody);

                        if (availableImages == null || availableImages.Count == 0)
                        {
                            MessageBox.Show("Aucune image disponible pour la salle sélectionnée.");
                            return;
                        }

                        // Concatène chaque nom d'image avec l'URL de base pour former l'URL complète
                        List<string> fullImageUrls = new List<string>();
                        foreach (var imageName in availableImages)
                        {
                            fullImageUrls.Add(salleUrl + imageName);  // Construit l'URL complète
                        }

                        // Met à jour le ItemsControl avec les URLs complètes des images
                        ImagesControl.ItemsSource = fullImageUrls;
                    }
                    else
                    {
                        MessageBox.Show($"Erreur HTTP lors de la récupération des images : {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des images : {ex.Message}");
            }
        }

        // Gestionnaire d'événement pour le clic sur le bouton émoji pour revenir à la MainWindow
        private void RetourAccueil_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            // Fermer la fenêtre actuelle
            this.Close();
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
    }
}
