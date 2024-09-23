using ProjetAdminSmartDisplay.View;
using System;
using System.Windows;

namespace ProjetAdminSmartDisplay
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Méthode pour ouvrir la vue EcranView
        private void EcranButton_Click(object sender, RoutedEventArgs e)
        {
            EcranView ecranView = new EcranView();
            ecranView.Show();
            this.Close();  // Optionnel, ferme la fenêtre principale
        }

        // Méthode pour ouvrir la vue EnvoyerDocumentView
        private void EnvoyerDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            EnvoyerDocumentView envoyerDocumentView = new EnvoyerDocumentView();
            envoyerDocumentView.Show();  // Ouvre la fenêtre "Envoyer un document"
            this.Close();
        }

        private void AddClasse_Click(object sender, RoutedEventArgs e)
        {
            AddClasse addClasse = new AddClasse();
            addClasse.Show();
            this.Close();
            
        }
    }
}
