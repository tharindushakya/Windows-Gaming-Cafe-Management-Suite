using GamingCafe.Core.DTOs;
using System;

namespace GamingCafe.Admin.Services;

public class CurrentUserState
{
    private UserDto? _user;
    private string? _token;

    public UserDto? User => _user;
    public string? Token => _token;

    public event Action? OnChange;

    public void Set(UserDto user, string token)
    {
        _user = user;
        _token = token;
        OnChange?.Invoke();
    }

    public void Clear()
    {
        _user = null;
        _token = null;
        OnChange?.Invoke();
    }
}
