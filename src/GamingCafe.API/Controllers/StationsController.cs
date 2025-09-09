using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using GamingCafe.API.Services;
using GamingCafe.API.Hubs;
using Asp.Versioning;
using GamingCafe.Core.Models;
using GamingCafe.Core.Interfaces;

namespace GamingCafe.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class StationsController : ControllerBase
{
    private readonly IStationService _stationService;
    private readonly IHubContext<GameCafeHub> _hubContext;
    private readonly GamingCafe.Core.Interfaces.Services.ICacheService _cacheService;

    public StationsController(IStationService stationService, IHubContext<GameCafeHub> hubContext, GamingCafe.Core.Interfaces.Services.ICacheService cacheService)
    {
        _stationService = stationService;
        _hubContext = hubContext;
        _cacheService = cacheService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllStations()
    {
        var cacheKey = "stations:all";
        var cached = await _cacheService.GetAsync<List<StationDto>>(cacheKey);
        if (cached != null)
            return Ok(cached);

        var stations = await _stationService.GetAllStationsAsync();
        var dto = stations.Select(MapToDto).ToList();

        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromSeconds(15));
        return Ok(dto);
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableStations()
    {
        var stations = await _stationService.GetAvailableStationsAsync();
        return Ok(stations.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetStation(int id)
    {
        var cacheKey = $"station:snapshot:{id}";
    var cached = await _cacheService.GetAsync<StationDto>(cacheKey);
        if (cached != null)
            return Ok(cached);

        var station = await _stationService.GetStationByIdAsync(id);
        if (station == null)
            return NotFound();

        var dto = MapToDto(station);
        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromSeconds(15));
        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateStation([FromBody] StationCreateRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var station = new GameStation
        {
            StationName = request.StationName,
            StationType = request.StationType,
            Description = request.Description,
            HourlyRate = request.HourlyRate,
            Processor = request.Processor ?? "",
            GraphicsCard = request.GraphicsCard ?? "",
            Memory = request.Memory ?? "",
            Storage = request.Storage ?? "",
            IpAddress = request.IpAddress ?? "",
            MacAddress = request.MacAddress ?? ""
        };

    var createdStation = await _stationService.CreateStationAsync(station);
    var createdDto = MapToDto(createdStation);
    // invalidate list cache and cache created snapshot briefly
    await _cacheService.RemoveAsync("stations:all");
    await _cacheService.SetAsync($"station:snapshot:{createdStation.StationId}", createdDto, TimeSpan.FromSeconds(15));
    await _hubContext.Clients.All.SendAsync("StationCreated", createdDto);

    return CreatedAtAction(nameof(GetStation), new { id = createdStation.StationId }, createdDto);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateStation(int id, [FromBody] StationUpdateRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var station = new GameStation
        {
            StationName = request.StationName,
            StationType = request.StationType,
            Description = request.Description,
            HourlyRate = request.HourlyRate,
            IsAvailable = request.IsAvailable,
            Processor = request.Processor ?? "",
            GraphicsCard = request.GraphicsCard ?? "",
            Memory = request.Memory ?? "",
            Storage = request.Storage ?? "",
            IpAddress = request.IpAddress ?? "",
            MacAddress = request.MacAddress ?? ""
        };

        var updatedStation = await _stationService.UpdateStationAsync(id, station);
        if (updatedStation == null)
            return NotFound();

    var dto = MapToDto(updatedStation);
    // invalidate caches
    await _cacheService.RemoveAsync("stations:all");
    await _cacheService.RemoveAsync($"station:snapshot:{id}");

    await _hubContext.Clients.All.SendAsync("StationUpdated", dto);
    return Ok(dto);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteStation(int id)
    {
        var success = await _stationService.DeleteStationAsync(id);
        if (!success)
            return NotFound();

    // invalidate caches
    await _cacheService.RemoveAsync("stations:all");
    await _cacheService.RemoveAsync($"station:snapshot:{id}");

    await _hubContext.Clients.All.SendAsync("StationDeleted", id);
    return NoContent();
    }

    [HttpPost("{id}/start-session")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> StartSession(int id, [FromBody] StartSessionRequest request)
    {
        var success = await _stationService.StartSessionAsync(id, request.UserId, request.HourlyRate);
        if (!success)
            return BadRequest("Could not start session");

    // invalidate/update cache for this station and list
    await _cacheService.RemoveAsync("stations:all");
    await _cacheService.RemoveAsync($"station:snapshot:{id}");

    await _hubContext.Clients.All.SendAsync("SessionStarted", id, request.UserId);
    return Ok();
    }

    [HttpPost("{id}/end-session")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> EndSession(int id)
    {
        var success = await _stationService.EndSessionAsync(id);
        if (!success)
            return BadRequest("Could not end session");

    // invalidate/update cache for this station and list
    await _cacheService.RemoveAsync("stations:all");
    await _cacheService.RemoveAsync($"station:snapshot:{id}");

    await _hubContext.Clients.All.SendAsync("SessionEnded", id);
    return Ok();
    }

    private static StationDto MapToDto(GameStation station)
    {
        return new StationDto
        {
            StationId = station.StationId,
            StationName = station.StationName,
            StationType = station.StationType,
            Description = station.Description,
            HourlyRate = station.HourlyRate,
            IsAvailable = station.IsAvailable,
            IsActive = station.IsActive,
            Processor = station.Processor,
            GraphicsCard = station.GraphicsCard,
            Memory = station.Memory,
            Storage = station.Storage,
            IpAddress = station.IpAddress,
            MacAddress = station.MacAddress,
            CurrentUserId = station.CurrentUserId,
            CurrentUsername = station.CurrentUser?.Username,
            SessionStartTime = station.SessionStartTime
        };
    }
}

public class StationDto
{
    public int StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public string StationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsActive { get; set; }
    public string Processor { get; set; } = string.Empty;
    public string GraphicsCard { get; set; } = string.Empty;
    public string Memory { get; set; } = string.Empty;
    public string Storage { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public int? CurrentUserId { get; set; }
    public string? CurrentUsername { get; set; }
    public DateTime? SessionStartTime { get; set; }
}

public class StationCreateRequest
{
    public string StationName { get; set; } = string.Empty;
    public string StationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public string? Processor { get; set; }
    public string? GraphicsCard { get; set; }
    public string? Memory { get; set; }
    public string? Storage { get; set; }
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
}

public class StationUpdateRequest : StationCreateRequest
{
    public bool IsAvailable { get; set; }
}

public class StartSessionRequest
{
    public int UserId { get; set; }
    public decimal HourlyRate { get; set; }
}

