using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using WebApplication2.Models;

namespace WebApplication2;

[ApiController]
[Route("api/trips")]
public class TripController: ControllerBase
{
    private readonly IConfiguration _configuration;

    public TripController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetTrips(CancellationToken token)
    {
        var tripList=new List<Trip>();
        
        await using var con=new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await using var com = new SqlCommand(@"
            SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name AS Country
            FROM Trip t
            JOIN Country_Trip ct ON ct.IdTrip = t.IdTrip
            JOIN Country c ON c.IdCountry = ct.IdCountry
        ", con);
        await con.OpenAsync(token);
        //btw i used cancelation token but i hope its ok
        //https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken?view=net-9.0
        
        await using var rdr = await com.ExecuteReaderAsync(token);
        
        
        var tripDicti= new Dictionary<int, Trip>();

        while (await rdr.ReadAsync(token))
        {
            var id=(int) rdr["IdTrip"];

            tripDicti[id] = new Trip
            {
                IdTrip = id,
                Name = rdr["Name"].ToString(),
                Description = rdr["Description"].ToString(),
                DateFrom = (DateTime)rdr["DateFrom"],
                DateTo = (DateTime)rdr["DateTo"],
                MaxPeople = (int)rdr["MaxPeople"],
                Countries = new List<string>() 
            };  //i add an updater cause {insert exp here im lazy}
            tripDicti[id].Countries.Add(rdr["Country"].ToString());
        }
        return Ok(tripDicti.Values);//check what happens if i use .toList()
    }
}