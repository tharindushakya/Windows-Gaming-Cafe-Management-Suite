$ErrorActionPreference = 'Stop'

# Login
$login = @{ email = 'admin@gamingcafe.com'; password = 'Admin123!' } | ConvertTo-Json
$resp = Invoke-RestMethod -Uri 'http://localhost:5148/api/v1.0/auth/login' -Method Post -Body $login -ContentType 'application/json'
$token = $resp.accessToken
Write-Output "Token length: $($token.Length)"

# Build payload programmatically (safe)
# Use tomorrow's date to avoid past-date validation
$start = (Get-Date).AddDays(1).Date.AddHours(10)   # 10:00 AM local time tomorrow
$end = $start.AddHours(1)

$payload = @{
    userId = 7
    stationId = 5
    reservationDate = $start.ToUniversalTime().ToString('yyyy-MM-dd')
    startTime = $start.ToString('o')
    endTime = $end.ToString('o')
    notes = 'test from script - tomorrow'
} | ConvertTo-Json -Depth 5

Write-Output "Payload: $payload"

# POST reservation and capture full error body if any
try {
    $result = Invoke-RestMethod -Uri 'http://localhost:5148/api/v1.0/reservations' -Method Post -Body $payload -ContentType 'application/json' -Headers @{ Authorization = "Bearer $token" } -ErrorAction Stop
    $result | ConvertTo-Json -Depth 5 | Write-Output
} catch {
    Write-Output "POST error: $($_.Exception.Message)"
    if ($_.Exception.Response -ne $null) {
        $respStream = $_.Exception.Response.GetResponseStream()
        $sr = New-Object System.IO.StreamReader($respStream)
        $body = $sr.ReadToEnd()
        Write-Output "Response body: $body"
    }
}
