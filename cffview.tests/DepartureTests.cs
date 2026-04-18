using cffview.Models;
using Xunit;

namespace cffview.tests;

public class DepartureTests
{
    [Fact]
    public void DisplayTime_ReturnsRealTime_WhenRealTimeIsSet()
    {
        var departure = new Departure
        {
            ScheduledTime = new DateTime(2026, 4, 18, 10, 0, 0),
            RealTime = new DateTime(2026, 4, 18, 10, 5, 0)
        };

        Assert.Equal(new DateTime(2026, 4, 18, 10, 5, 0), departure.DisplayTime);
    }

    [Fact]
    public void DisplayTime_ReturnsScheduledTime_WhenRealTimeIsNull()
    {
        var departure = new Departure
        {
            ScheduledTime = new DateTime(2026, 4, 18, 10, 0, 0),
            RealTime = null
        };

        Assert.Equal(new DateTime(2026, 4, 18, 10, 0, 0), departure.DisplayTime);
    }

    [Fact]
    public void IsDelayed_ReturnsTrue_WhenDelayMinutesGreaterThanZero()
    {
        var departure = new Departure { DelayMinutes = 5 };

        Assert.True(departure.IsDelayed);
    }

    [Fact]
    public void IsDelayed_ReturnsFalse_WhenDelayMinutesIsZero()
    {
        var departure = new Departure { DelayMinutes = 0 };

        Assert.False(departure.IsDelayed);
    }
}

public class StopTests
{
    [Fact]
    public void Stop_DefaultValues_AreEmpty()
    {
        var stop = new Stop();

        Assert.Equal(string.Empty, stop.Id);
        Assert.Equal(string.Empty, stop.Name);
    }

    [Fact]
    public void Stop_WithValues_CanBeCreated()
    {
        var stop = new Stop
        {
            Id = "8501120",
            Name = "Lausanne",
            Latitude = 46.516795,
            Longitude = 6.629087
        };

        Assert.Equal("8501120", stop.Id);
        Assert.Equal("Lausanne", stop.Name);
        Assert.Equal(46.516795, stop.Latitude);
    }
}

public class LineTests
{
    [Fact]
    public void Line_DefaultValues_AreEmpty()
    {
        var line = new Line();

        Assert.Equal(string.Empty, line.Id);
        Assert.Equal(string.Empty, line.ShortName);
    }

    [Fact]
    public void Line_WithValues_CanBeCreated()
    {
        var line = new Line
        {
            Id = "EC",
            ShortName = "EC",
            LongName = "EuroCity",
            Color = "#004D95"
        };

        Assert.Equal("EC", line.ShortName);
        Assert.Equal("#004D95", line.Color);
    }
}

public class FavoriteTests
{
    [Fact]
    public void Favorite_DefaultDisplayOrder_IsZero()
    {
        var favorite = new Favorite();

        Assert.Equal(0, favorite.DisplayOrder);
    }

    [Fact]
    public void Favorite_CreatedAt_IsSet()
    {
        var before = DateTime.Now;
        var favorite = new Favorite();
        var after = DateTime.Now;

        Assert.InRange(favorite.CreatedAt, before, after);
    }
}