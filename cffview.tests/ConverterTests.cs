using System.Windows;
using cffview.Converters;
using Xunit;

namespace cffview.tests;

public class ConverterTests
{
    [Theory]
    [InlineData(true, Visibility.Visible)]
    [InlineData(false, Visibility.Collapsed)]
    public void BoolToVisibilityConverter_True_ReturnsVisible(bool input, Visibility expected)
    {
        var converter = new BoolToVisibilityConverter();
        var result = converter.Convert(input, typeof(Visibility), null, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, Visibility.Collapsed)]
    [InlineData(false, Visibility.Visible)]
    public void BoolToVisibilityConverter_Inverted_ReturnsVisible(bool input, Visibility expected)
    {
        var converter = new BoolToVisibilityConverter();
        var result = converter.Convert(input, typeof(Visibility), "Invert", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TimeFormatConverter_DateTime_ReturnsHHmm()
    {
        var converter = new TimeFormatConverter();
        var dt = new DateTime(2026, 4, 18, 14, 30, 0);
        var result = converter.Convert(dt, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal("14:30", result);
    }

    [Fact]
    public void TimeFormatConverter_Null_ReturnsDefault()
    {
        var converter = new TimeFormatConverter();
        var result = converter.Convert(null, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal("--:--", result);
    }

    [Theory]
    [InlineData("#EE1C25")]
    [InlineData("#004D95")]
    [InlineData("#FFFFFF")]
    [InlineData("invalid")]
    public void LineColorConverter_HexColor_ReturnsColor(string hex)
    {
        // Note: The converter returns a SolidColorBrush, we just verify it doesn't throw
        var converter = new LineColorConverter();
        var result = converter.Convert(hex, typeof(System.Windows.Media.Brush), null, System.Globalization.CultureInfo.InvariantCulture);
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(0, "Gray")]
    [InlineData(5, "Red")]
    [InlineData(-2, "Gray")]
    public void DelayToColorConverter_ReturnsColor(int delay, string expectedColor)
    {
        var converter = new DelayToColorConverter();
        var result = converter.Convert(delay, typeof(System.Windows.Media.Brush), null, System.Globalization.CultureInfo.InvariantCulture);
        
        if (expectedColor == "Red")
            Assert.Equal(System.Windows.Media.Color.FromRgb(238, 28, 37), ((System.Windows.Media.SolidColorBrush)result).Color);
        else
            Assert.Equal(System.Windows.Media.Colors.Gray, ((System.Windows.Media.SolidColorBrush)result).Color);
    }

    [Fact]
    public void NullToVisibilityConverter_NotNull_ReturnsVisible()
    {
        var converter = new NullToVisibilityConverter();
        var result = converter.Convert("some value", typeof(Visibility), null, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(Visibility.Visible, result);
    }

    [Fact]
    public void NullToVisibilityConverter_Null_ReturnsCollapsed()
    {
        var converter = new NullToVisibilityConverter();
        var result = converter.Convert(null, typeof(Visibility), null, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void NullToVisibilityConverter_Inverted_ReversesResult()
    {
        var converter = new NullToVisibilityConverter();
        var result = converter.Convert("value", typeof(Visibility), "Invert", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(Visibility.Collapsed, result);
    }
}