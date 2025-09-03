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
    private readonly IDbContextFactory<GamingCafeContext> _contextFactory;

    public StationService(IDbContextFactory<GamingCafeContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<GameStation>> GetGameStationsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.GameStations
            .OrderBy(s => s.StationName)
            .ToListAsync();
    }

    public async Task<List<GameConsole>> GetGameConsolesAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.GameConsoles
            .OrderBy(c => c.ConsoleName)
            .ToListAsync();
    }

    public async Task<GameStation> GetGameStationByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var station = await context.GameStations.FindAsync(id);
        if (station == null)
            throw new ArgumentException($"Game station with ID {id} not found");
        
        return station;
    }

    public async Task<GameConsole> GetGameConsoleByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var console = await context.GameConsoles.FindAsync(id);
        if (console == null)
            throw new ArgumentException($"Game console with ID {id} not found");
        
        return console;
    }

    public async Task<GameStation> CreateGameStationAsync(GameStation station)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.GameStations.Add(station);
        await context.SaveChangesAsync();
        return station;
    }

    public async Task<GameConsole> CreateGameConsoleAsync(GameConsole console)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.GameConsoles.Add(console);
        await context.SaveChangesAsync();
        return console;
    }

    public async Task<GameStation> UpdateGameStationAsync(GameStation station)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.GameStations.Update(station);
        await context.SaveChangesAsync();
        return station;
    }

    public async Task<GameConsole> UpdateGameConsoleAsync(GameConsole console)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.GameConsoles.Update(console);
        await context.SaveChangesAsync();
        return console;
    }

    public async Task<bool> DeleteGameStationAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var station = await context.GameStations.FindAsync(id);
        if (station == null)
            return false;

        context.GameStations.Remove(station);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteGameConsoleAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var console = await context.GameConsoles.FindAsync(id);
        if (console == null)
            return false;

        context.GameConsoles.Remove(console);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<List<GameSession>> GetActiveSessionsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.GameSessions
            .Include(s => s.User)
            .Where(s => s.EndTime == null)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<List<ConsoleSession>> GetActiveConsoleSessionsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ConsoleSessions
            .Include(s => s.User)
            .Where(s => s.EndTime == null)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<bool> EndSessionAsync(int sessionId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var session = await context.GameSessions.FindAsync(sessionId);
        if (session == null || session.EndTime != null)
            return false;

        session.EndTime = DateTime.UtcNow;
        session.Status = SessionStatus.Completed;
        
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EndConsoleSessionAsync(int sessionId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var session = await context.ConsoleSessions.FindAsync(sessionId);
        if (session == null || session.EndTime != null)
            return false;

        session.EndTime = DateTime.UtcNow;
        session.Status = SessionStatus.Completed;
        
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<StationStats> GetStationStatsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var totalStations = await context.GameStations.CountAsync();
        var totalConsoles = await context.GameConsoles.CountAsync();
        var activeStations = await context.GameStations.CountAsync(s => !s.IsAvailable);
        var activeConsoles = await context.GameConsoles.CountAsync(c => c.Status == ConsoleStatus.InUse);
        var activeSessions = await context.GameSessions.CountAsync(s => s.EndTime == null);
        var activeConsoleSessions = await context.ConsoleSessions.CountAsync(s => s.EndTime == null);

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
