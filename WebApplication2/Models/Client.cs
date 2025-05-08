namespace WebApplication2.Models;

public class Client
{
    
    private int IdClient {get; set;}
    public string FirstName {get; set;}
    public string LastName {get; set;}
    public string Email {get; set;}
    public string Telephone {get; set;}
    //i did not add pesel cause isnt it the same thing as Id??
}