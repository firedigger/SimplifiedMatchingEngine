using System.Collections.Concurrent;

namespace SimplifiedMatchingEngine.Tests
{
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
        public void OrderFulfilmentOrder()
        {
            var orderMatchingService = new OrderMatchingService();
            var sellOrder1 = new Order(OrderSide.Sell, 100, 5);
            orderMatchingService.PlaceOrder(sellOrder1);
            var sellOrder2 = new Order(OrderSide.Sell, 100, 5);
            orderMatchingService.PlaceOrder(sellOrder2);
            var sellOrder3 = new Order(OrderSide.Sell, 100, 5);
            orderMatchingService.PlaceOrder(sellOrder3);
            orderMatchingService.PlaceOrder(new Order(OrderSide.Buy, 100, 6));
            Assert.AreEqual(OrderStatus.Filled, sellOrder1.Status);
            Assert.AreEqual(OrderStatus.PartiallyFilled, sellOrder2.Status);
            Assert.AreEqual(OrderStatus.New, sellOrder3.Status);
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

        }
    }
}
