namespace Domain.Constants;

public enum DefaultStatuses
{
    Created,
    InProgress,
    OnApproval,
    Completed
}

public static class DefaultStatusMapper
{
    public static string ToString(DefaultStatuses status)
    {
        return status switch
        {
            DefaultStatuses.Created    => "Cоздана",
            DefaultStatuses.InProgress => "В работе",
            DefaultStatuses.OnApproval => "На согласовании",
            DefaultStatuses.Completed  => "Завершена",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    public static DefaultStatuses ToEnum(string display)
    {
        return display.Trim().ToLowerInvariant() switch
        {
            "Создана"         => DefaultStatuses.Created,
            "В работе"        => DefaultStatuses.InProgress,
            "На согласовании" => DefaultStatuses.OnApproval,
            "Завершена"       => DefaultStatuses.Completed,
            _ => throw new ArgumentOutOfRangeException(nameof(display), display, null)
        };
    }
}