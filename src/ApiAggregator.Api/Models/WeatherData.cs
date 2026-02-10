namespace ApiAggregator.Api.Models;

/// <summary>
/// Weather data from OpenWeatherMap API
/// </summary>
public class WeatherData
{
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double FeelsLike { get; set; }
    public int Humidity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public double WindSpeed { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Raw response from OpenWeatherMap API
/// </summary>
public class OpenWeatherMapResponse
{
    public OpenWeatherMain? Main { get; set; }
    public List<OpenWeatherDescription>? Weather { get; set; }
    public OpenWeatherWind? Wind { get; set; }
    public OpenWeatherSys? Sys { get; set; }
    public string? Name { get; set; }
}

public class OpenWeatherMain
{
    public double Temp { get; set; }
    public double Feels_like { get; set; }
    public int Humidity { get; set; }
}

public class OpenWeatherDescription
{
    public string? Description { get; set; }
    public string? Icon { get; set; }
}

public class OpenWeatherWind
{
    public double Speed { get; set; }
}

public class OpenWeatherSys
{
    public string? Country { get; set; }
}
