namespace ToolGsm.Models;

public class Logging
{
    public DateTime CreateLogging { get; set; } = DateTime.Now;
    public string Message { get; set; }

    public Logging(string message)
    {
        Message = $"[{CreateLogging}] {message}";
    }
}