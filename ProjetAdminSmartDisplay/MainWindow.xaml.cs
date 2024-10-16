using ProjetAdminSmartDisplay.View;
using System.Windows;


namespace ProjetAdminSmartDisplay
{
    public partial class MainWindow : Window
    {
        
        private object _initialContent;
        private string _username;

        public MainWindow(string username)
        {
            InitializeComponent();
            //pleine ecran
            WindowState = WindowState.Maximized;
            _username = username;
            UpdateWelcomeMessage();

            // Sauvegarder le contenu initial de MainContentControl
            if (MainContentControl != null)
            {
                _initialContent = MainContentControl.Content;
            }
        }

        private void UpdateWelcomeMessage()
        {
            // Mettre à jour le texte de bienvenue
            WelcomeTextBlock.Text = $"Hello, {_username}";
        }


        private void EcranButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainContentControl != null)
            {
                MainContentControl.Content = new EcranView();
            }
        }

        private void EnvoyerDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainContentControl != null)
            {
                MainContentControl.Content = new EnvoyerDocumentView();
            }
        }

        private void AddClasse_Click(object sender, RoutedEventArgs e)
        {
            if (MainContentControl != null)
            {
                MainContentControl.Content = new AddClasse();
            }
        }

        private void SupprimerLesSalles_Click(object sender, RoutedEventArgs e)
        {
            if (MainContentControl != null)
            {
                MainContentControl.Content = new SupprimerLesSalles();
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            // Restaurer le contenu initial
            if (_initialContent != null && MainContentControl != null)
            {
                MainContentControl.Content = _initialContent;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (MainContentControl != null)
            {
                MainContentControl.Content = new Alerte();
            }

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (MainContentControl != null)
            {
                MainContentControl.Content = new Message();
            }

        }
    }
}
