namespace TodoApi.DTOs;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresInSeconds { get; set; }
    public AuthUserResponse User { get; set; } = new();
}
