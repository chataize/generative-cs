using GenerativeCS.Interfaces;

namespace GenerativeCS.Utilities;

public static class TimeAwareness
{
    public static void AddCurrentTimeInfo<T>(IList<T> messages) where T : IChatMessage, new()
    {
        var firstMessage = messages.FirstOrDefault();
        if (firstMessage == null || firstMessage.Role != Enums.ChatRole.System)
        {
            firstMessage = new T
            {
                Role = Enums.ChatRole.System,
                Content = $"Current time (C# DateTimeOffset UTC): {DateTimeOffset.UtcNow}"
            };

            messages.Insert(0, firstMessage);
        }
        else
        {
            firstMessage.Content += $"\n\nCurrent time (C# DateTimeOffset UTC): {DateTimeOffset.UtcNow}";
        }
    }
}
