using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Models;

namespace ChatAIze.GenerativeCS.Utilities;

internal static class MessageTools
{
    internal static void AddTimeInformation(List<ChatMessage> messages, DateTime currentTime)
    {
        var firstMessage = new ChatMessage(ChatRole.System, $"Current time: {currentTime}", PinLocation.Begin);
        messages.Insert(0, firstMessage);
    }

    internal static void LimitTokens(List<ChatMessage> messages, int? messageLimit, int? characterLimit)
    {
        var sortedMessages = new List<ChatMessage>(messages.Where(m => m.PinLocation == PinLocation.Begin));

        sortedMessages.AddRange(messages.Where(m => m.PinLocation is PinLocation.None or PinLocation.Automatic));
        sortedMessages.AddRange(messages.Where(m => m.PinLocation == PinLocation.End));

        messages.Clear();
        messages.AddRange(sortedMessages);

        var excessiveMessages = messageLimit.HasValue ? messages.Count - messageLimit : 0;
        var excessiveCharacters = characterLimit.HasValue ? messages.Sum(m => m.Content?.Length ?? 0) - characterLimit : 0;

        var messagesToRemove = new List<ChatMessage>();
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

        _ = messages.RemoveAll(messagesToRemove.Contains);
    }

    internal static void ReplaceSystemRole(List<ChatMessage> messages)
    {
        for (var i = messages.Count - 1; i >= 0; i--)
        {
            var currentMessage = messages[i];
            if (currentMessage.Role == ChatRole.System)
            {
                var updatedMessage = currentMessage with { };
                updatedMessage.Role = ChatRole.User;

                messages.RemoveAt(i);
                messages.Insert(i, updatedMessage);
            }
        }
    }

    internal static void MergeMessages(List<ChatMessage> messages)
    {
        for (var i = messages.Count - 1; i >= 1; i--)
        {
            var previousMessage = messages[i - 1];
            var currentMessage = messages[i];

            if (previousMessage.Role == currentMessage.Role && previousMessage.Name == currentMessage.Name)
            {
                var replacementMessage = previousMessage with { };
                replacementMessage.Content += $"\n\n{currentMessage.Content}";

                messages.RemoveAt(i - 1);
                messages.Insert(i - 1, replacementMessage);
                messages.RemoveAt(i);
            }
        }
    }
}
