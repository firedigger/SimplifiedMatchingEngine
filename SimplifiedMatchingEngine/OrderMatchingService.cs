using SimplifiedMatchingEngine.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace SimplifiedMatchingEngine;

public sealed class OrderMatchingService : IDisposable
{
    public void PlaceOrder(Order order)
    {
        if (order.Price <= 0)
        {
            throw new ArgumentException("Price must be greater than zero.", nameof(order));
        }
        if (order.RemainingQuantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(order));
        }
        MatchOrder(order);
    }

    public void CancelOrder(Order order)
    {
        if (order.Status == OrderStatus.Filled || order.Status == OrderStatus.Canceled)
        {
            throw new InvalidOperationException("Cannot cancel a filled or already canceled order.");
        }
        using (_lock.EnterWriteScope())
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
        using var _ = _lock.EnterReadScope();
        var dictionary = side == OrderSide.Buy ? _sellOrders : _buyOrders;
        return dictionary.Count > 0 ? dictionary.First().Key : null;
    }

    public string GetOrderBookText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Buy Orders:");
        using (_lock.EnterReadScope())
        {
            foreach (var queue in _buyOrders)
                sb.AppendLine($"Price: {queue.Key}, Quantity: {queue.Value.Sum(o => o.RemainingQuantity)}");
            sb.AppendLine("Sell Orders:");
            foreach (var queue in _sellOrders)
                sb.AppendLine($"Price: {queue.Key}, Quantity: {queue.Value.Sum(o => o.RemainingQuantity)}");
        }
        return sb.ToString();
    }

    public string GetTradingHistoryText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Trading History:");
        foreach (var trade in _tradingHistory)
            sb.AppendLine($"Price: {trade.Price}, Quantity: {trade.Quantity}");
        return sb.ToString();
    }

    /// <summary>
    /// Attempts to match the given order with existing orders in the order book.
    /// If a match is found, the orders are updated to reflect the match.
    /// </summary>
    /// <param name="order">The order to match</param>
    /// <returns>Whether the <paramref name="order"/> was filled</returns>
    private void MatchOrder(Order order)
    {
        var orders = order.Side == OrderSide.Buy ? _sellOrders : _buyOrders;
        using var _ = _lock.EnterWriteScope();
        var bestPrice = GetBestPrice(order.Side);
        while (bestPrice is not null && (order.Side == OrderSide.Buy && order.Price >= bestPrice || order.Side == OrderSide.Sell && order.Price <= bestPrice))
        {
            var matchedOrder = orders[bestPrice.Value].First;
            if (matchedOrder is not null && MatchOrders(order, matchedOrder.Value))
            {
                return;
            }
            bestPrice = GetBestPrice(order.Side);
        }
        // If the order was not filled, add it to the order book
        var dictionary = order.Side == OrderSide.Buy ? _buyOrders : _sellOrders;
        if (!dictionary.TryGetValue(order.Price, out var list))
        {
            list = new LinkedList<Order>();
            dictionary[order.Price] = list;
        }
        list.AddLast(order);
    }

    /// <summary>
    /// Matches the given orders and updates their quantities accordingly.
    /// If the <paramref name="matchedOrder"/> is fully filled, it is removed from the order book.
    /// </summary>
    /// <param name="placedOrder"></param>
    /// <param name="matchedOrder"></param>
    /// <returns>Whether the <paramref name="placedOrder"/> was filled</returns>
    private bool MatchOrders(Order placedOrder, Order matchedOrder)
    {
        Debug.Assert(placedOrder.Side != matchedOrder.Side, "Orders must be of different sides to match.");
        var orders = placedOrder.Side == OrderSide.Buy ? _sellOrders : _buyOrders;
        var quantity = Math.Min(placedOrder.RemainingQuantity, matchedOrder.RemainingQuantity);
        _tradingHistory.Enqueue(new Trade { Price = matchedOrder.Price, Quantity = quantity });
        matchedOrder.ReduceQuantity(quantity);
        placedOrder.ReduceQuantity(quantity);
        if (matchedOrder.RemainingQuantity == 0)
        {
            orders[matchedOrder.Price].RemoveFirst();
            if (orders[matchedOrder.Price].Count == 0)
            {
                orders.Remove(matchedOrder.Price);
            }
        }
        return placedOrder.RemainingQuantity == 0;
    }

    public void Dispose()
    {
        _lock.Dispose();
    }

    private readonly SortedDictionary<decimal, LinkedList<Order>> _buyOrders = new(Comparer<decimal>.Create((a, b) => b.CompareTo(a)));
    private readonly SortedDictionary<decimal, LinkedList<Order>> _sellOrders = [];
    private readonly ConcurrentQueue<Trade> _tradingHistory = [];
    private readonly ReaderWriterLockSlim _lock = new();
}