using System.Collections.Concurrent;
using System.Text;

namespace SimplifiedMatchingEngine;

/// <summary>
/// Concurrent order book
/// </summary>
public sealed class OrderMatchingService
{
    private readonly SortedDictionary<decimal, LinkedList<Order>> _buyOrders = new(Comparer<decimal>.Create((a, b) => b.CompareTo(a)));
    private readonly SortedDictionary<decimal, LinkedList<Order>> _sellOrders = [];
    private readonly ConcurrentQueue<Trade> _tradingHistory = [];
    private readonly Lock _lock = new();

    public void PlaceOrder(Order order)
    {
        if (MatchOrder(order))
        {
            return;
        }
        using (_lock.EnterScope())
        {
            var dictionary = order.Side == OrderSide.Buy ? _buyOrders : _sellOrders;
            if (!dictionary.TryGetValue(order.Price, out var list))
            {
                list = new LinkedList<Order>();
                dictionary[order.Price] = list;
            }
            list.AddLast(order);
        }
    }
    public void CancelOrder(Order order)
    {
        if (order.Status == OrderStatus.Filled || order.Status == OrderStatus.Canceled)
        {
            throw new InvalidOperationException("Cannot cancel a filled or already canceled order.");
        }
        using (_lock.EnterScope())
        {
            var dictionary = order.Side == OrderSide.Buy ? _buyOrders : _sellOrders;
            dictionary[order.Price].Remove(order);
            if (dictionary[order.Price].Count == 0)
            {
                dictionary.Remove(order.Price);
            }
        }
        order.Status = OrderStatus.Canceled;
    }

    public decimal? GetBestPrice(OrderSide side)
    {
        using var _ = _lock.EnterScope();
        var dictionary = side == OrderSide.Buy ? _sellOrders : _buyOrders;
        return dictionary.Count > 0 ? dictionary.First().Key : null;
    }

    private bool MatchOrder(Order order)
    {
        var orders = order.Side == OrderSide.Buy ? _sellOrders : _buyOrders;
        using var scope = _lock.EnterScope();
        var bestPrice = GetBestPrice(order.Side);
        while (bestPrice is not null && (order.Side == OrderSide.Buy && order.Price >= bestPrice || order.Side == OrderSide.Sell && order.Price <= bestPrice))
        {
            var matchedOrder = orders[bestPrice.Value].First!.Value;
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
                _tradingHistory.Enqueue(new Trade { Price = order.Price, Quantity = order.RemainingQuantity });
                order.RemainingQuantity = 0;
                return true;
            }
            else
            {
                order.Status = OrderStatus.PartiallyFilled;
                order.RemainingQuantity -= matchedOrder.RemainingQuantity;
                matchedOrder.Status = OrderStatus.Filled;
                matchedOrder.RemainingQuantity = 0;
                _tradingHistory.Enqueue(new Trade { Price = matchedOrder.Price, Quantity = matchedOrder.RemainingQuantity });
                orders[bestPrice.Value].RemoveFirst();
                if (orders[bestPrice.Value].Count == 0)
                {
                    orders.Remove(bestPrice.Value);
                    bestPrice = GetBestPrice(order.Side);
                }
            }
        }
        return false;
    }

    public string PrintOrderBook()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Buy Orders:");
        using (_lock.EnterScope())
        {
            foreach (var queue in _buyOrders)
                sb.AppendLine($"Price: {queue.Key}, Quantity: {queue.Value.Sum(o => o.RemainingQuantity)}");
            sb.AppendLine("Sell Orders:");
            foreach (var queue in _sellOrders)
                sb.AppendLine($"Price: {queue.Key}, Quantity: {queue.Value.Sum(o => o.RemainingQuantity)}");
        }
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