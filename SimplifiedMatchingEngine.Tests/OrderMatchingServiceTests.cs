using SimplifiedMatchingEngine.Models;
using System.Collections.Concurrent;

namespace SimplifiedMatchingEngine.Tests;

[TestClass]
public sealed class OrderMatchingServiceTests
{
    [TestMethod]
    public void SingleOrder()
    {
        var orderMatchingService = new OrderMatchingService();
        var order = new Order(OrderSide.Buy, 100, 10);
        orderMatchingService.PlaceOrder(order);
        Assert.AreEqual(10, order.RemainingQuantity);
        Assert.AreEqual(OrderStatus.New, order.Status);
    }

    [TestMethod]
    public void TwoMatchedOrders()
    {
        var orderMatchingService = new OrderMatchingService();
        var buyOrder = new Order(OrderSide.Buy, 100, 10);
        var sellOrder = new Order(OrderSide.Sell, 100, 10);
        orderMatchingService.PlaceOrder(buyOrder);
        orderMatchingService.PlaceOrder(sellOrder);
        Assert.AreEqual(0, buyOrder.RemainingQuantity);
        Assert.AreEqual(OrderStatus.Filled, buyOrder.Status);
        Assert.AreEqual(0, sellOrder.RemainingQuantity);
        Assert.AreEqual(OrderStatus.Filled, sellOrder.Status);
        Assert.IsNull(orderMatchingService.GetBestPrice(OrderSide.Buy));
        Assert.IsNull(orderMatchingService.GetBestPrice(OrderSide.Sell));
    }

    [TestMethod]
    public void MatchByBestPrice()
    {
        var orderMatchingService = new OrderMatchingService();
        var buyOrder1 = new Order(OrderSide.Buy, 100, 10);
        orderMatchingService.PlaceOrder(buyOrder1);
        var buyOrder2 = new Order(OrderSide.Buy, 150, 5);
        orderMatchingService.PlaceOrder(buyOrder2);
        var sellOrder = new Order(OrderSide.Sell, 100, 10);
        orderMatchingService.PlaceOrder(sellOrder);
        Assert.AreEqual(OrderStatus.Filled, sellOrder.Status);
        Assert.AreEqual(OrderStatus.Filled, buyOrder2.Status);
        Assert.AreEqual(OrderStatus.PartiallyFilled, buyOrder1.Status);
        Assert.AreEqual(5, buyOrder1.RemainingQuantity);
    }

    [TestMethod]
    public void BestPriceToSell()
    {
        var orderMatchingService = new OrderMatchingService();
        orderMatchingService.PlaceOrder(new Order(OrderSide.Buy, 100, 10));
        orderMatchingService.PlaceOrder(new Order(OrderSide.Buy, 101, 10));
        Assert.AreEqual(101, orderMatchingService.GetBestPrice(OrderSide.Sell));
    }

    [TestMethod]
    public void BestPriceToBuy()
    {
        var orderMatchingService = new OrderMatchingService();
        orderMatchingService.PlaceOrder(new Order(OrderSide.Sell, 100, 10));
        orderMatchingService.PlaceOrder(new Order(OrderSide.Sell, 101, 10));
        Assert.AreEqual(100, orderMatchingService.GetBestPrice(OrderSide.Buy));
    }

    [TestMethod]
    public void StatusTransition()
    {
        var orderMatchingService = new OrderMatchingService();
        var order = new Order(OrderSide.Buy, 100, 10);
        orderMatchingService.PlaceOrder(order);
        Assert.AreEqual(OrderStatus.New, order.Status);
        orderMatchingService.PlaceOrder(new Order(OrderSide.Sell, 100, 5));
        Assert.AreEqual(OrderStatus.PartiallyFilled, order.Status);
        orderMatchingService.PlaceOrder(new Order(OrderSide.Sell, 100, 5));
        Assert.AreEqual(OrderStatus.Filled, order.Status);
    }

    [TestMethod]
    public void CancelOrder()
    {
        var orderMatchingService = new OrderMatchingService();
        var order = new Order(OrderSide.Buy, 100, 10);
        orderMatchingService.PlaceOrder(order);
        Assert.AreEqual(OrderStatus.New, order.Status);
        orderMatchingService.PlaceOrder(new Order(OrderSide.Sell, 100, 5));
        Assert.AreEqual(OrderStatus.PartiallyFilled, order.Status);
        orderMatchingService.CancelOrder(order);
        Assert.IsNull(orderMatchingService.GetBestPrice(OrderSide.Sell));
        Assert.AreEqual(OrderStatus.Canceled, order.Status);
    }

    [TestMethod]
    public void MatchFIFO()
    {
        var orderMatchingService = new OrderMatchingService();
        var sellOrder1 = new Order(OrderSide.Sell, 100, 5);
        orderMatchingService.PlaceOrder(sellOrder1);
        var sellOrder2 = new Order(OrderSide.Sell, 100, 5);
        orderMatchingService.PlaceOrder(sellOrder2);
        var sellOrder3 = new Order(OrderSide.Sell, 100, 5);
        orderMatchingService.PlaceOrder(sellOrder3);
        var buyOrder = new Order(OrderSide.Buy, 100, 6);
        orderMatchingService.PlaceOrder(buyOrder);
        Assert.AreEqual(OrderStatus.Filled, sellOrder1.Status);
        Assert.AreEqual(0, sellOrder1.RemainingQuantity);
        Assert.AreEqual(OrderStatus.PartiallyFilled, sellOrder2.Status);
        Assert.AreEqual(4, sellOrder2.RemainingQuantity);
        Assert.AreEqual(OrderStatus.New, sellOrder3.Status);
        Assert.AreEqual(5, sellOrder3.RemainingQuantity);
        Assert.AreEqual(OrderStatus.Filled, buyOrder.Status);
        Assert.AreEqual(0, buyOrder.RemainingQuantity);
        Assert.IsNull(orderMatchingService.GetBestPrice(OrderSide.Sell));
        Assert.AreEqual(100, orderMatchingService.GetBestPrice(OrderSide.Buy));
    }

    [TestMethod]
    public void Concurrency()
    {
        var orderMatchingService = new OrderMatchingService();
        const int count = 20;
        var bag = new ConcurrentBag<Order>();
        Parallel.For(0, count * 2, i =>
        {
            var order = new Order(i < count ? OrderSide.Buy : OrderSide.Sell, 100, 10);
            bag.Add(order);
            orderMatchingService.PlaceOrder(order);
        });
        Assert.IsNull(orderMatchingService.GetBestPrice(OrderSide.Buy));
        Assert.IsNull(orderMatchingService.GetBestPrice(OrderSide.Sell));
        Assert.IsTrue(bag.All(o => o.Status == OrderStatus.Filled));
    }

    [TestMethod]
    public void ConcurrenctPlaceAndCancel()
    {
        var orderMatchingService = new OrderMatchingService();
        const int count = 200;
        const int baseQuantity = 10;
        var order = new Order(OrderSide.Buy, 100, count * baseQuantity);
        var rng = new Random(369);
        orderMatchingService.PlaceOrder(order);
        var orders = new ConcurrentBag<Order>();
        Parallel.For(0, count * 2, i =>
        {
            var order = new Order(OrderSide.Sell, 100, baseQuantity);
            orders.Add(order);
            orderMatchingService.PlaceOrder(order);
            // 50% chance to cancel the order
            if (rng.NextDouble() < 0.5)
            {
                if (order.Status == OrderStatus.New)
                    orderMatchingService.CancelOrder(order);
            }
        });
        var processedOrders = orders.Where(o => o.Status == OrderStatus.Filled).ToList();
        Assert.IsTrue(processedOrders.Count > 0);
        var cancelledOrders = orders.Where(o => o.Status == OrderStatus.Canceled).ToList();
        Assert.IsTrue(cancelledOrders.Count > 0);
        Assert.AreEqual(count * baseQuantity - order.RemainingQuantity, processedOrders.Count * baseQuantity);
    }
}
