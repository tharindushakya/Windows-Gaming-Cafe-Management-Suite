using GamingCafe.Core.Models;
using GamingCafe.Data;
using Microsoft.EntityFrameworkCore;

namespace GamingCafe.Admin.Services;

public interface IStationService
{
    Task<List<GameStation>> GetGameStationsAsync();
    Task<List<GameConsole>> GetGameConsolesAsync();
    Task<GameStation> GetGameStationByIdAsync(int id);
    Task<GameConsole> GetGameConsoleByIdAsync(int id);
    Task<GameStation> CreateGameStationAsync(GameStation station);
    Task<GameConsole> CreateGameConsoleAsync(GameConsole console);
    Task<GameStation> UpdateGameStationAsync(GameStation station);
    Task<GameConsole> UpdateGameConsoleAsync(GameConsole console);
    Task<bool> DeleteGameStationAsync(int id);
    Task<bool> DeleteGameConsoleAsync(int id);
    Task<List<GameSession>> GetActiveSessionsAsync();
    Task<List<ConsoleSession>> GetActiveConsoleSessionsAsync();
    Task<bool> EndSessionAsync(int sessionId);
    Task<bool> EndConsoleSessionAsync(int sessionId);
    Task<StationStats> GetStationStatsAsync();
}

public class StationService : IStationService
{
    private readonly GamingCafeContext _context;

    public StationService(GamingCafeContext context)
    {
        _context = context;
    }

    public async Task<List<GameStation>> GetGameStationsAsync()
    {
        return await _context.GameStations
            .OrderBy(s => s.StationName)
            .ToListAsync();
    }

    public async Task<List<GameConsole>> GetGameConsolesAsync()
    {
        return await _context.GameConsoles
            .OrderBy(c => c.ConsoleName)
            .ToListAsync();
    }

    public async Task<GameStation> GetGameStationByIdAsync(int id)
    {
        var station = await _context.GameStations.FindAsync(id);
        if (station == null)
            throw new ArgumentException($"Game station with ID {id} not found");
        
        return station;
    }

    public async Task<GameConsole> GetGameConsoleByIdAsync(int id)
    {
        var console = await _context.GameConsoles.FindAsync(id);
        if (console == null)
            throw new ArgumentException($"Game console with ID {id} not found");
        
        return console;
    }

    public async Task<GameStation> CreateGameStationAsync(GameStation station)
    {
        _context.GameStations.Add(station);
        await _context.SaveChangesAsync();
        return station;
    }

    public async Task<GameConsole> CreateGameConsoleAsync(GameConsole console)
    {
        _context.GameConsoles.Add(console);
        await _context.SaveChangesAsync();
        return console;
    }

    public async Task<GameStation> UpdateGameStationAsync(GameStation station)
    {
        _context.GameStations.Update(station);
        await _context.SaveChangesAsync();
        return station;
    }

    public async Task<GameConsole> UpdateGameConsoleAsync(GameConsole console)
    {
        _context.GameConsoles.Update(console);
        await _context.SaveChangesAsync();
        return console;
    }

    public async Task<bool> DeleteGameStationAsync(int id)
    {
        var station = await _context.GameStations.FindAsync(id);
        if (station == null)
            return false;

        _context.GameStations.Remove(station);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteGameConsoleAsync(int id)
    {
        var console = await _context.GameConsoles.FindAsync(id);
        if (console == null)
            return false;

        _context.GameConsoles.Remove(console);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<GameSession>> GetActiveSessionsAsync()
    {
        return await _context.GameSessions
            .Include(s => s.User)
            .Where(s => s.EndTime == null)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<List<ConsoleSession>> GetActiveConsoleSessionsAsync()
    {
        return await _context.ConsoleSessions
            .Include(s => s.User)
            .Where(s => s.EndTime == null)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<bool> EndSessionAsync(int sessionId)
    {
        var session = await _context.GameSessions.FindAsync(sessionId);
        if (session == null || session.EndTime != null)
            return false;

        session.EndTime = DateTime.UtcNow;
        session.Status = SessionStatus.Completed;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EndConsoleSessionAsync(int sessionId)
    {
        var session = await _context.ConsoleSessions.FindAsync(sessionId);
        if (session == null || session.EndTime != null)
            return false;

        session.EndTime = DateTime.UtcNow;
        session.Status = SessionStatus.Completed;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<StationStats> GetStationStatsAsync()
    {
        var totalStations = await _context.GameStations.CountAsync();
        var totalConsoles = await _context.GameConsoles.CountAsync();
        var activeStations = await _context.GameStations.CountAsync(s => !s.IsAvailable);
        var activeConsoles = await _context.GameConsoles.CountAsync(c => c.Status == ConsoleStatus.InUse);
        var activeSessions = await _context.GameSessions.CountAsync(s => s.EndTime == null);
        var activeConsoleSessions = await _context.ConsoleSessions.CountAsync(s => s.EndTime == null);

        return new StationStats
        {
            TotalStations = totalStations,
            TotalConsoles = totalConsoles,
            ActiveStations = activeStations,
            ActiveConsoles = activeConsoles,
            ActiveSessions = activeSessions + activeConsoleSessions,
            AvailableStations = totalStations - activeStations,
            AvailableConsoles = totalConsoles - activeConsoles
        };
    }
}

public class StationStats
{
    public int TotalStations { get; set; }
    public int TotalConsoles { get; set; }
    public int ActiveStations { get; set; }
    public int ActiveConsoles { get; set; }
    public int ActiveSessions { get; set; }
    public int AvailableStations { get; set; }
    public int AvailableConsoles { get; set; }
}
