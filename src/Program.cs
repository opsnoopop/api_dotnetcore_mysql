using System.Data;
using Dapper;
using MySqlConnector;
using Api.Models;

var builder = WebApplication.CreateBuilder(args);

// ===== ENV → connection string =====
string dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "container_mysql";
string dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
string dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "testuser";
string dbPass = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "testpass";
string dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "testdb";

// ปรับ pool ได้ตามโหลด (ตั้งค่าตัวอย่าง MaximumPoolSize=200)
string connStr =
    $"Server={dbHost};Port={dbPort};User ID={dbUser};Password={dbPass};Database={dbName};" +
    "Pooling=true;MaximumPoolSize=200;Connection Timeout=5;Default Command Timeout=30;Allow User Variables=true;";

// ===== Register a single, thread-safe DataSource =====
var dataSource = new MySqlDataSourceBuilder(connStr).Build();
builder.Services.AddSingleton(dataSource);

var app = builder.Build();

app.MapGet("/", () => Results.Json(new { message = "Hello World from .NET" }));

// POST /users { username, email }
app.MapPost("/users", async (MySqlDataSource ds, User payload) =>
{
    if (string.IsNullOrWhiteSpace(payload.username) || string.IsNullOrWhiteSpace(payload.email))
        return Results.BadRequest(new { error = "username and email are required" });

    const string sql = @"INSERT INTO users (username, email) VALUES (@username, @email);
                         SELECT LAST_INSERT_ID();";
    try
    {
        await using var db = await ds.OpenConnectionAsync(); // connection ใหม่ ต่อคำขอ
        var newId = await db.ExecuteScalarAsync<long>(sql, new { payload.username, payload.email });
        return Results.Created($"/users/{newId}", new { message = "User created successfully", user_id = newId });
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex);
        return Results.Json(new { error = "Database error" }, statusCode: 500);
        // หรือ: return Results.Problem(title: "Database error", statusCode: 500);
    }
});

// GET /users/{user_id}
app.MapGet("/users/{user_id:long}", async (MySqlDataSource ds, long user_id) =>
{
    const string sql = "SELECT user_id, username, email FROM users WHERE user_id = @user_id LIMIT 1;";
    try
    {
        await using var db = await ds.OpenConnectionAsync(); // connection ใหม่ ต่อคำขอ
        var user = await db.QueryFirstOrDefaultAsync<User>(sql, new { user_id });
        if (user is null) return Results.NotFound(new { error = "User not found" });
        return Results.Ok(user);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex);
        return Results.Json(new { error = "Database error" }, statusCode: 500);
        // หรือ: return Results.Problem(title: "Database error", statusCode: 500);
    }
});

app.Run();
