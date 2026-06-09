namespace Wup.Works.SiteBroker.Client.Models.Payload;

public class GenericPayloadDto
{
    /// <summary>
    /// The status of the payload or event.
    /// </summary>
    public int Status { get; set;  }

    /// <summary>
    /// AdditionalProperties key/value properties.
    /// </summary>
    public IDictionary<string, string> AdditionalProperties { get; set; }
}