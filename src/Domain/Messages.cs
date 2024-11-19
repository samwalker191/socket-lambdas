namespace Domain;

public class SubscribeMessage
{
    public string? Topic { get; set; }
}

public class PublishMessage
{
    public string? Topic { get; set; }
    
    public string? Message { get; set; }
}