using System;

namespace bothtech.Shared.Models
{
    public class StaffMemberModel
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string Matricule { get; set; } = "";
        public string Name { get; set; } = "";
        public string Sexe { get; set; } = "Masculin";
        public string Situation { get; set; } = "Célibataire";
        public DateTime BirthDate { get; set; } = DateTime.Today;
        public string BirthPlace { get; set; } = "";
        public int NbCharges { get; set; } = 0;
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string EmergencyName { get; set; } = "";
        public string EmergencyPhone { get; set; } = "";
        public string Role { get; set; } = "commercial";
        public string ContractType { get; set; } = "cdi";
        public decimal BaseSalary { get; set; } = 0;
        public decimal CommissionPct { get; set; } = 0;
        public decimal Indemnite { get; set; } = 0;
        public decimal AllocFamiliale { get; set; } = 0;
        public decimal Iuts { get; set; } = 0;
        public decimal Cnss { get; set; } = 5.5m;
        public bool Active { get; set; } = true;
    }
}