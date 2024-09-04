using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProjetAdminSmartDisplay
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        // Dictionnaire pour stocker les étages et les salles associées à chaque bâtiment
        private Dictionary<string, List<Floor>> buildingFloors;

        public MainWindow()
        {
            InitializeComponent();
            LoadBuildingData(); // Charger les données des bâtiments au démarrage
        }

        // Chargement des étages et salles pour chaque bâtiment
        private void LoadBuildingData()
        {
            buildingFloors = new Dictionary<string, List<Floor>>
            {
                { "KM", new List<Floor>
                    {
                        new Floor { Name = "Étage 1", Rooms = new List<Room>
                            {
                                new Room { Name = "Salle KM100", IsOnline = true },
                                new Room { Name = "Salle KM101", IsOnline = false },
                                new Room { Name = "Salle KM102", IsOnline = true },
                                new Room { Name = "Salle KM103", IsOnline = false },
                                new Room { Name = "Salle KM104", IsOnline = true }
                            }
                        },
                        new Floor { Name = "Étage 2", Rooms = new List<Room>
                            {
                                new Room { Name = "Salle KM200", IsOnline = true },
                                new Room { Name = "Salle KM201", IsOnline = true },
                                new Room { Name = "Salle KM202", IsOnline = false },
                                new Room { Name = "Salle KM203", IsOnline = true },
                                new Room { Name = "Salle KM204", IsOnline = false }
                            }
                        },
                        new Floor { Name = "Étage 3", Rooms = new List<Room>
                            {
                                new Room { Name = "Salle KM300", IsOnline = false },
                                new Room { Name = "Salle KM301", IsOnline = true },
                                new Room { Name = "Salle KM302", IsOnline = true },
                                new Room { Name = "Salle KM303", IsOnline = false },
                                new Room { Name = "Salle KM304", IsOnline = true }
                            }
                        }
                    }
                },
                // Ajouter d'autres bâtiments avec leurs étages et salles
            };
        }

        // Événement déclenché lorsqu'un bouton de bâtiment est cliqué
        private void BuildingButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                string buildingKey = button.Tag.ToString();
                if (buildingFloors.ContainsKey(buildingKey))
                {
                    DisplayFloorsAndRooms(buildingFloors[buildingKey]);
                }
            }
        }

        // Méthode pour afficher les étages et salles correspondant au bâtiment sélectionné
        private void DisplayFloorsAndRooms(List<Floor> floors)
        {
            treeViewDetails.Items.Clear(); // Nettoyer l'affichage existant

            foreach (var floor in floors)
            {
                TreeViewItem floorItem = new TreeViewItem { Header = floor.Name };

                foreach (var room in floor.Rooms)
                {
                    // Création d'un stack panel pour inclure la checkbox et l'emoji de statut
                    StackPanel roomPanel = new StackPanel { Orientation = Orientation.Horizontal };

                    // Checkbox pour sélectionner la salle
                    CheckBox checkBox = new CheckBox { Margin = new Thickness(0, 0, 10, 0) };
                    roomPanel.Children.Add(checkBox);

                    // Label de la salle
                    TextBlock roomLabel = new TextBlock { Text = room.Name };
                    roomPanel.Children.Add(roomLabel);

                    // Emoji pour l'état (vert pour en ligne, rouge pour hors ligne)
                    TextBlock statusEmoji = new TextBlock
                    {
                        Text = room.IsOnline ? "🟢" : "🔴",
                        Margin = new Thickness(10, 0, 0, 0)
                    };
                    roomPanel.Children.Add(statusEmoji);

                    // Ajouter le panel au TreeViewItem
                    TreeViewItem roomItem = new TreeViewItem { Header = roomPanel };
                    floorItem.Items.Add(roomItem);
                }

                treeViewDetails.Items.Add(floorItem);
            }
        }
    }

    // Modèle pour les étages avec une liste de salles
    public class Floor
    {
        public string Name { get; set; }
        public List<Room> Rooms { get; set; }
    }

    // Modèle pour les salles avec un nom et un état en ligne
    public class Room
    {
        public string Name { get; set; }
        public bool IsOnline { get; set; }
    }
}
