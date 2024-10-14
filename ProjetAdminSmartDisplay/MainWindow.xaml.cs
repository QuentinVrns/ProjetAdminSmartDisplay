using ProjetAdminSmartDisplay.View;
using System;
using System.Windows;

namespace ProjetAdminSmartDisplay
{
    public partial class MainWindow : Window
    {

        private object _initialContent;
        public MainWindow()
        {
            InitializeComponent();
            //ouvrir en plein ecran
            WindowState = WindowState.Maximized;

            // Sauvegarder le contenu initial au démarrage
            _initialContent = MainContentControl.Content;
        }

        private void EcranButton_Click(object sender, RoutedEventArgs e)
        {
            // Charger EcranView dans le ContentControl et masquer le logo et le titre
            MainContentControl.Content = new EcranView();
            
        }

        private void EnvoyerDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            // Charger EnvoyerDocumentView dans le ContentControl et masquer le logo et le titre
            MainContentControl.Content = new EnvoyerDocumentView();
            
        }

        private void AddClasse_Click(object sender, RoutedEventArgs e)
        {
            // Charger une autre vue et masquer le logo et le titre
            MainContentControl.Content = new AddClasse();
            
        }

        private void SupprimerLesSalles_Click(object sender, RoutedEventArgs e)
        {
            // Charger une autre vue et masquer le logo et le titre
            MainContentControl.Content = new SupprimerLesSalles();
            
        }



        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            // Restaurer le contenu initial
            MainContentControl.Content = _initialContent;

        }
    }
}
