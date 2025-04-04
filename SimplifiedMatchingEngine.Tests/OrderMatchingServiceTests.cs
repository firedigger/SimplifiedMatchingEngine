using SimplifiedMatchingEngine.Models;
using System.Collections.Concurrent;

namespace SimplifiedMatchingEngine.Tests;

[TestClass]
public sealed class OrderMatchingServiceTests
{
#pragma warning disable CS8618
    private OrderMatchingService _orderMatchingService;
#pragma warning restore CS8618

    [TestInitialize]
    public void Init()
    {
        _orderMatchingService = new OrderMatchingService();
    }

    [TestMethod]
    public void SingleOrder()
    {
        var order = new Order(OrderSide.Buy, 100, 10);
        _orderMatchingService.PlaceOrder(order);
        Assert.AreEqual(10, order.RemainingQuantity);
        Assert.AreEqual(OrderStatus.New, order.Status);
    }

    [TestMethod]
    public void TwoMatchedFilledOrders()
    {
        var buyOrder = new Order(OrderSide.Buy, 100, 10);
        var sellOrder = new Order(OrderSide.Sell, 100, 10);
        _orderMatchingService.PlaceOrder(buyOrder);
        _orderMatchingService.PlaceOrder(sellOrder);
        Assert.AreEqual(0, buyOrder.RemainingQuantity);
        Assert.AreEqual(OrderStatus.Filled, buyOrder.Status);
        Assert.AreEqual(0, sellOrder.RemainingQuantity);
        Assert.AreEqual(OrderStatus.Filled, sellOrder.Status);
        Assert.IsNull(_orderMatchingService.GetBestPrice(OrderSide.Buy));
        Assert.IsNull(_orderMatchingService.GetBestPrice(OrderSide.Sell));
    }

    [TestMethod]
    public void TwoNotMatchedOrders()
    {
        var buyOrder = new Order(OrderSide.Buy, 100, 10);
        var sellOrder = new Order(OrderSide.Sell, 101, 10);
        _orderMatchingService.PlaceOrder(buyOrder);
        _orderMatchingService.PlaceOrder(sellOrder);
        Assert.AreEqual(10, buyOrder.RemainingQuantity);
        Assert.AreEqual(OrderStatus.New, buyOrder.Status);
        Assert.AreEqual(10, sellOrder.RemainingQuantity);
        Assert.AreEqual(OrderStatus.New, sellOrder.Status);
        Assert.IsNotNull(_orderMatchingService.GetBestPrice(OrderSide.Buy));
        Assert.IsNotNull(_orderMatchingService.GetBestPrice(OrderSide.Sell));
    }

    [TestMethod]
    public void TwoMatchedPartiallyFilledOrders()
    {
        var buyOrder = new Order(OrderSide.Buy, 100, 10);
        var sellOrder = new Order(OrderSide.Sell, 100, 11);
        _orderMatchingService.PlaceOrder(buyOrder);
        _orderMatchingService.PlaceOrder(sellOrder);
        Assert.AreEqual(0, buyOrder.RemainingQuantity);
        Assert.AreEqual(OrderStatus.Filled, buyOrder.Status);
        Assert.AreEqual(1, sellOrder.RemainingQuantity);
        Assert.AreEqual(OrderStatus.PartiallyFilled, sellOrder.Status);
        Assert.AreEqual(100, _orderMatchingService.GetBestPrice(OrderSide.Buy));
        Assert.IsNull(_orderMatchingService.GetBestPrice(OrderSide.Sell));
    }

    [TestMethod]
    public void MatchByBestPrice()
    {
        var buyOrder1 = new Order(OrderSide.Buy, 100, 10);
        _orderMatchingService.PlaceOrder(buyOrder1);
        var buyOrder2 = new Order(OrderSide.Buy, 150, 5);
        _orderMatchingService.PlaceOrder(buyOrder2);
        var sellOrder = new Order(OrderSide.Sell, 100, 10);
        _orderMatchingService.PlaceOrder(sellOrder);
        Assert.AreEqual(OrderStatus.Filled, sellOrder.Status);
        Assert.AreEqual(OrderStatus.Filled, buyOrder2.Status);
        Assert.AreEqual(OrderStatus.PartiallyFilled, buyOrder1.Status);
        Assert.AreEqual(5, buyOrder1.RemainingQuantity);
    }

    [TestMethod]
    public void BestPriceToSell()
    {
        _orderMatchingService.PlaceOrder(new Order(OrderSide.Buy, 100, 10));
        _orderMatchingService.PlaceOrder(new Order(OrderSide.Buy, 101, 10));
        Assert.AreEqual(101, _orderMatchingService.GetBestPrice(OrderSide.Sell));
    }

    [TestMethod]
    public void BestPriceToBuy()
    {
        _orderMatchingService.PlaceOrder(new Order(OrderSide.Sell, 100, 10));
        _orderMatchingService.PlaceOrder(new Order(OrderSide.Sell, 101, 10));
        Assert.AreEqual(100, _orderMatchingService.GetBestPrice(OrderSide.Buy));
    }

    [TestMethod]
    public void StatusTransition()
    {
        var order = new Order(OrderSide.Buy, 100, 10);
        _orderMatchingService.PlaceOrder(order);
        Assert.AreEqual(OrderStatus.New, order.Status);
        _orderMatchingService.PlaceOrder(new Order(OrderSide.Sell, 100, 5));
        Assert.AreEqual(OrderStatus.PartiallyFilled, order.Status);
        _orderMatchingService.PlaceOrder(new Order(OrderSide.Sell, 100, 5));
        Assert.AreEqual(OrderStatus.Filled, order.Status);
    }

    [TestMethod]
    public void CancelOrder()
    {
        var order = new Order(OrderSide.Buy, 100, 10);
        _orderMatchingService.PlaceOrder(order);
        Assert.AreEqual(OrderStatus.New, order.Status);
        _orderMatchingService.PlaceOrder(new Order(OrderSide.Sell, 100, 5));
        Assert.AreEqual(OrderStatus.PartiallyFilled, order.Status);
        _orderMatchingService.CancelOrder(order);
        Assert.IsNull(_orderMatchingService.GetBestPrice(OrderSide.Sell));
        Assert.AreEqual(OrderStatus.Canceled, order.Status);
    }

    [TestMethod]
    public void MatchFIFO()
    {
        var sellOrder1 = new Order(OrderSide.Sell, 100, 5);
        _orderMatchingService.PlaceOrder(sellOrder1);
        var sellOrder2 = new Order(OrderSide.Sell, 100, 5);
        _orderMatchingService.PlaceOrder(sellOrder2);
        var sellOrder3 = new Order(OrderSide.Sell, 100, 5);
        _orderMatchingService.PlaceOrder(sellOrder3);
        var buyOrder = new Order(OrderSide.Buy, 100, 6);
        _orderMatchingService.PlaceOrder(buyOrder);
        Assert.AreEqual(OrderStatus.Filled, sellOrder1.Status);
        Assert.AreEqual(0, sellOrder1.RemainingQuantity);
        Assert.AreEqual(OrderStatus.PartiallyFilled, sellOrder2.Status);
        Assert.AreEqual(4, sellOrder2.RemainingQuantity);
        Assert.AreEqual(OrderStatus.New, sellOrder3.Status);
        Assert.AreEqual(5, sellOrder3.RemainingQuantity);
        Assert.AreEqual(OrderStatus.Filled, buyOrder.Status);
        Assert.AreEqual(0, buyOrder.RemainingQuantity);
        Assert.IsNull(_orderMatchingService.GetBestPrice(OrderSide.Sell));
        Assert.AreEqual(100, _orderMatchingService.GetBestPrice(OrderSide.Buy));
    }

    [TestMethod]
    public void Concurrency()
    {
        const int count = 20;
        var bag = new ConcurrentBag<Order>();
        Parallel.For(0, count * 2, i =>
        {
            var order = new Order(i < count ? OrderSide.Buy : OrderSide.Sell, 100, 10);
            bag.Add(order);
            _orderMatchingService.PlaceOrder(order);
        });
        Assert.IsNull(_orderMatchingService.GetBestPrice(OrderSide.Buy));
        Assert.IsNull(_orderMatchingService.GetBestPrice(OrderSide.Sell));
        Assert.IsTrue(bag.All(o => o.Status == OrderStatus.Filled));
    }

    [TestMethod]
    public void ConcurrenctPlaceAndCancel()
    {
        const int count = 200;
        const int baseQuantity = 10;
        var order = new Order(OrderSide.Buy, 100, count * baseQuantity);
        var rng = new Random(369);
        _orderMatchingService.PlaceOrder(order);
        var orders = new ConcurrentBag<Order>();
        Parallel.For(0, count * 2, i =>
        {
            var order = new Order(OrderSide.Sell, 100, baseQuantity);
            orders.Add(order);
            _orderMatchingService.PlaceOrder(order);
            // 50% chance to cancel the order
            if (rng.NextDouble() < 0.5)
            {
                if (order.Status == OrderStatus.New)
                    _orderMatchingService.CancelOrder(order);
            }
        });
        var processedOrders = orders.Where(o => o.Status == OrderStatus.Filled).ToList();
        Assert.IsTrue(processedOrders.Count > 0);
        var cancelledOrders = orders.Where(o => o.Status == OrderStatus.Canceled).ToList();
        Assert.IsTrue(cancelledOrders.Count > 0);
        Assert.AreEqual(count * baseQuantity - order.RemainingQuantity, processedOrders.Count * baseQuantity);
    }
}
