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
}