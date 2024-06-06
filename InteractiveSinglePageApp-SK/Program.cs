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

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAntiforgery();
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



var app = builder.Build();
app.MapFallbackToFile("/index.html");
app.UseAntiforgery();
app.UseStaticFiles();
app.UseHttpsRedirection();

app.Run();

public class User
{
    public int? Id { get; set; }
    public string? email { get; set; }

    public string? password { get; set; }
}
