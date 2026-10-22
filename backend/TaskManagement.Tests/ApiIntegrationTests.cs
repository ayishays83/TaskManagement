using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Api.Data;
using Xunit;

namespace TaskManagement.Tests;

/// <summary>Runs the real API in memory, with an in-memory database instead of SQL Server.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var existing = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (existing != null) services.Remove(existing);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }
}

public class ApiIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ApiIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient client, int userId)> LoginAsync(string email, string password)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/Auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("token").GetString()!;
        var userId = body.GetProperty("user").GetProperty("id").GetInt32();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, userId);
    }

    private static object NewTask(int assignedToId) => new
    {
        title = "Integration test task",
        description = "Created by a test",
        priority = "Medium",
        deadline = (string?)null,
        assignedToId
    };

    // ---------- Authentication ----------

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/Task");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndUser()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/Auth/login",
            new { email = "admin@taskflow.com", password = "Admin@123" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("token").GetString()));
        Assert.Equal("Admin", body.GetProperty("user").GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/Auth/login",
            new { email = "admin@taskflow.com", password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Task assignment rules ----------

    [Fact]
    public async Task Manager_CannotAssignATaskToAnAdmin()
    {
        var (adminClient, adminId) = await LoginAsync("admin@taskflow.com", "Admin@123");
        var (managerClient, _) = await LoginAsync("manager@taskflow.com", "Manager@123");

        var response = await managerClient.PostAsJsonAsync("/api/Task", NewTask(adminId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        adminClient.Dispose();
    }

    [Fact]
    public async Task Manager_CanAssignATaskToAMemberOfTheirTeam()
    {
        var (managerClient, _) = await LoginAsync("manager@taskflow.com", "Manager@123");

        var assignable = await managerClient.GetFromJsonAsync<JsonElement>("/api/Users/assignable");
        Assert.True(assignable.GetArrayLength() > 0);
        var memberId = assignable[0].GetProperty("id").GetInt32();

        var response = await managerClient.PostAsJsonAsync("/api/Task", NewTask(memberId));

        Assert.True(response.IsSuccessStatusCode, $"Expected success but got {(int)response.StatusCode}.");
    }

    [Fact]
    public async Task User_CannotCreateTasks()
    {
        var (userClient, userId) = await LoginAsync("user1@taskflow.com", "User@1234");

        var response = await userClient.PostAsJsonAsync("/api/Task", NewTask(userId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Team access rules ----------

    [Fact]
    public async Task User_ListingTeams_OnlySeesTheirOwnTeam()
    {
        var (userClient, userId) = await LoginAsync("user1@taskflow.com", "User@1234");

        var response = await userClient.GetAsync("/api/Teams");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var teams = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(teams.GetArrayLength() <= 1);

        foreach (var team in teams.EnumerateArray())
        {
            var memberIds = team.GetProperty("members")
                .EnumerateArray()
                .Select(m => m.GetProperty("id").GetInt32());

            Assert.Contains(userId, memberIds);
        }
    }

    [Fact]
    public async Task User_CannotCreateTeams()
    {
        var (userClient, _) = await LoginAsync("user1@taskflow.com", "User@1234");

        var response = await userClient.PostAsJsonAsync("/api/Teams", new { name = "Sneaky Team" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Manager_CannotCreateTeams()
    {
        var (managerClient, _) = await LoginAsync("manager@taskflow.com", "Manager@123");

        var response = await managerClient.PostAsJsonAsync("/api/Teams", new { name = "Another Team" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_CannotAddTeamMembers()
    {
        var (userClient, userId) = await LoginAsync("user1@taskflow.com", "User@1234");

        var response = await userClient.PostAsJsonAsync("/api/Teams/1/members", new { userId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Manager_CannotChangeUserRoles()
    {
        var (managerClient, _) = await LoginAsync("manager@taskflow.com", "Manager@123");

        var response = await managerClient.PatchAsJsonAsync("/api/Users/2/role", new { role = "Admin" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}