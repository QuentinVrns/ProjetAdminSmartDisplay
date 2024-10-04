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
            //ouvrir en plein ecran
            WindowState = WindowState.Maximized;
        }

        private void EcranButton_Click(object sender, RoutedEventArgs e)
        {
            // Charger EcranView dans le ContentControl et masquer le logo et le titre
            MainContentControl.Content = new EcranView();
            LogoStackPanel.Visibility = Visibility.Collapsed;
        }

        private void EnvoyerDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            // Charger EnvoyerDocumentView dans le ContentControl et masquer le logo et le titre
            MainContentControl.Content = new EnvoyerDocumentView();
            LogoStackPanel.Visibility = Visibility.Collapsed;
        }

        private void AddClasse_Click(object sender, RoutedEventArgs e)
        {
            // Charger une autre vue et masquer le logo et le titre
            MainContentControl.Content = new AddClasse();
            LogoStackPanel.Visibility = Visibility.Collapsed;
        }

        private void SupprimerLesSalles_Click(object sender, RoutedEventArgs e)
        {
            // Charger une autre vue et masquer le logo et le titre
            MainContentControl.Content = new SupprimerLesSalles();
            LogoStackPanel.Visibility = Visibility.Collapsed;
        }



        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            // Vider le contenu du ContentControl et réafficher le logo et le titre
            MainContentControl.Content = null;
            LogoStackPanel.Visibility = Visibility.Visible; // Afficher le logo et le titre
        }
    }
}
