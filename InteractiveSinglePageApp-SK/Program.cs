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
using System.Xml;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAntiforgery();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/";
    options.LogoutPath = "/";
    options.AccessDeniedPath = "/";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
});
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
                        Url TEXT NOT NULL UNIQUE,
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
            string htmlContent = $"""<div class="alert alert-danger registerMsg" role="alert">User Already Exists.</div>""";
            return Results.Content(htmlContent, "text/html");
        }
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        connection.Execute("INSERT INTO Users (email, password) VALUES (@Email, @PasswordHash)", new { Email = email, PasswordHash = passwordHash });
        return Results.Content(content: $"""<div class="alert alert-success registerMsg" role="alert">User Created Successfully.</div>""", contentType: "text/html");
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
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(20)
        };
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
        context.Session.SetInt32("UserId", (int)user.Id);
        string success = $"""<div class="alert alert-success loginMessage" role="alert">Login Successful</div>""";
        return Results.Content(success, "text/html");
    }
});

app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    context.Session.Remove("UserId");
    return Results.NoContent();
});



app.MapPost("/addFeed", async (HttpContext context, [FromForm] string addURL, IDbConnection db) =>
{
    var userId = context.Session.GetInt32("UserId");
    if (userId == null)
    {
        return Results.Redirect("/login");
    }
    if (!await IsValidRSSFeed(addURL))
    {
        string htmlContent = $"""<div class="alert alert-danger addFeedMsg" role="alert">Invalid RSS feed URL.</div>""";
        return Results.Content(htmlContent, "text/html");
    }
    using (var connection = new SqliteConnection(connectionString))
    {
        try
        {
            connection.Execute("INSERT INTO Feeds (UserId, Url) VALUES (@UserId, @Url)", new { UserId = userId, Url = addURL });
        }catch
        {
            return Results.Content($"""<div class="alert alert-danger addFeedMsg" role="alert">Feed already Exists.</div>""");
        }
        return Results.Content($"""<div class="alert alert-success addFeedMsg" role="alert">Feed Added Successfully.</div>""");
    }

});

app.MapDelete("/removeFeed", async (HttpContext context, [FromForm] string id , IDbConnection db) =>
{
    var userId = context.Session.GetInt32("UserId");
    if (userId == null)
    {
        return Results.Redirect("/logout");
    }


    using (var connection = new SqliteConnection(connectionString))
    {
        int numRows = connection.Execute("DELETE FROM Feeds WHERE Id = @Id AND UserId = @UserId", new { Id = id, UserId = userId });
    }
    return Results.Content($"""<div class="alert alert-success removeFeedMsg" role="alert">Feed Removed Successfully.</div>""");
});

app.MapGet("/home", async (HttpContext context, IDbConnection db, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context);
    var userId = context.Session.GetInt32("UserId");
    if (userId == null)
    {
        string loginHtml = $"""
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
                <a hx-get="/registerPage" hx-target=".addFeedError" class="form-text register">Don't have an account? Register
                    Now!</a>
                <div class="loginError"><div>
            </div>
        </div>
    </div>
    """;
        return Results.Content(loginHtml, "text/html");
    }

    using (var connection = new SqliteConnection(connectionString))
    {
        var feeds = connection.Query<Feed>("SELECT * FROM Feeds WHERE UserId = @UserId", new { UserId = userId });
        var options = new StringBuilder();
        var feedCards = new StringBuilder();
        foreach (var feed in feeds)
        {
            options.AppendLine($"""<option value="{feed.Id}">{feed.Url}</option>""");
            XmlReader reader = XmlReader.Create(feed.Url);
            SyndicationFeed feedItems = SyndicationFeed.Load(reader);
            reader.Close();
            // Start a container for the feed link
            feedCards.AppendLine($@"
                <div class='card mb-3 mx-auto'>
                <div class='card-body'>
                <h5 class='card-title'>{feedItems.Title.Text}: {feed.Url}</h5>
                <p class='card-text text-muted feedHeader'>Last Updated: {feedItems.LastUpdatedTime}</p>");
            // Iterate over items and add them to the container
            foreach (var item in feedItems.Items)
            {
                var title = item.Title?.Text ?? "Title Not Available";
                var summary = item.Summary?.Text ?? "Summary Not Available";
                var link = item.Links.Count > 0 ? item.Links[0]?.Uri?.AbsoluteUri ?? "#" : "#";
                var publishedDate = item.PublishDate.DateTime;
                // Generate HTML card for each feed item
                feedCards.AppendLine($@"
                    <div class='feed-item'>
                    <p class='card-text'>{summary}</p>
                    <a href='{link}' class='btn btn-outline-dark mb-2'>Read More</a>
                    <p class='card-text mb-2 text-muted'>Publish Date: {publishedDate}</p>
                    </div>"
                 );
            }
            // Close the container for the feed link
            feedCards.AppendLine(@"
                </div>
                </div>");
        }
        string loggedInHtml = $"""
        <div class="d-flex justify-content-center" id="pageTopRow">
            <div class="card" id="feeds">
                <div class="card-body">
                    <h5 class="card-title h3 pt-3">Hello, User!</h5>
                    <form hx-post="/addFeed" hx-target=".addFeedError">
                        <input name="{token.FormFieldName}" type="hidden" value="{token.RequestToken}" />
                        <div class="input-control">
                            <label for="URL" class="form-label">Feed URL</label>
                            <input type="text" required class="form-control mb-2" id="addURL"
                                placeholder="Enter RSS feed URL" name="addURL">
                        </div>
                        <button class="btn btn-outline-dark mt-2" type="submit" id="addURL">Add Feed</button>
                    </form>
                    <div class="addFeedError"></div>
                    <form hx-delete="/removeFeed" hx-target=".removeFeedError">
                        <input name="{token.FormFieldName}" type="hidden" value="{token.RequestToken}" />
                        <div class="input-control">
                            <label for="URL" class="form-label">Feed URL</label>
                            <select required class="form-select" name="id" id="id">
                                <option disabled hidden selected>Select a URL to remove</option>
                                {options}
                            </select>
                        </div>
                        <button class="btn btn-outline-dark mt-2" type="submit" id="addURL">Remove Feed</button>
                    </form>
                    <div class="removeFeedError"></div>
                </div>
            </div>
        </div>
            <h2 class="text-center mt-2">Your Feeds</h2>
            {feedCards}
        </div>
        """;
        return Results.Content(loggedInHtml, "text/html");
    }
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

async Task<bool> IsValidRSSFeed(string url)
{
    try
    {
        using (HttpClient client = new HttpClient())
        {
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }
            var content = await response.Content.ReadAsStringAsync();
            var xmlDoc = XDocument.Parse(content);
            return xmlDoc.Root.Name.LocalName == "rss" || xmlDoc.Root.Name.LocalName == "feed";
        }
    }
    catch
    {
        return false;
    }
}

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




