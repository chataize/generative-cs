using ChatAIze.Abstractions.Chat;

namespace ChatAIze.GenerativeCS.Utilities;

/// <summary>
/// Helper utilities for preparing chat message payloads.
/// </summary>
internal static class MessageTools
{
    /// <summary>
    /// Adds a system message to the beginning of the message list when provided.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="messages">Message list to modify.</param>
    /// <param name="systemMessage">Optional system message.</param>
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

    /// <summary>
    /// Appends a time-aware system message to the end of the message list.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="messages">Message list to modify.</param>
    /// <param name="currentTime">Current time supplied by the caller.</param>
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

    /// <summary>
    /// Removes messages flagged as unsent or deleted via reflection-based markers.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="messages">Messages to filter.</param>
    internal static void RemoveDeletedMessages<TMessage, TFunctionCall, TFunctionResult>(IList<TMessage> messages)
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall
        where TFunctionResult : IFunctionResult
    {
        for (var i = messages.Count - 1; i >= 0; i--)
        {
            var currentMessage = messages[i];

            var isUnsentProperty = currentMessage.GetType().GetProperty("IsUnsent");
            if (isUnsentProperty is not null && (isUnsentProperty.GetValue(currentMessage) as bool?) == true)
            {
                // Honor lightweight “soft delete” flags that may exist on provider-specific message types.
                messages.RemoveAt(i);
                continue;
            }

            var isDeletedProperty = currentMessage.GetType().GetProperty("IsDeleted");
            if (isDeletedProperty is not null && (isDeletedProperty.GetValue(currentMessage) as bool?) == true)
            {
                // Honor lightweight “soft delete” flags that may exist on provider-specific message types.
                messages.RemoveAt(i);
                continue;
            }
        }
    }

    /// <summary>
    /// Reorders and trims messages according to the configured message and character limits.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="messages">Messages to mutate.</param>
    /// <param name="messageLimit">Optional message count limit.</param>
    /// <param name="characterLimit">Optional character count limit.</param>
    internal static void LimitTokens<TMessage, TFunctionCall, TFunctionResult>(List<TMessage> messages, int? messageLimit, int? characterLimit)
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>
        where TFunctionCall : IFunctionCall
        where TFunctionResult : IFunctionResult
    {
        // Preserve the ordering contract: explicitly pinned messages stay at the ends and everything
        // else keeps its relative order in the middle.
        var sortedMessages = messages.Where(m => m.PinLocation == PinLocation.Begin).ToList();

        sortedMessages.AddRange(messages.Where(m => m.PinLocation is PinLocation.None or PinLocation.Automatic));
        sortedMessages.AddRange(messages.Where(m => m.PinLocation == PinLocation.End));

        messages.Clear();
        messages.AddRange(sortedMessages);

        var excessiveMessages = messageLimit.HasValue ? messages.Count(m => m.Role != ChatRole.System) - messageLimit : 0;
        // Character trimming accounts for both user content and any function result payloads.
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
                if (message.FunctionCalls is not null)
                {
                    foreach (var functionCall in message.FunctionCalls)
                    {
                        if (string.IsNullOrWhiteSpace(functionCall.ToolCallId))
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

    /// <summary>
    /// Removes previous function calls that precede the last user message.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="messages">Messages to mutate.</param>
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
            if (currentMessage.FunctionCalls.Count > 0 || currentMessage.FunctionResult is not null)
            {
                // Drop tool calls/results that happened before the latest user input so the model
                // does not see stale tool interactions when continuing the conversation.
                messages.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Converts any system messages to user role messages while preserving content.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="messages">Messages to mutate.</param>
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

    /// <summary>
    /// Merges consecutive messages from the same sender into a single message.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="messages">Messages to mutate.</param>
    internal static void MergeMessages<TMessage, TFunctionCall, TFunctionResult>(IList<TMessage> messages)
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall
        where TFunctionResult : IFunctionResult
    {
        for (var i = messages.Count - 1; i >= 1; i--)
        {
            var previousMessage = messages[i - 1];
            var currentMessage = messages[i];

            if (previousMessage.Role == currentMessage.Role && previousMessage.UserName == currentMessage.UserName)
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
