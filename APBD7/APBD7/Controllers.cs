using Microsoft.AspNetCore.Mvc;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace APBD7
{
    [ApiController]
    [Route("[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly string _connectionString;

        public OrdersController(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default");
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(string productName, int amount)
        {
            try
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
                    string insertOrderQuery = $"INSERT INTO [Order](IdProduct, Amount, CreatedAt) VALUES(@ProductId, @Amount, GETDATE())";
                    using (SqlCommand command = new SqlCommand(insertOrderQuery, connection))
                    {
                        command.Parameters.AddWithValue("@ProductId", productId);
                        command.Parameters.AddWithValue("@Amount", amount);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                return Ok("Order created successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}