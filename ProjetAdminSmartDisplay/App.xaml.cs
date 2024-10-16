using ProjetAdminSmartDisplay.View;
using System.Windows;


namespace ProjetAdminSmartDisplay
{
    
        public partial class App : Application
        
        {
        private Alerte _alertePage;
        protected override void OnStartup(StartupEventArgs e)
            {
                base.OnStartup(e);
            _alertePage = new Alerte();

            // Ouvrir la fenêtre de login
            LoginPage loginPage = new LoginPage();
                loginPage.Show();
            }

            protected override void OnExit(ExitEventArgs e)
            {
                // Appeler la méthode pour vider les alertes du fichier via FTP avant de fermer
                _alertePage.DeleteAlertViaFtp();
                base.OnExit(e);
            }
        }


    
}
