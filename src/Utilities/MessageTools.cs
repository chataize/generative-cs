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

        internal static void LimitTokens<T>(List<T> messages, int? messageLimit, int? characterLimit) where T : IChatMessage
        {
            var sortedMessages = new List<T>(messages.Where(m => m.PinLocation == PinLocation.Begin));

            sortedMessages.AddRange(messages.Where(m => m.PinLocation == PinLocation.None || m.PinLocation == PinLocation.Automatic));
            sortedMessages.AddRange(messages.Where(m => m.PinLocation == PinLocation.End));

            messages.Clear();
            messages.AddRange(sortedMessages);

            var excessiveMessages = messageLimit.HasValue ? messages.Count - messageLimit : 0;
            var excessiveCharacters = characterLimit.HasValue ? messages.Sum(m => m.Content?.Length ?? 0) - characterLimit : 0;

            var messagesToRemove = new List<T>();
            foreach (var message in messages)
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

            messages.RemoveAll(messagesToRemove.Contains);
        }

        internal static void MergeMessages<T>(List<T> messages) where T : IChatMessage
        {
            for (int i = messages.Count - 1; i >= 1; i--)
            {
                var previousMessage = messages[i - 1];
                var currentMessage = messages[i];

                if (previousMessage.Role == currentMessage.Role && previousMessage.Author == currentMessage.Author)
                {
                    previousMessage.Content += $"\n\n{currentMessage.Content}";
                    messages.RemoveAt(i);
                }
            }
        }
    }
}
