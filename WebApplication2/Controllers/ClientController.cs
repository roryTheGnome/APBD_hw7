using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using WebApplication2.Models;

namespace WebApplication2;


[Route("api/clients")]
[ApiController]
public class ClientController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ClientController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("{id}/trips")]
    public async Task<IActionResult> GetClientTrips(int id, CancellationToken token)
    {
        var tripList = new List<ClientTrip>();
        
        await using var con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await using var com = new SqlCommand(@"
            SELECT t.IdTrip, t.Name AS TripName, t.Description, t.DateFrom, t.DateTo, 
                   ct.RegisteredAt, ct.PaymentDate
            FROM Client_Trip ct
            JOIN Trip t ON t.IdTrip = ct.IdTrip
            WHERE ct.IdClient = @id
        ", con);
        
        com.Parameters.AddWithValue("@id", id);
        await con.OpenAsync(token);

        await using var rdr = await com.ExecuteReaderAsync(token);
        while (await rdr.ReadAsync(token))
        {
            tripList.Add(new ClientTrip
            {
                IdTrip = (int)rdr["IdTrip"],
                TripName = rdr["TripName"].ToString()!,
                Description = rdr["Description"].ToString()!,
                DateFrom = (DateTime)rdr["DateFrom"],
                DateTo = (DateTime)rdr["DateTo"],
                RegisteredAt = (DateTime)rdr["RegisteredAt"],
                PaymentDate = rdr["PaymentDate"] as DateTime?
            });
        }
        
        return Ok(tripList);
    }


    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] Client client, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(client.FirstName) ||
            string.IsNullOrWhiteSpace(client.LastName))
        {
            return BadRequest("Missing required fields.");
        } 
        
        
        await using var con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await using var com = new SqlCommand(@"
        INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
        VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel);
        SELECT SCOPE_IDENTITY();
    ", con);
        
        com.Parameters.AddWithValue("@FirstName", client.FirstName);
        com.Parameters.AddWithValue("@LastName", client.LastName);
        com.Parameters.AddWithValue("@Email", client.Email ?? (object)DBNull.Value);
        com.Parameters.AddWithValue("@Telephone", client.Telephone ?? (object)DBNull.Value);
        com.Parameters.AddWithValue("@Pesel", client.Pesel);

        await con.OpenAsync(token);
        
        var idd = Convert.ToInt32(await com.ExecuteScalarAsync(token));
        client.IdClient = idd;

        return CreatedAtAction(nameof(GetClientTrips), new { id = idd }, client);
    }

    
    [HttpPut("{id}/trips/{tripId}")]
    public async Task<IActionResult> RegisterTrip(int id, int trip, CancellationToken token)
    {
        await using var con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await con.OpenAsync(token);
        await using var transaction=await con.BeginTransactionAsync(token);

        try
        {
            await using (var checkClient = new SqlCommand("SELECT 1 FROM Client WHERE IdClient = @id", con, (SqlTransaction)transaction))
            {
                checkClient.Parameters.AddWithValue("@id", id);
                var exists = await checkClient.ExecuteScalarAsync(token);
                if (exists == null)
                    return NotFound($"Client {id} does not exist.");
            }//NOTE TO FUTURE SELF: no need to delete this part in order to shrink the code
            
            await using (var checkTrip = new SqlCommand("SELECT 1 FROM Trip WHERE IdTrip = @tripId", con, (SqlTransaction)transaction))
            {
                checkTrip.Parameters.AddWithValue("@tripId", trip);
                var exists = await checkTrip.ExecuteScalarAsync(token);
                if (exists == null)
                    return NotFound($"Trip {trip} does not exist.");
            }
            
            await using (var checkExisting = new SqlCommand(@"
            SELECT 1 FROM Client_Trip WHERE IdClient = @id AND IdTrip = @tripId
        ", con, (SqlTransaction)transaction))
            {
                checkExisting.Parameters.AddWithValue("@id", id);
                checkExisting.Parameters.AddWithValue("@tripId", trip);
                var exists = await checkExisting.ExecuteScalarAsync(token);
                if (exists != null)
                    return BadRequest($"Client {id} is already registered for trip {trip}.");
            }
            
            await using (var insert = new SqlCommand(@"
            INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)
            VALUES (@id, @tripId, @registeredAt)
        ", con, (SqlTransaction)transaction))
            {
                insert.Parameters.AddWithValue("@id", id);
                insert.Parameters.AddWithValue("@tripId", trip);
                insert.Parameters.AddWithValue("@registeredAt", DateTime.UtcNow);

                await insert.ExecuteNonQueryAsync(token);
            }
            
            await transaction.CommitAsync(token);
            return Ok($"registiry success");
        }
        catch
        {
            await transaction.RollbackAsync(token);
            return StatusCode(500, "Internal server error.");
        }
        
    }
    
    [HttpDelete("{id}/trips/{tripId}")]
    public async Task<IActionResult> CancelClientTrip(int id, int tripId, CancellationToken token)
    {
        await using var con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await using var com = new SqlCommand(@"
        DELETE FROM Client_Trip
        WHERE IdClient = @id AND IdTrip = @tripId
    ", con);

        com.Parameters.AddWithValue("@id", id);
        com.Parameters.AddWithValue("@tripId", tripId);

        await con.OpenAsync(token);
        var rowsAffected = await com.ExecuteNonQueryAsync(token);

        if (rowsAffected == 0)
        {
            return NotFound("No such registery exists.");
        }

        return Ok("done.");
    }

    
}