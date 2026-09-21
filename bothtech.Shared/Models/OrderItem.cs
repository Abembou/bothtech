using SQLite;
using System;

namespace bothtech.Shared.Models;

public class OrderItem
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // DOIT être un string pour correspondre au nouveau Order.Id
    public string OrderId { get; set; } = string.Empty;

    // DOIT être un string pour correspondre au nouveau Product.Id
    // (Conservez ce nom de variable car il est utilisé dans DatabaseService)
    public string ProductFirebaseId { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int UnitPrice { get; set; }

    // Total calculé automatiquement
    public int TotalPrice => Quantity * UnitPrice;
}