namespace bothtech.Shared.Models
{
    public class Loan
    {
        public string FirebaseId { get; set; }
        public string AgentId { get; set; }
        public string AgentName { get; set; }
        public double MontantPretNet { get; set; }
        public double Interet { get; set; }
        public double Tva { get; set; }
        public double MontantInitial { get; set; }
        public double MontantRestant { get; set; }
        public int NbMois { get; set; }
        public double ValeurRetenue { get; set; }
        public string Motif { get; set; }
        public string Status { get; set; }
        public long CreatedAt { get; set; }
    }
}