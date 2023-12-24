using GenerativeCS.Interfaces;

namespace GenerativeCS.Utilities
{
    internal static class MessageTools
    {
        internal static void AddTimeInformation<T>(IList<T> messages) where T : IChatMessage, new()
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

        internal static List<T> LimitTokens<T>(List<T> messages, int? messageLimit, int? characterLimit) where T : IChatMessage
        {
            var sortedMessages = new List<T>(messages.Where(m => m.PinLocation == PinLocation.Begin));

            sortedMessages.AddRange(messages.Where(m => m.PinLocation == PinLocation.None || m.PinLocation == PinLocation.Automatic));
            sortedMessages.AddRange(messages.Where(m => m.PinLocation == PinLocation.End));

            var excessiveMessages = messageLimit.HasValue ? sortedMessages.Count - messageLimit : 0;
            var excessiveCharacters = characterLimit.HasValue ? sortedMessages.Sum(m => m.Content?.Length ?? 0) - characterLimit : 0;

            var messagesToRemove = new List<T>();
            foreach (var message in sortedMessages)
            {
                if (excessiveMessages <= 0 && excessiveCharacters <= 0)
                {
                    break;
                }

                if ((excessiveMessages >= 0 || excessiveCharacters >= 0) && message.PinLocation == PinLocation.None)
                {
                    messagesToRemove.Add(message);

                    excessiveMessages--;
                    excessiveCharacters -= message.Content?.Length ?? 0;
                }
            }

            sortedMessages.RemoveAll(messagesToRemove.Contains);
            return sortedMessages;
        }
    }
}
