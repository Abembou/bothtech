using SQLite;
using System;

namespace bothtech.Shared.Models
{
    public class ExpenseModel
    {
        [PrimaryKey]
        public string Id { get; set; } = "";
        public string Category { get; set; } = "";
        public decimal Amount { get; set; }
        public long Date { get; set; }
        public string Beneficiary { get; set; } = "";
        public string Reason { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public string AgentId { get; set; } = "";
    }
}