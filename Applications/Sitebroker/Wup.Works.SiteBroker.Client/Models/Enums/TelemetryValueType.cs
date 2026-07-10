namespace Wup.Works.SiteBroker.Client.Models.Enums;

/// <summary>
/// The type of a telemetry value. Only <c>number</c> or <c>string</c> are valid; serialized
/// lower-case on the wire (<c>"number"</c> / <c>"string"</c>).
/// </summary>
/// <remarks>
/// Named <c>TelemetryValueType</c> rather than <c>ValueType</c> to avoid clashing with
/// <see cref="System.ValueType"/> under implicit usings.
/// </remarks>
public enum TelemetryValueType
{
    Number,
    String
}
