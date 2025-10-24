namespace TouchNStars.Server.Models;

public class ApiResponse
{
    public bool Success { get; set; }
    public object Response { get; set; }
    public string Error { get; set; }
    public int StatusCode { get; set; }
    public string Type { get; set; }
}
