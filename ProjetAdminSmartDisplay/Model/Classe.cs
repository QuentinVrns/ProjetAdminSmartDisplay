using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetAdminSmartDisplay.Model
{
    public class Classe
    {
        public int Id { get; set; }
        public string NomSalle { get; set; }
        public int EtageId { get; set; }

        // Propriété pour savoir si la salle est sélectionnée
        public bool IsSelected { get; set; } = false;
    }
}
