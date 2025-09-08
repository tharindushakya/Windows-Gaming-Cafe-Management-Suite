using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

Console.WriteLine("Admin UI Smoke Test starting...");
var apiBase = Environment.GetEnvironmentVariable("ADMIN_UI_SMOKE_TEST_API_BASE") ?? "http://localhost:5148";
var client = new HttpClient { BaseAddress = new Uri(apiBase) };

// Password is read from the environment to avoid committing credentials in source.
var smokeTestPassword = Environment.GetEnvironmentVariable("ADMIN_UI_SMOKE_TEST_PASSWORD") ?? "<<ADMIN_UI_SMOKE_TEST_PASSWORD>>";

async Task<string?> LoginWith2FA()
{
    var loginReq = new { email = "admin@gamingcafe.com", password = smokeTestPassword };
    var resp = await client.PostAsync("/api/auth/login", new StringContent(JsonSerializer.Serialize(loginReq), Encoding.UTF8, "application/json"));
    Console.WriteLine($"Login initial: {resp.StatusCode}");
    var body = await resp.Content.ReadAsStringAsync();
    Console.WriteLine(body);
    if (!resp.IsSuccessStatusCode) return null;

    using var doc = JsonDocument.Parse(body);
    var root = doc.RootElement;
    if (root.TryGetProperty("accessToken", out var at)) return at.GetString();

    if (root.TryGetProperty("requiresTwoFactor", out var r) && r.GetBoolean())
    {
        var twoFactorToken = root.TryGetProperty("twoFactorToken", out var tft) ? tft.GetString() : null;
        var devResp = await client.PostAsync("/api/auth/dev/get-2fa-code", new StringContent(JsonSerializer.Serialize(new { email = "admin@gamingcafe.com" }), Encoding.UTF8, "application/json"));
        if (!devResp.IsSuccessStatusCode) return null;
        var devBody = await devResp.Content.ReadAsStringAsync();
        using var devDoc = JsonDocument.Parse(devBody);
        var code = devDoc.RootElement.GetProperty("code").GetString();

    var loginWithCode = new { email = "admin@gamingcafe.com", password = smokeTestPassword, twoFactorCode = code, twoFactorToken = twoFactorToken };
        var final = await client.PostAsync("/api/auth/login", new StringContent(JsonSerializer.Serialize(loginWithCode), Encoding.UTF8, "application/json"));
        if (!final.IsSuccessStatusCode) return null;
        var finalBody = await final.Content.ReadAsStringAsync();
        using var finalDoc = JsonDocument.Parse(finalBody);
        return finalDoc.RootElement.GetProperty("accessToken").GetString();
    }

    return null;
}

var token = await LoginWith2FA();
if (string.IsNullOrEmpty(token))
{
    Console.WriteLine("Login failed for Admin UI smoke test");
    return 1;
}

client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

// Call Users list
var usersResp = await client.GetAsync("/api/users");
Console.WriteLine($"Users list status: {usersResp.StatusCode}");
var usersBody = await usersResp.Content.ReadAsStringAsync();
Console.WriteLine(usersBody);

// Call Products list
var productsResp = await client.GetAsync("/api/products");
Console.WriteLine($"Products list status: {productsResp.StatusCode}");
var productsBody = await productsResp.Content.ReadAsStringAsync();
Console.WriteLine(productsBody);

Console.WriteLine("Admin UI Smoke Test finished.");
return 0;
