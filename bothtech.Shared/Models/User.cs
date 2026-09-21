using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bothtech.Shared.Models;

public class User
{
    public int Id { get; set; } // ID local MySQL
    public string FirebaseUid { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public string Role { get; set; }
    public bool Active { get; set; }
    public long CreatedAt { get; set; }
    public string Matricule { get; set; }
    public string Sexe { get; set; }
    public string Situation { get; set; }
    public int NbCharges { get; set; }
    public string ContractType { get; set; } // cdi, cdd, prestataire
    public int ContractDuration { get; set; } // Pour le CDD
    public string Categorie { get; set; } // cadre, non_cadre
    public int BaseSalary { get; set; }
    public int CommissionPct { get; set; }
    // (Ajoutez les autres champs comme Iuts, Cnss, etc. selon vos besoins)
}
