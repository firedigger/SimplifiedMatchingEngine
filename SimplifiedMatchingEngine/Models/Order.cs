namespace SimplifiedMatchingEngine.Models;

public record Order
{
    public Order(OrderSide side, decimal price, int quantity)
    {
        Side = side;
        Price = price;
        RemainingQuantity = quantity;
        Status = OrderStatus.New;
    }

    public decimal Price { get; set; }
    public int RemainingQuantity { get; set; }
    public OrderStatus Status { get; set; }
    public OrderSide Side { get; set; }
    public override string ToString()
    {
        return $"Order: {Side}, Price: {Price}, Remaining Quantity: {RemainingQuantity}, Status: {Status}";
    }
}

public enum OrderStatus
{
    New,
    PartiallyFilled,
    Filled,
    Canceled
}

public enum OrderSide
{
    Buy,
    Sell
}