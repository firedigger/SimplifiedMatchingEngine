using System.Text;

namespace SimplifiedMatchingEngine;

/// <summary>
/// Concurrent order book
/// </summary>
public class OrderMatchingService
{
    private readonly SortedDictionary<decimal, LinkedList<Order>> _buyOrders = []; //TODO: make concurrent
    private readonly SortedDictionary<decimal, LinkedList<Order>> _sellOrders = [];
    private readonly IList<Trade> _tradingHistory = [];
    public void PlaceOrder(Order order)
    {
        if (MatchOrder(order))
        {
            return;
        }
        var dictionary = order.Side == OrderSide.Buy ? _buyOrders : _sellOrders;
        if (!dictionary.TryGetValue(order.Price, out var list))
        {
            list = new LinkedList<Order>();
            dictionary[order.Price] = list;
        }
        list.AddLast(order);
    }
    public void CancelOrder(Order order)
    {
        if (order.Status == OrderStatus.Filled || order.Status == OrderStatus.Canceled)
        {
            throw new InvalidOperationException("Cannot cancel a filled or already canceled order.");
        }
        //TODO: lock
        var dictionary = order.Side == OrderSide.Buy ? _buyOrders : _sellOrders;
        dictionary[order.Price].Remove(order);
        if (dictionary[order.Price].Count == 0)
        {
            dictionary.Remove(order.Price);
        }
        order.Status = OrderStatus.Canceled;
    }
    public decimal? GetBestBuyPrice() => _buyOrders.Count > 0 ? _buyOrders.Last().Key : null;
    public decimal? GetBestSellPrice() => _sellOrders.Count > 0 ? _sellOrders.First().Key : null;

    private bool MatchOrder(Order order)
    {
        var orders = order.Side == OrderSide.Buy ? _sellOrders : _buyOrders;
        var bestPrice = order.Side == OrderSide.Buy ? GetBestSellPrice() : GetBestBuyPrice();
        while (bestPrice is not null && (order.Side == OrderSide.Buy && order.Price >= bestPrice || order.Side == OrderSide.Sell && order.Price <= bestPrice))
        {
            var matchedOrder = orders[bestPrice.Value].First.Value;
            if (matchedOrder.RemainingQuantity >= order.RemainingQuantity)
            {
                matchedOrder.RemainingQuantity -= order.RemainingQuantity;
                if (matchedOrder.RemainingQuantity == 0)
                {
                    matchedOrder.Status = OrderStatus.Filled;
                    orders[bestPrice.Value].RemoveFirst();
                    if (orders[bestPrice.Value].Count == 0)
                    {
                        orders.Remove(bestPrice.Value);
                    }
                }
                else
                {
                    matchedOrder.Status = OrderStatus.PartiallyFilled;
                }
                order.Status = OrderStatus.Filled;
                _tradingHistory.Add(new Trade { Price = order.Price, Quantity = order.RemainingQuantity });
                order.RemainingQuantity = 0;
                return true;
            }
            else
            {
                order.Status = OrderStatus.PartiallyFilled;
                order.RemainingQuantity -= matchedOrder.RemainingQuantity;
                matchedOrder.Status = OrderStatus.Filled;
                _tradingHistory.Add(new Trade { Price = matchedOrder.Price, Quantity = matchedOrder.RemainingQuantity });
                orders[bestPrice.Value].RemoveFirst();
                if (orders[bestPrice.Value].Count == 0)
                {
                    orders.Remove(bestPrice.Value);
                    bestPrice = GetBestSellPrice();
                }
            }
        }
        return false;
    }

    public string PrintOrderBook()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Buy Orders:");
        foreach (var queue in _buyOrders)
            foreach (var order in queue.Value)
                sb.AppendLine($"Price: {order.Price}, Quantity: {order.RemainingQuantity}");
        sb.AppendLine("Sell Orders:");
        foreach (var queue in _sellOrders)
            foreach (var order in queue.Value)
                sb.AppendLine($"Price: {order.Price}, Quantity: {order.RemainingQuantity}");
        return sb.ToString();
    }

    public string PrintTradingHistory()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Trading History:");
        foreach (var trade in _tradingHistory)
            sb.AppendLine($"Price: {trade.Price}, Quantity: {trade.Quantity}");
        return sb.ToString();
    }
}