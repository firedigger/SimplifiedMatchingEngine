# Simplified Matching Engine

A lightweight, concurrent in-memory order matching engine written in C#.

---

## Project Structure

This project implements a simplified concurrent order matching engine using pure in-memory structures. The layout is designed for clarity and separation of concerns:

```
/Models
  Plain object classes such as Order.cs and Trade.cs
OrderMatchingService.cs // Core service responsible for placing, canceling, and matching orders
```

### `OrderMatchingService`
- Maintains internal `SortedDictionary<decimal, LinkedList<Order>>` for buy/sell sides.
- `SortedDictionary` orders the prices in a convenient way for upkeeping the best price in the begining of the dictionary
- `LinkedList` is chosen for efficient order removal
- Trades are collected in a `ConcurrentQueue` as only addition and iteration is required
- Uses .NET 9 `Lock` to ensure thread safety.
- Supports:
  - Placing orders with immediate matching if possible
  - Canceling orders
  - Asking for best price
  - Printing order book and trade history

---

## Console App

The solution can be opened in Visual studio or run via .NET CLI:

1. **Build the project:**
   ```bash
   dotnet build
   ```

2. **Run the app:**
   ```bash
   dotnet run
   ```

3. Modify the `Program.cs` file to run your own simulation:

```csharp
var service = new OrderMatchingService();
service.PlaceOrder(new Order { Side = OrderSide.Buy, Price = 10, Quantity = 5 });
service.PlaceOrder(new Order { Side = OrderSide.Sell, Price = 9, Quantity = 5 });

Console.WriteLine(service.GetOrderBookText());
Console.WriteLine(service.GetTradingHistoryText());
```

No external input files are required — it runs fully in-memory. The class methods are all synchonous as there is no external I/O to do in this demonstration.

---

## Tests

This project includes a comprehensive test suite (`OrderMatchingServiceTests.cs`) that validates both functional correctness and thread safety of the matching engine.

### Test Coverage

- **Order placement and matching**
- **Order cancellation**
- **Price discovery**
- **Thread safety under concurrency**

### Run Tests

Run using the .NET CLI:

```bash
dotnet test
```

## Concurrency and Locking Strategy

The demonstrated implementation uses a simple global locking strategy that ensures thread-safety across all operations. While effective, this broad locking becomes a scalability bottleneck in highly concurrent systems, as the majority of operations are serialized through a single shared lock.

It's worth considering **lock locality**. The engine's structure can be viewed as three-dimensional:

1. Order side: Buy or Sell  
2. Price level  
3. List of orders at that price

Clearly, matching at one price level does not need to block order placement at unrelated price levels. More granular locking — ideally at the **price-level list** — is achievable.

That said, real-world usage typically clusters orders near the current market price. As a result, contention will naturally concentrate around a small set of lists. To alleviate this, we can aim for **finer-grained operations within a list** itself. For example, we could allow matching (i.e. dequeuing from the front) to happen concurrently with enqueuing new orders at the end.

`ConcurrentQueue<T>` would be an ideal candidate, but it does not support arbitrary removals — which is necessary for order cancellation. There are two main strategies to work around this:

1. **Custom concurrent queue implementation** with indexed removal.  
2. **Postponed removal** — where canceled or filled orders are simply left in the queue until they're dequeued naturally, skipping over them as needed.