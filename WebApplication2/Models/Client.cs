namespace WebApplication2.Models;

public class Client
{
    
    public int IdClient {get; set;}
    public string FirstName {get; set;} = null!;
    public string LastName {get; set;}= null!;
    public string Email {get; set;}= null!;
    public string Telephone {get; set;}= null!;
    public string Pesel {get; set;}= null!;
    //i did not add pesel cause isnt it the same thing as Id??
}