namespace cffview.Models.DTOs;

public class StopRecordDto
{
    public string StopId { get; set; } = string.Empty;
    public string StopName { get; set; } = string.Empty;
    public string StopDesc { get; set; } = string.Empty;
    public double StopLat { get; set; }
    public double StopLon { get; set; }
    public string ZoneId { get; set; } = string.Empty;
    public string LocationType { get; set; } = string.Empty;
    public string ParentStation { get; set; } = string.Empty;
}

public class RouteRecordDto
{
    public string RouteId { get; set; } = string.Empty;
    public string AgencyId { get; set; } = string.Empty;
    public string RouteShortName { get; set; } = string.Empty;
    public string RouteLongName { get; set; } = string.Empty;
    public string RouteType { get; set; } = string.Empty;
    public string RouteColor { get; set; } = string.Empty;
    public string RouteTextColor { get; set; } = string.Empty;
}

public class TripRecordDto
{
    public string TripId { get; set; } = string.Empty;
    public string RouteId { get; set; } = string.Empty;
    public string ServiceId { get; set; } = string.Empty;
    public string TripHeadSign { get; set; } = string.Empty;
    public string DirectionId { get; set; } = string.Empty;
}

public class StopTimeRecordDto
{
    public int Id { get; set; }
    public string TripId { get; set; } = string.Empty;
    public string StopId { get; set; } = string.Empty;
    public string ArrivalTime { get; set; } = string.Empty;
    public string DepartureTime { get; set; } = string.Empty;
    public int StopSequence { get; set; }
    public string PickupType { get; set; } = string.Empty;
    public string DropOffType { get; set; } = string.Empty;
}

public class CalendarRecordDto
{
    public string ServiceId { get; set; } = string.Empty;
    public int Monday { get; set; }
    public int Tuesday { get; set; }
    public int Wednesday { get; set; }
    public int Thursday { get; set; }
    public int Friday { get; set; }
    public int Saturday { get; set; }
    public int Sunday { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
}