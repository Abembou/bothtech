using SQLite;
using System;
using System.Collections.Generic;

namespace bothtech.Shared.Models;

public class OrderModel
{
    public string Id { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string RecipientName { get; set; } = "";
    public string Status { get; set; } = "nouveau";
    public string Type { get; set; } = "";
    public string TraitePar { get; set; } = "";
    public decimal Total { get; set; }
    public decimal Reste { get; set; }
    public long CreatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // DOIT être un string pour correspondre au nouveau Order.Id
    public string OrderId { get; set; } = string.Empty;

    // DOIT être un string pour correspondre au nouveau Product.Id
    // (Conservez ce nom de variable car il est utilisé dans DatabaseService)
    public string ProductFirebaseId { get; set; } = string.Empty;

    public string Name
    {
        get => ProductName;
        set => ProductName = value;
    }

    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public int UnitPrice { get; set; }

    // ✅ L'alias ajouté pour corriger l'erreur dans MainLayout.razor
    public decimal Price
    {
        get => (decimal)UnitPrice;
        set => UnitPrice = (int)value;
    }

    // Total calculé automatiquement
    public int TotalPrice => Quantity * UnitPrice;
}