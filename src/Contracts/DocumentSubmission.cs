namespace Contracts;

public class DocumentSubmission
{
    public required string? Location { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
