using APBD7.Models;

using Microsoft.AspNetCore.Mvc;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

[ApiController]
[Route("[controller]")]
public class WarehouseController : ControllerBase
{
    private readonly string _connectionString;

    public WarehouseController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default");
    }

    [HttpPost]
    public async Task<IActionResult> AddProductToWarehouse([FromBody] WarehouseEntry entry)
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // Verify product and warehouse existence
                if (!await VerifyProductAndWarehouseExist(entry.IdProduct, entry.IdWarehouse, connection))
                {
                    return BadRequest("Product or warehouse does not exist.");
                }

                // Update the order and get the order ID
                int orderId = await UpdateOrderAndGetId(entry.IdProduct, entry.Amount, connection);
                if (orderId == 0)
                {
                    return BadRequest("No valid order found or already fulfilled.");
                }

                // Add to Product_Warehouse
                await AddToProductWarehouse(entry, orderId, connection);

                return Ok("Product added successfully to the warehouse and order fulfilled.");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    private async Task<bool> VerifyProductAndWarehouseExist(int productId, int warehouseId, SqlConnection connection)
    {
        string commandText = @"
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM Product WHERE IdProduct = @IdProduct) 
                AND EXISTS (
                SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse)
                THEN 1 ELSE 0 END";
        using (SqlCommand command = new SqlCommand(commandText, connection))
        {
            command.Parameters.AddWithValue("@IdProduct", productId);
            command.Parameters.AddWithValue("@IdWarehouse", warehouseId);
            return (int)await command.ExecuteScalarAsync() == 1;
        }
    }

    private async Task<int> UpdateOrderAndGetId(int productId, int amount, SqlConnection connection)
    {
        string updateOrder = @"
            UPDATE [Order] SET FulfilledAt = GETDATE()
            WHERE IdProduct = @IdProduct AND Amount = @Amount AND FulfilledAt IS NULL
            SELECT SCOPE_IDENTITY();";
        using (SqlCommand command = new SqlCommand(updateOrder, connection))
        {
            command.Parameters.AddWithValue("@IdProduct", productId);
            command.Parameters.AddWithValue("@Amount", amount);
            object result = await command.ExecuteScalarAsync();
            return result == DBNull.Value ? 0 : Convert.ToInt32(result);
        }
    }

    private async Task AddToProductWarehouse(WarehouseEntry entry, int orderId, SqlConnection connection)
    {
        string addToWarehouse = @"
            INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
            VALUES (@IdWarehouse, @IdProduct, @OrderId, @Amount, 
            (SELECT Price FROM Product WHERE IdProduct = @IdProduct) * @Amount, @CreatedAt)";
        using (SqlCommand command = new SqlCommand(addToWarehouse, connection))
        {
            command.Parameters.AddWithValue("@IdWarehouse", entry.IdWarehouse);
            command.Parameters.AddWithValue("@IdProduct", entry.IdProduct);
            command.Parameters.AddWithValue("@OrderId", orderId);
            command.Parameters.AddWithValue("@Amount", entry.Amount);
            command.Parameters.AddWithValue("@CreatedAt", entry.CreatedAt);
            await command.ExecuteNonQueryAsync();
        }
    }
}
