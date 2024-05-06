using System.Data.SqlClient;
using APBD7.Models;

namespace APBD7.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly string _connectionString;

    public OrderRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default");
    }

    public async Task<int> CreateOrderAsync(string productName, int amount)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            // Retrieve the product ID
            string getProductIDQuery = $"SELECT IdProduct FROM Product WHERE Name=@ProductName";
            int productId;
            using (SqlCommand command = new SqlCommand(getProductIDQuery, connection))
            {
                command.Parameters.AddWithValue("@ProductName", productName);
                productId = (int)await command.ExecuteScalarAsync();
            }

            // Insert the order
            string insertOrderQuery = $"INSERT INTO [Order](IdProduct, Amount, CreatedAt) VALUES(@ProductId, @Amount, GETDATE()); SELECT SCOPE_IDENTITY()";
            using (SqlCommand command = new SqlCommand(insertOrderQuery, connection))
            {
                command.Parameters.AddWithValue("@ProductId", productId);
                command.Parameters.AddWithValue("@Amount", amount);
                return Convert.ToInt32(await command.ExecuteScalarAsync());
            }
        }
    }

    public async Task<IEnumerable<Order>> GetOrdersAsync()
    {
        List<Order> orders = new List<Order>();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            string retrieveOrdersQuery = "SELECT o.IdOrder, p.Name AS ProductName, o.Amount, o.CreatedAt, o.FulfilledAt " +
                                         "FROM [Order] o " +
                                         "INNER JOIN Product p ON o.IdProduct = p.IdProduct";

            using (SqlCommand command = new SqlCommand(retrieveOrdersQuery, connection))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        orders.Add(new Order
                        {
                            Id = reader.GetInt32(0),
                            ProductId = reader.GetInt32(1),
                            Amount = reader.GetInt32(2),
                            CreatedAt = reader.GetDateTime(3),
                            FulfilledAt = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4)
                        });
                    }
                }
            }
        }
        return orders;
    }
}