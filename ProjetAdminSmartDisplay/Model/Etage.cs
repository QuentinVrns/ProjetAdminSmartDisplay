using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetAdminSmartDisplay.Model
{
    public class Etage
    {
        public int Id { get; set; }
        public string NomEtage { get; set; }
        public int BatimentId { get; set; }
    }
}
