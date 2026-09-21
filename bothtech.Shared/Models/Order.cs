using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bothtech.Shared.Models;

public class Order
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string FirebaseId { get; set; }
    public int Total { get; set; }
    public string Status { get; set; }
    public string Type { get; set; }
    public long CreatedAt { get; set; }
    public string SyncStatus { get; set; }

  
  
    public string ClientId { get; set; }
    public string ClientName { get; set; }
    public string ClientPhone { get; set; }
    public string ClientAddress { get; set; }
 
    public string PaymentMethod { get; set; }
    public string TraitePar { get; set; }

    public int Avance { get; set; }
    public int Reste { get; set; }
    public string RecipientName { get; set; }
    public string RecipientPhone { get; set; }
    public string RecipientAddress { get; set; }
    public string VenteId { get; set; } // ID de la vente générée lors de la conclusion

    [Ignore] // Empêche SQLite d'essayer de créer une colonne "Items"
    public List<OrderItem> Items { get; set; } = new List<OrderItem>();
}
