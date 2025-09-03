using GamingCafe.Core.Models;
using GamingCafe.Data;
using Microsoft.EntityFrameworkCore;

namespace GamingCafe.API.Services;

public interface IStationService
{
    Task<IEnumerable<GameStation>> GetAllStationsAsync();
    Task<GameStation?> GetStationByIdAsync(int stationId);
    Task<GameStation> CreateStationAsync(GameStation station);
    Task<GameStation?> UpdateStationAsync(int stationId, GameStation station);
    Task<bool> DeleteStationAsync(int stationId);
    Task<bool> StartSessionAsync(int stationId, int userId, decimal hourlyRate);
    Task<bool> EndSessionAsync(int stationId);
    Task<IEnumerable<GameStation>> GetAvailableStationsAsync();
}

public class StationService : IStationService
{
    private readonly GamingCafeContext _context;

    public StationService(GamingCafeContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<GameStation>> GetAllStationsAsync()
    {
        return await _context.GameStations
            .Include(s => s.CurrentUser)
            .Where(s => s.IsActive)
            .ToListAsync();
    }

    public async Task<GameStation?> GetStationByIdAsync(int stationId)
    {
        return await _context.GameStations
            .Include(s => s.CurrentUser)
            .FirstOrDefaultAsync(s => s.StationId == stationId && s.IsActive);
    }

    public async Task<GameStation> CreateStationAsync(GameStation station)
    {
        station.CreatedAt = DateTime.UtcNow;
        _context.GameStations.Add(station);
        await _context.SaveChangesAsync();
        return station;
    }

    public async Task<GameStation?> UpdateStationAsync(int stationId, GameStation station)
    {
        var existingStation = await GetStationByIdAsync(stationId);
        if (existingStation == null) return null;

        existingStation.StationName = station.StationName;
        existingStation.StationType = station.StationType;
        existingStation.Description = station.Description;
        existingStation.HourlyRate = station.HourlyRate;
        existingStation.IsAvailable = station.IsAvailable;
        existingStation.Processor = station.Processor;
        existingStation.GraphicsCard = station.GraphicsCard;
        existingStation.Memory = station.Memory;
        existingStation.Storage = station.Storage;
        existingStation.IpAddress = station.IpAddress;
        existingStation.MacAddress = station.MacAddress;

        await _context.SaveChangesAsync();
        return existingStation;
    }

    public async Task<bool> DeleteStationAsync(int stationId)
    {
        var station = await GetStationByIdAsync(stationId);
        if (station == null) return false;

        station.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> StartSessionAsync(int stationId, int userId, decimal hourlyRate)
    {
        var station = await GetStationByIdAsync(stationId);
        if (station == null || !station.IsAvailable || station.CurrentUserId.HasValue)
            return false;

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        // Create new game session
        var session = new GameSession
        {
            UserId = userId,
            StationId = stationId,
            StartTime = DateTime.UtcNow,
            HourlyRate = hourlyRate,
            Status = SessionStatus.Active
        };

        _context.GameSessions.Add(session);

        // Update station status
        station.CurrentUserId = userId;
        station.SessionStartTime = DateTime.UtcNow;
        station.IsAvailable = false;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EndSessionAsync(int stationId)
    {
        var station = await GetStationByIdAsync(stationId);
        if (station == null || !station.CurrentUserId.HasValue)
            return false;

        // Find active session
        var session = await _context.GameSessions
            .FirstOrDefaultAsync(s => s.StationId == stationId && 
                                     s.UserId == station.CurrentUserId && 
                                     s.Status == SessionStatus.Active);

        if (session != null)
        {
            session.EndTime = DateTime.UtcNow;
            session.Status = SessionStatus.Completed;
            session.TotalCost = CalculateSessionCost(session.StartTime, session.EndTime.Value, session.HourlyRate);
        }

        // Update station status
        station.CurrentUserId = null;
        station.SessionStartTime = null;
        station.IsAvailable = true;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<GameStation>> GetAvailableStationsAsync()
    {
        return await _context.GameStations
            .Where(s => s.IsActive && s.IsAvailable && !s.CurrentUserId.HasValue)
            .ToListAsync();
    }

    private decimal CalculateSessionCost(DateTime startTime, DateTime endTime, decimal hourlyRate)
    {
        var duration = endTime.Subtract(startTime);
        var hours = (decimal)duration.TotalHours;
        return Math.Round(hours * hourlyRate, 2);
    }
}
