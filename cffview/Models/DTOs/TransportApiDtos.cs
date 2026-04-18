namespace cffview.Models.DTOs;

public class LocationSearchResponse
{
    public List<LocationDto>? Stations { get; set; }
    public List<LocationDto>? Locations { get; set; }
}

public class LocationDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public CoordinateDto? Coordinate { get; set; }
}

public class CoordinateDto
{
    public double? X { get; set; }
    public double? Y { get; set; }
}

public class StationBoardResponse
{
    public StationDto? Station { get; set; }
    public List<StationBoardItemDto>? Stationboard { get; set; }
}

public class StationDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public CoordinateDto? Coordinate { get; set; }
}

public class StationBoardItemDto
{
    public StopDetailDto? Stop { get; set; }
    public string? Name { get; set; }
    public string? Category { get; set; }
    public string? Number { get; set; }
    public string? Operator { get; set; }
    public string? To { get; set; }
    public string? Departure { get; set; }
    public long DepartureTimestamp { get; set; }
    public int? Delay { get; set; }
    public string? Platform { get; set; }
}

public class StopDetailDto
{
    public StationDto? Station { get; set; }
}