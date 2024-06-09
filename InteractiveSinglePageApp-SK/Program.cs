using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Data.Sqlite;
using System.Data;
using Dapper;
using SQLitePCL;
using Microsoft.AspNetCore.Http;
using System.Text;
using Microsoft.AspNetCore.Builder;
using System.Xml.Linq;
using System.ServiceModel.Syndication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAntiforgery();
builder.Services.AddDistributedMemoryCache();
var connectionString = "Data Source=wwwroot/database.db";
builder.Services.AddSingleton<IDbConnection>(_ => new SqliteConnection(connectionString));
using (var connection = new SqliteConnection(connectionString))
{
    connection.Open();
    connection.Execute("""
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        email TEXT NOT NULL UNIQUE,
                        password TEXT NOT NULL
                      
                    )
                    """);

    connection.Execute("""
                    CREATE TABLE IF NOT EXISTS Feeds (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId INTEGER NOT NULL,
                        Url TEXT NOT NULL,
                        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
                    )
                    """);
}
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();
app.MapFallbackToFile("/index.html");
app.UseAntiforgery();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseSession();

app.MapPost("/register", async (HttpContext context, [FromForm] string email, [FromForm] string password, IDbConnection db, IAntiforgery antiforgery) =>
{
    await antiforgery.ValidateRequestAsync(context);
    using (var connection = new SqliteConnection(connectionString))
    {
        var user = connection.QuerySingleOrDefault<User>("SELECT * FROM Users WHERE email = @Email", new { Email = email });
        if (user != null)
        {
            string htmlContent = $"""<div class="alert alert-danger" role="alert">User Already Exists.</div>""";
            return Results.Content(htmlContent, "text/html");
        }
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        connection.Execute("INSERT INTO Users (email, password) VALUES (@Email, @PasswordHash)", new { Email = email, PasswordHash = passwordHash });
        return Results.Ok();
    }
});

app.MapGet("/api/tokens", (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context);
    string html = $"""<input name = "{token.FormFieldName}" type = "hidden" value = "{token.RequestToken}"/>""";
    return Results.Content(html, "text/html");
});

app.MapPost("/login", async (HttpContext context, [FromForm] string email, [FromForm] string password, IDbConnection db,IAntiforgery antiforgery) =>
{
    await antiforgery.ValidateRequestAsync(context);
    using (var connection = new SqliteConnection(connectionString))
    {
        var user = connection.QuerySingleOrDefault<User>("SELECT * FROM Users WHERE email = @Email", new { Email = email });
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.password))
        {
            string htmlContent = $"""<div class="alert alert-danger loginMessage" role="alert">Invalid email or password</div>""";
            return Results.Content(htmlContent,"text/html");
        }
        context.Session.SetInt32("UserId", (int)user.Id);
        string success = $"""<div class="alert alert-success loginMessage" role="alert">Login Successful</div>""";
        return Results.Content(success, "text/html");
    }
});

app.MapGet("/logout", async (HttpContext context) =>
{
    context.Session.Remove("UserId");
    return Results.NoContent();
});

app.MapGet("/home", async (HttpContext context, IDbConnection db) =>
{
    var userId = context.Session.GetInt32("UserId");
    if (userId == null)
    {
        return Results.Redirect("/login");
    }

    using (var connection = new SqliteConnection(connectionString))
    {
        var feeds = connection.Query<Feed>("SELECT * FROM Feeds WHERE UserId = @UserId", new { UserId = userId });
        return Results.Content("<div>Testing</div>","text/html");
    }
});

app.MapPost("/addFeed", async (HttpContext context, [FromForm] string url, IDbConnection db) =>
{
    var userId = context.Session.GetInt32("UserId");
    if (userId == null)
    {
        return Results.Redirect("/login");
    }

    using (var connection = new SqliteConnection(connectionString))
    {
        connection.Execute("INSERT INTO Feeds (UserId, Url) VALUES (@UserId, @Url)", new { UserId = userId, Url = url });
    }
    return Results.Redirect("/home");
});

app.MapDelete("/removeFeed", async (HttpContext context, [FromForm] int id, IDbConnection db) =>
{
    var userId = context.Session.GetInt32("UserId");
    if (userId == null)
    {
        return Results.Redirect("/login");
    }

    using (var connection = new SqliteConnection(connectionString))
    {
        connection.Execute("DELETE FROM Feeds WHERE Id = @Id AND UserId = @UserId", new { Id = id, UserId = userId });
    }
    return Results.Redirect("/home");
});


app.MapGet("/loginPage", async (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context);
    string htmlContent = $"""
        <div class="d-flex justify-content-center" id="pageTopRow">
        <div class="card" id="quickSignIn">
            <div class="card-body">
                <h5 class="card-title h3 pt-3">Hello!</h5>
                <p class="card-text">Please Log In</p>
                <form class="login" hx-post="/login" hx-target=".loginError">
                    <input name = "{token.FormFieldName}" type = "hidden" value = "{token.RequestToken}"/>
                    <div class="input-control">
                        <label for="email" class="form-label">Email address</label>
                        <input type="text" class="form-control mb-2" id="email" placeholder="name@example.com" name="email">
                        <div class="error form-text mb-1"></div>
                    </div>
                    <div class="input-control password">
                        <label for="password" class="form-label">Password</label>
                        <input type="password" id="password" class="form-control mb-2" placeholder="password" name="password">
                        <div class="error form-text"></div>
                    </div>
                    <button disabled class="btn btn-outline-dark mt-2" type="submit" id="login">Log In</button>
                </form>
                <a hx-get="/registerPage" hx-target=".replace" class="form-text register">Don't have an account? Register
                    Now!</a>
                <div class="loginError"><div>
            </div>
        </div>
    </div>
    """;
    return Results.Content(htmlContent, "text/html");
});

app.MapGet("/registerPage", async (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context);
    string htmlContent = $"""
    <div class="d-flex justify-content-center" id="pageTopRow">
        <div class="card" id="quickSignIn">
            <div class="card-body">
                <h5 class="card-title h3 pt-3">Create a new account</h5>
                <p class="card-text">Please fill the required info</p>
                <form hx-post="/register" hx-target=".registerError" novalidate class="register">
                    <input name = "{token.FormFieldName}" type = "hidden" value = "{token.RequestToken}"/>
                    <div class="input-control">
                        <label for="email" class="form-label">Email address</label>
                        <input type="text" class="form-control mb-2" id="email" placeholder="name@example.com" name="email">
                        <div class="error"></div>
                    </div>
                    <div class="input-control password">
                        <label for="inputPassword5" class="form-label">Password</label>
                        <input type="password" id="password" class="form-control mb-3 password"
                            aria-describedby="passwordHelpBlock mb-0" placeholder="password" name="password">
                        <div class="error"></div>
                    </div>
                    <div class="input-control password">
                        <label for="passwordConf" class="form-label">Confirm Password</label>
                        <input type="password" id="passwordConf" class="form-control mb-3" required
                            aria-describedby="passwordHelpBlock mb-0" placeholder="password">
                        <div class="error"></div>
                    </div>
                    <button class="btn btn-outline-dark mt-2" type="submit" disabled id="registerBtn">Register</button>
                    <div class="registerError"></div>
                </form>
            </div>
        </div>
    </div>
    """;
    return Results.Content(htmlContent,"text/html");
});

app.Run();
public class User
{
    public int? Id { get; set; }
    public string? email { get; set; }
    public string? password { get; set; }
}

public class Feed
{
    public int? Id { get; set; }
    public int? UserId { get; set; }
    public string? Url { get; set; }
}

