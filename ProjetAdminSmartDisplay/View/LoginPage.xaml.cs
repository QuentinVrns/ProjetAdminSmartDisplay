using System.Windows;

namespace ProjetAdminSmartDisplay
{
    public partial class LoginPage : Window
    {
        public LoginPage()
        {
            InitializeComponent();
            //pleine ecran
            WindowState = WindowState.Maximized;

        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            // Vérification du compte prédéfini
            if (username == "Quentin" && password == "1234")
            {
                // Si l'utilisateur se connecte avec succès, on ouvre la fenêtre principale
                MainWindow mainWindow = new MainWindow(username);
                mainWindow.Show();
                this.Close(); // Fermer la page de connexion
            }
            else
            {
                // Afficher un message d'erreur
                ErrorMessage.Text = "Nom d'utilisateur ou mot de passe incorrect";
                ErrorMessage.Visibility = Visibility.Visible;
            }
        }
    }
}
