using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ProjetAdminSmartDisplay.View
{
    public partial class Message : UserControl
    {
        private HttpClient _httpClient = new HttpClient();
        private List<Classe> _classes = new List<Classe>();
        private List<MessageData> _messages = new List<MessageData>();

        public Message()
        {
            InitializeComponent();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization",
                "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE3Mjc2MzA0ODMsImV4cCI6MTAxNzI3NjMwNDgzLCJkYXRhIjp7ImlkIjoxLCJ1c2VybmFtZSI6IlF1ZW50aW4ifX0.k7m0hTQ4-6H7mEI9IPcwvtGdjxqk7q_vip-dRCjwavk");
            LoadClasses();
        }

        private async void LoadClasses()
        {
            string url = "https://quentinvrns.alwaysdata.net/getAllClasse";
            try
            {
                var response = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
                _classes = JsonConvert.DeserializeObject<List<Classe>>(response);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ClasseComboBox.ItemsSource = _classes;
                    ClasseComboBox.DisplayMemberPath = "NomSalle";
                    ClasseComboBox.SelectedValuePath = "Id";
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des classes : {ex.Message}");
            }
        }

        private async void PublishButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedClasse = ClasseComboBox.SelectedValue?.ToString();
            var messageContent = MessageTextBox.Text;

            if (string.IsNullOrEmpty(selectedClasse) || string.IsNullOrEmpty(messageContent))
            {
                MessageBox.Show("Veuillez sélectionner une classe et saisir un message.");
                return;
            }

            var messageData = new
            {
                ClasseId = selectedClasse,
                Message = messageContent
            };

            try
            {
                var jsonContent = new StringContent(JsonConvert.SerializeObject(messageData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://quentinvrns.alwaysdata.net/addMessage", jsonContent).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Message publié avec succès !");
                    Application.Current.Dispatcher.Invoke(() => { MessageTextBox.Clear(); });
                }
                else
                {
                    MessageBox.Show($"Erreur lors de la publication : {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de connexion : {ex.Message}");
            }
        }

        private async void AfficherMessagesButton_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://quentinvrns.alwaysdata.net/getAllMessages";  // Garder votre API inchangée
            try
            {
                var response = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
                _messages = JsonConvert.DeserializeObject<List<MessageData>>(response);

                // Associer le nom de la salle à chaque message
                foreach (var message in _messages)
                {
                    var classe = _classes.Find(c => c.Id == message.ClasseId);
                    message.NomSalle = classe != null ? classe.NomSalle : "Salle inconnue"; // Gestion du cas où la salle n'est pas trouvée
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessagesListBox.ItemsSource = _messages;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'affichage des messages : {ex.Message}");
            }
        }

        private async void DeleteMessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessagesListBox.SelectedItem is MessageData selectedMessage)
            {
                string url = $"https://quentinvrns.alwaysdata.net/deleteMessage/{selectedMessage.Id}";

                try
                {
                    var response = await _httpClient.DeleteAsync(url).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Message supprimé avec succès !");
                        _messages.Remove(selectedMessage);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessagesListBox.ItemsSource = null;
                            MessagesListBox.ItemsSource = _messages;
                        });
                    }
                    else
                    {
                        MessageBox.Show($"Erreur lors de la suppression : {response.ReasonPhrase}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la suppression du message : {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un message à supprimer.");
            }
        }

        public class Classe
        {
            public int Id { get; set; }
            public string NomSalle { get; set; }
        }

        public class MessageData
        {
            public int Id { get; set; }
            public string Message { get; set; }  // Contenu du message
            public int ClasseId { get; set; }     // L'ID de la classe
            public string NomSalle { get; set; }  // Pour stocker le nom de la salle associée
        }
    }
}
