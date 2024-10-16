using System;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;

namespace ProjetAdminSmartDisplay.View
{
    public partial class Alerte : UserControl
    {
        private bool alertActive = false;
        private string alertMessage = "";
        private string alertFileUrl = "ftp://quentinvrns.fr/Document/alerte.txt";
        private FtpCredentials _ftpCredentials;

        public Alerte()
        {
            InitializeComponent();
            LoadFtpConfig();
        }

        // Charger la configuration FTP depuis le fichier de configuration
        private void LoadFtpConfig()
        {
            try
            {
                string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                var configJson = File.ReadAllText(configFilePath);
                var config = JsonConvert.DeserializeObject<FtpConfig>(configJson);
                _ftpCredentials = config.FtpCredentials;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement de la configuration FTP : {ex.Message}");
            }
        }

        private void BtnIncendie_Click(object sender, RoutedEventArgs e)
        {
            if (!alertActive)
            {
                alertMessage = "ALERTE INCENDIE";
                SendAlertViaFtp(alertMessage);
                alertActive = true;
            }
            else
            {
                MessageBox.Show("Une alerte est déjà active. Veuillez la désactiver avant de continuer.");
            }
        }

        private void BtnIntrusion_Click(object sender, RoutedEventArgs e)
        {
            if (!alertActive)
            {
                alertMessage = "ALERTE INTRUSION";
                SendAlertViaFtp(alertMessage);
                alertActive = true;
            }
            else
            {
                MessageBox.Show("Une alerte est déjà active. Veuillez la désactiver avant de continuer.");
            }
        }

        private void BtnEvacuation_Click(object sender, RoutedEventArgs e)
        {
            if (!alertActive)
            {
                alertMessage = "ALERTE EVACUATION AUTRE DANGER";
                SendAlertViaFtp(alertMessage);
                alertActive = true;
            }
            else
            {
                MessageBox.Show("Une alerte est déjà active. Veuillez la désactiver avant de continuer.");
            }
        }

        // Désactiver l'alerte en vidant le fichier même si aucune alerte n'est active
        private void BtnDisableAlert_Click(object sender, RoutedEventArgs e)
        {
            DeleteAlertViaFtp();
            alertActive = false; // Toujours réinitialiser l'état d'alerte après suppression
        }

        // Méthode pour envoyer l'alerte via FTP
        private void SendAlertViaFtp(string message)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(alertFileUrl);
                request.Method = WebRequestMethods.Ftp.UploadFile;

                // Credentials pour FTP
                request.Credentials = new NetworkCredential(_ftpCredentials.Username, _ftpCredentials.Password);
                request.EnableSsl = false;

                byte[] fileContents = Encoding.UTF8.GetBytes(message);
                request.ContentLength = fileContents.Length;

                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    MessageBox.Show($"Alerte envoyée : {message}. Statut FTP : {response.StatusDescription}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'envoi de l'alerte via FTP : {ex.Message}");
            }
        }

        // Méthode pour désactiver l'alerte et vider le fichier via FTP
        public void DeleteAlertViaFtp()
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(alertFileUrl);
                request.Method = WebRequestMethods.Ftp.UploadFile;

                // Credentials pour FTP
                request.Credentials = new NetworkCredential(_ftpCredentials.Username, _ftpCredentials.Password);
                request.EnableSsl = false;

                byte[] fileContents = Encoding.UTF8.GetBytes(""); // Efface le contenu
                request.ContentLength = fileContents.Length;

                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }

                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la désactivation de l'alerte via FTP : {ex.Message}");
            }
        }
    }
}
