using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Interfaces;

namespace ChatAIze.GenerativeCS.Utilities;

internal static class MessageTools
{
    internal static void AddDynamicSystemMessage<TMessage, TFunctionCall, TFunctionResult>(List<TMessage> messages, string? systemMessage)
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall
        where TFunctionResult : IFunctionResult
    {
        if (string.IsNullOrWhiteSpace(systemMessage))
        {
            return;
        }

        var firstMessage = new TMessage
        {
            Role = ChatRole.System,
            Content = systemMessage,
            PinLocation = PinLocation.Begin
        };

        messages.Insert(0, firstMessage);
    }

    internal static void AddTimeInformation<TMessage, TFunctionCall, TFunctionResult>(List<TMessage> messages, DateTime currentTime)
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall
        where TFunctionResult : IFunctionResult
    {
        var timeMessage = new TMessage
        {
            Role = ChatRole.System,
            Content = $"Current Time: '{currentTime:dddd, MMMM d, yyyy, HH:mm}'.",
            PinLocation = PinLocation.End
        };

        messages.Add(timeMessage);
    }

    internal static void RemoveDeletedMessages<TMessage, TFunctionCall, TFunctionResult>(IList<TMessage> messages)
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall
        where TFunctionResult : IFunctionResult
    {
        for (var i = messages.Count - 1; i >= 0; i--)
        {
            var currentMessage = messages[i];

            var isUnsentProperty = currentMessage.GetType().GetProperty("IsUnsent");
            if (isUnsentProperty != null && (isUnsentProperty.GetValue(currentMessage) as bool?) == true)
            {
                messages.RemoveAt(i);
                continue;
            }

            var isDeletedProperty = currentMessage.GetType().GetProperty("IsDeleted");
            if (isDeletedProperty != null && (isDeletedProperty.GetValue(currentMessage) as bool?) == true)
            {
                messages.RemoveAt(i);
                continue;
            }
        }
    }

    internal static void LimitTokens<TMessage, TFunctionCall, TFunctionResult>(List<TMessage> messages, int? messageLimit, int? characterLimit)
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>
        where TFunctionCall : IFunctionCall
        where TFunctionResult : IFunctionResult
    {
        var sortedMessages = messages.Where(m => m.PinLocation == PinLocation.Begin).ToList();

        sortedMessages.AddRange(messages.Where(m => m.PinLocation is PinLocation.None or PinLocation.Automatic));
        sortedMessages.AddRange(messages.Where(m => m.PinLocation == PinLocation.End));

        messages.Clear();
        messages.AddRange(sortedMessages);

        var excessiveMessages = messageLimit.HasValue ? messages.Count(m => m.Role != ChatRole.System) - messageLimit : 0;
        var excessiveCharacters = characterLimit.HasValue ? messages.Where(m => m.Role != ChatRole.System).Sum(m => m.Content?.Length ?? 0 + m.FunctionResult?.Value.Length ?? 0) - characterLimit : 0;

        var messagesToRemove = new List<TMessage>();
        foreach (var message in messages)
        {
            if (excessiveMessages <= 0 && excessiveCharacters <= 0)
            {
                break;
            }

            if (message.Role == ChatRole.System)
            {
                continue;
            }

            if ((excessiveMessages >= 0 || excessiveCharacters >= 0) && message.PinLocation == PinLocation.None)
            {
                messagesToRemove.Add(message);

                // If message is a function call, remove paired function result:
                if (message.FunctionCalls != null)
                {
                    foreach (var functionCall in message.FunctionCalls)
                    {
                        if (string.IsNullOrEmpty(functionCall.ToolCallId))
                        {
                            continue;
                        }

                        foreach (var message2 in messages)
                        {
                            if (message2.FunctionResult?.ToolCallId == functionCall.ToolCallId)
                            {
                                messagesToRemove.Add(message2);
                            }
                        }
                    }
                }

                excessiveMessages--;
                excessiveCharacters -= message.Content?.Length ?? 0 + message.FunctionResult?.Value.Length ?? 0;
            }
        }

        _ = messages.RemoveAll(messagesToRemove.Contains);
    }

    internal static void RemovePreviousFunctionCalls<TMessage, TFunctionCall, TFunctionResult>(List<TMessage> messages)
    where TMessage : IChatMessage<TFunctionCall, TFunctionResult>
    where TFunctionCall : IFunctionCall
    where TFunctionResult : IFunctionResult
    {
        var lastNonFunctionMessage = messages.LastOrDefault(m => m.Role == ChatRole.User);
        if (lastNonFunctionMessage is null)
        {
            return;
        }

        var lastNonFunctionMessageIndex = messages.IndexOf(lastNonFunctionMessage);
        for (var i = lastNonFunctionMessageIndex - 1; i >= 0; i--)
        {
            var currentMessage = messages[i];
            if (currentMessage.FunctionCalls.Count > 0 || currentMessage.FunctionResult != null)
            {
                messages.RemoveAt(i);
            }
        }
    }

    internal static void ReplaceSystemRole<TMessage, TFunctionCall, TFunctionResult>(IList<TMessage> messages)
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall
        where TFunctionResult : IFunctionResult
    {
        for (var i = messages.Count - 1; i >= 0; i--)
        {
            var currentMessage = messages[i];
            if (currentMessage.Role == ChatRole.System)
            {
                var updatedMessage = new TMessage
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

    internal static void MergeMessages<TMessage, TFunctionCall, TFunctionResult>(IList<TMessage> messages)
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall
        where TFunctionResult : IFunctionResult
    {
        for (var i = messages.Count - 1; i >= 1; i--)
        {
            var previousMessage = messages[i - 1];
            var currentMessage = messages[i];

            if (previousMessage.Role == currentMessage.Role && previousMessage.Author == currentMessage.Author)
            {
                var replacementMessage = new TMessage
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
