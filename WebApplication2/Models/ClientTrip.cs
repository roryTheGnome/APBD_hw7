namespace WebApplication2.Models;

public class ClientTrip
{
    public int IdTrip { get; set; }
    public string TripName { get; set; } = null!;//maybe if i put trip object it would be easier??
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string Description { get; set; } = null!;
    public DateTime RegisteredAt { get; set; }
    public DateTime? PaymentDate { get; set; }
}