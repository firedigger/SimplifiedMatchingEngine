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
    public void ReduceQuantity(int quantity)
    {
        if (quantity > RemainingQuantity)
        {
            throw new ArgumentException("Quantity to reduce exceeds remaining quantity.", nameof(quantity));
        }
        RemainingQuantity -= quantity;
        Status = RemainingQuantity == 0 ? OrderStatus.Filled : OrderStatus.PartiallyFilled;
    }
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