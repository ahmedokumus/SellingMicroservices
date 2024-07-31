using Newtonsoft.Json;

namespace EventBus.Base.Events;

public class IntegrationEvent
{
    [JsonProperty]
    public string Id { get; private set; }
    
    [JsonProperty]
    public DateTime CreatedDate { get; private set; }

    public IntegrationEvent()
    {
        Id = Guid.NewGuid().ToString();
        CreatedDate = DateTime.Now;
    }

    [JsonConstructor]
    public IntegrationEvent(string id, DateTime createdDate)
    {
        Id = id;
        CreatedDate = createdDate;
    }
}