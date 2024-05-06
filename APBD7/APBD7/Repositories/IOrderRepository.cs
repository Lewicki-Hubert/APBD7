using APBD7.Models;

namespace APBD7.Repositories;

public interface IOrderRepository
{
    Task<int> CreateOrderAsync(string productName, int amount);
    Task<IEnumerable<Order>> GetOrdersAsync();
}