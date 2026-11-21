namespace Relay.Api.Cards;

public static class CardText
{
    public const int MaxTitleLength = 120;

    public const int MaxDescriptionLength = 2000;

    public static bool TryNormalize(
        string? title,
        string? description,
        out string normalizedTitle,
        out string? normalizedDescription,
        out string? error)
    {
        normalizedTitle = title?.Trim() ?? string.Empty;

        var trimmedDescription = description?.Trim();
        normalizedDescription = string.IsNullOrEmpty(trimmedDescription) ? null : trimmedDescription;

        if (normalizedTitle.Length == 0)
        {
            error = "Title is required.";
            return false;
        }

        if (normalizedTitle.Length > MaxTitleLength)
        {
            error = $"Title must be {MaxTitleLength} characters or fewer.";
            return false;
        }

        if (normalizedDescription is not null && normalizedDescription.Length > MaxDescriptionLength)
        {
            error = $"Description must be {MaxDescriptionLength} characters or fewer.";
            return false;
        }

        error = null;

        return true;
    }
}
