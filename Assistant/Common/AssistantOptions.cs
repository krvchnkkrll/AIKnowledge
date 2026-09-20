namespace Assistant.Common;

public sealed class AssistantOptions
{
    public required string Model { get; init; }
    public required string ApiKey { get; init; }
    public required string Url { get; init; }
}