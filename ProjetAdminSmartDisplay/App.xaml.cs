using System.Windows;

namespace ProjetAdminSmartDisplay
{
    
        public partial class App : Application
        {
            protected override void OnStartup(StartupEventArgs e)
            {
                base.OnStartup(e);

                // Ouvrir la fenêtre de login
                LoginPage loginPage = new LoginPage();
                loginPage.Show();
            }
        }


    
}
