using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
Console.WriteLine("API Smoke Test starting...");
var apiBase = "http://localhost:5148";
var client = new HttpClient { BaseAddress = new Uri(apiBase) };

async Task<JsonElement?> DoLoginGetResponseAsync(string? twoFactorCode = null, string? twoFactorToken = null)
{
    var loginReq = new Dictionary<string, object?>
    {
        ["email"] = "admin@gamingcafe.com",
        ["password"] = "Admin123!",
    };
    if (!string.IsNullOrEmpty(twoFactorCode)) loginReq["twoFactorCode"] = twoFactorCode;
    if (!string.IsNullOrEmpty(twoFactorToken)) loginReq["twoFactorToken"] = twoFactorToken;

    var loginResp = await client.PostAsync("/api/auth/login", new StringContent(JsonSerializer.Serialize(loginReq), Encoding.UTF8, "application/json"));
    Console.WriteLine($"Login status: {loginResp.StatusCode}");
    var loginJson = await loginResp.Content.ReadAsStringAsync();
    Console.WriteLine(loginJson);
    if (!loginResp.IsSuccessStatusCode)
        return null;

    using var loginDoc = JsonDocument.Parse(loginJson);
    return loginDoc.RootElement.Clone();
}

var initialLogin = await DoLoginGetResponseAsync();
string? token = null;
if (initialLogin == null)
{
    Console.WriteLine("Initial login failed—aborting.");
    return 1;
}

if (initialLogin.Value.TryGetProperty("accessToken", out var tokenElem))
{
    token = tokenElem.GetString();
}
else if (initialLogin.Value.TryGetProperty("requiresTwoFactor", out var requires2fa) && requires2fa.GetBoolean())
{
    // Extract the twoFactorToken and call the dev helper to obtain current TOTP
    var twoFactorToken = initialLogin.Value.TryGetProperty("twoFactorToken", out var tft) ? tft.GetString() : null;
    Console.WriteLine($"Two-factor required. Token: {twoFactorToken}");

    try
    {
        var devReq = new { email = "admin@gamingcafe.com" };
        var devResp = await client.PostAsync("/api/auth/dev/get-2fa-code", new StringContent(JsonSerializer.Serialize(devReq), Encoding.UTF8, "application/json"));
        Console.WriteLine($"Dev 2FA status: {devResp.StatusCode}");
        var devJson = await devResp.Content.ReadAsStringAsync();
        Console.WriteLine(devJson);
        if (devResp.IsSuccessStatusCode)
        {
            using var devDoc = JsonDocument.Parse(devJson);
            var code = devDoc.RootElement.GetProperty("code").GetString();
            // Retry login with code and token
            var second = await DoLoginGetResponseAsync(code, twoFactorToken);
            if (second != null && second.Value.TryGetProperty("accessToken", out var tok2))
            {
                token = tok2.GetString();
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Failed to retrieve 2FA code via dev endpoint: " + ex.Message);
    }
}

if (string.IsNullOrEmpty(token))
{
    Console.WriteLine("Login failed  aborting smoke test.");
    return 1;
}

Console.WriteLine("Got token: " + (string.IsNullOrEmpty(token) ? "<empty>" : token.Substring(0, Math.Min(8, token.Length)) + "..."));
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

// Create product - match required API DTO fields (Name, Price, Category)
var newProduct = new {
    name = "SmokeTest Product " + DateTime.UtcNow.Ticks,
    description = "Created by smoke test",
    price = 9.99m,
    category = "Test",
    stockQuantity = 5,
    minStockLevel = 1,
    isActive = true
};
var createResp = await client.PostAsync("/api/products", new StringContent(JsonSerializer.Serialize(newProduct), Encoding.UTF8, "application/json"));
Console.WriteLine($"Create status: {createResp.StatusCode}");
var createJson = await createResp.Content.ReadAsStringAsync();
Console.WriteLine(createJson);
if (!createResp.IsSuccessStatusCode)
{
    Console.WriteLine("Create failed — aborting.");
    return 1;
}
var createdDoc = JsonDocument.Parse(createJson);
var id = createdDoc.RootElement.GetProperty("productId").GetInt32();
Console.WriteLine("Created product id: " + id);

// Update product
var updateProduct = new {
    name = "SmokeTest Product Updated",
    description = "Updated by smoke test",
    price = 7.5m,
    category = "Test",
    stockQuantity = 10,
    minStockLevel = 1,
    isActive = true
};
var updateResp = await client.PutAsync($"/api/products/{id}", new StringContent(JsonSerializer.Serialize(updateProduct), Encoding.UTF8, "application/json"));
Console.WriteLine($"Update status: {updateResp.StatusCode}");
var updateJson = await updateResp.Content.ReadAsStringAsync();
Console.WriteLine(updateJson);

// Delete product
var deleteResp = await client.DeleteAsync($"/api/products/{id}");
Console.WriteLine($"Delete status: {deleteResp.StatusCode}");
var deleteJson = await deleteResp.Content.ReadAsStringAsync();
Console.WriteLine(deleteJson);

Console.WriteLine("Smoke test finished.");
return 0;
