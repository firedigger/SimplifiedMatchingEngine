using SimplifiedMatchingEngine;
using SimplifiedMatchingEngine.Models;

var orderBook = new OrderMatchingService();
orderBook.PlaceOrder(new Order(OrderSide.Buy, 100, 10));
Console.WriteLine(orderBook.GetOrderBookText());
orderBook.PlaceOrder(new Order(OrderSide.Sell, 99, 11));
Console.WriteLine(orderBook.GetOrderBookText());
Console.WriteLine(orderBook.GetTradingHistoryText());