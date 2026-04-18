namespace cffview.Models;

public class Stop
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class Line
{
    public string Id { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string LongName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public class Departure
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string StopId { get; set; } = string.Empty;
    public string StopName { get; set; } = string.Empty;
    public Line Line { get; set; } = new();
    public DateTime ScheduledTime { get; set; }
    public DateTime? RealTime { get; set; }
    public int DelayMinutes { get; set; }
    public string Destination { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public DepartureStatus Status { get; set; } = DepartureStatus.Scheduled;

    public DateTime DisplayTime => RealTime ?? ScheduledTime;
    public bool IsDelayed => DelayMinutes > 0;
}

public enum DepartureStatus
{
    Scheduled,
    RealTime,
    Delayed,
    Cancelled
}

public class Favorite
{
    public int Id { get; set; }
    public string StopId { get; set; } = string.Empty;
    public string StopName { get; set; } = string.Empty;
    public string? LineId { get; set; }
    public string? LineName { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}