namespace Tracker.Models;

public class SoapLoggingOptions
{
    public bool Enabled { get; set; }
    public bool ToFile { get; set; }
    public string? Path { get; set; }
}