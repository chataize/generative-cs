using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Interfaces;

namespace ChatAIze.GenerativeCS.Utilities;

internal static class MessageTools
{
    internal static void AddTimeInformation<T>(List<T> messages, DateTime currentTime) where T : IChatMessage, new()
    {
        var firstMessage = new T
        {
            Role = ChatRole.System,
            Content = $"Current time: {currentTime}",
            PinLocation = PinLocation.Begin
        };

        messages.Insert(0, firstMessage);
    }

    internal static void LimitTokens<T>(List<T> messages, int? messageLimit, int? characterLimit) where T : IChatMessage, new()
    {
        var sortedMessages = messages.Where(m => m.PinLocation == PinLocation.Begin).ToList();

        sortedMessages.AddRange(messages.Where(m => m.PinLocation is PinLocation.None or PinLocation.Automatic));
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

                // If message is a function call, remove paired function result:
                if (message.FunctionCalls != null)
                {
                    foreach (var functionCall in message.FunctionCalls)
                    {
                        if (string.IsNullOrEmpty(functionCall.Id))
                        {
                            continue;
                        }

                        foreach (var message2 in messages)
                        {
                            if (message2.FunctionResult?.Id == functionCall.Id)
                            {
                                messagesToRemove.Add(message2);
                            }
                        }
                    }
                }

                excessiveMessages--;
                excessiveCharacters -= message.Content?.Length ?? 0;
            }
        }

        _ = messages.RemoveAll(messagesToRemove.Contains);
    }

    internal static void ReplaceSystemRole<T>(IList<T> messages) where T : IChatMessage, new()
    {
        for (var i = messages.Count - 1; i >= 0; i--)
        {
            var currentMessage = messages[i];
            if (currentMessage.Role == ChatRole.System)
            {
                var updatedMessage = new T
                {
                    Role = ChatRole.User,
                    Content = currentMessage.Content,
                    FunctionCalls = currentMessage.FunctionCalls,
                    FunctionResult = currentMessage.FunctionResult,
                    PinLocation = currentMessage.PinLocation
                };

                messages.RemoveAt(i);
                messages.Insert(i, updatedMessage);
            }
        }
    }

    internal static void MergeMessages<T>(IList<T> messages) where T : IChatMessage, new()
    {
        for (var i = messages.Count - 1; i >= 1; i--)
        {
            var previousMessage = messages[i - 1];
            var currentMessage = messages[i];

            if (previousMessage.Role == currentMessage.Role && previousMessage.Name == currentMessage.Name)
            {
                var replacementMessage = new T
                {
                    Role = previousMessage.Role,
                    Content = previousMessage.Content,
                    FunctionCalls = previousMessage.FunctionCalls,
                    FunctionResult = previousMessage.FunctionResult,
                    PinLocation = previousMessage.PinLocation
                };

                replacementMessage.Content += $"\n\n{currentMessage.Content}";

                messages.RemoveAt(i - 1);
                messages.Insert(i - 1, replacementMessage);
                messages.RemoveAt(i);
            }
        }
    }
}
