using System.Text;
using Microsoft.AspNetCore.Mvc;

using var client = new HttpClient();


//This will parse out the webToken from the login request
string getBetween(string strSource, string strStart, string strEnd)
{
    if (strSource.Contains(strStart) && strSource.Contains(strEnd))
    {
        int Start, End;
        Start = strSource.IndexOf(strStart, 0) + strStart.Length;
        End = strSource.IndexOf(strEnd, Start);
        return strSource.Substring(Start, End - Start);
    }

    return "";
}

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();



app.MapGet("/", () => "Hello World!");

//URL
Uri loginUrl = new Uri("https://api2.ghin.com/api/v1/golfer_login.json");

//This is post request to give get a token to use to access data. It will take in the users creds and then return a token to use. The user is then "logged in"
app.MapPost("/api/login", async (Credentials cred) => {
    //This will hold the web token when we get it.
    Token token = new Token();

    //Create a payload
    var payload = "{\"user\":{\"email_or_ghin\":" + cred.ghinNum + ",\"password\":\"" + cred.password + "\",\"remember_me\":\"true\"},\"token\":\"nonblank\"}";
    HttpContent content = new StringContent(payload, Encoding.UTF8, "application/json");

    //Run POST to get Auth Token
    var res = Task.Run(() => client.PostAsync(loginUrl, content));
   
    //Take the response from GHIN and use the Auth Token
    var responseString = await res.Result.Content.ReadAsStringAsync();

    //Parse the access token out of the json
    token.webToken = getBetween(responseString, "\"golfer_user_token\":\"", "\",");
   
    return Results.Created($"/api/login", token);
}); 


//This will be the get request for the ghin number
app.MapGet("/api/{ghinNum}", async (int ghinNum, [FromBody] Token token) => {
    //Add the bearer token
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.webToken);

    //Get request with the url
    var res = Task.Run(() => client.GetAsync("https://api.ghin.com/api/v1/golfers/search.json?per_page=1&page=1&golfer_id=" + ghinNum));
    return await res.Result.Content.ReadAsStringAsync();
});

//This will return all of the players recent scores
app.MapGet("api/{ghinNum}/recentScores", async (int ghinNum, [FromBody] Token token) => {
    //Add the bearer token
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.webToken);

    //Get request with the url
    var res = Task.Run(() => client.GetAsync("https://api2.ghin.com/api/v1/golfers/" + ghinNum + "/scores.json?source=GHINcom" + ghinNum));
    return await res.Result.Content.ReadAsStringAsync();
});

app.Run();


//This iwll store our credientials
class Credentials
{
    public int ghinNum {get; set;}
    public string? password{get; set;}

}; 

//This will store our token
class Token
{
    public string? webToken {get; set;}
} 
