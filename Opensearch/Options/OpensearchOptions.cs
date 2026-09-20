namespace Opensearch.Options;

public sealed class OpensearchOptions
{
    public required string Url { get; init; }
    public required string Index { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
}