using GenerativeCS.Interfaces;

namespace GenerativeCS.Utilities
{
    internal static class TokenLimiter
    {
        internal static void LimitTokens<TMessage>(ICollection<TMessage> messages, int? messageLimit, int? characterLimit) where TMessage : IChatMessage
        {
            if (messageLimit.HasValue)
            {
                messages = messages.Take(messageLimit.Value).ToList();
            }

            if (characterLimit.HasValue)
            {
                var currentCharacters = 0;
                for (var i = messages.Count - 1; i >= 0; i--)
                {
                    var currentMessage = messages.ElementAt(i);
                    var messageLength = currentMessage.Content?.Length ?? 0;

                    currentCharacters += messageLength;
                    if (currentCharacters > characterLimit.Value)
                    {
                        messages.Remove(currentMessage);
                    }
                }
            }
        }
    }
}
