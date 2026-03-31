using System.Text.Json;
using System.Text.Json.Serialization;
using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.Claude;

namespace ChatAIze.GenerativeCS.Providers.Claude;

/// <summary>
/// Implements moderation on top of Claude's Messages API following Anthropic's official moderation guidance.
/// </summary>
internal static class Moderation
{
    private const string ModerationPrompt = """
You are a content moderation classifier.

Assess the user's text for the categories below. Return only structured data that matches the requested schema.

Category guidance:
- Sexual: explicit sexual content involving adults.
- Sexual minors: sexual content involving minors, exploitation, or grooming.
- Harassment: abusive, insulting, demeaning, or targeted harassment.
- Harassment threatening: harassment that includes threats of harm.
- Hate: hateful content targeting protected characteristics.
- Hate threatening: hateful content that also threatens violence or serious harm.
- Illicit: assistance, planning, or encouragement for non-violent wrongdoing.
- Illicit violent: assistance, planning, or encouragement for violent wrongdoing.
- Self harm: promotion, endorsement, or discussion of self-harm as an action.
- Self harm intent: direct statements of intent to self-harm.
- Self harm instructions: instructions or advice for self-harm.
- Violence: realistic violence, violent threats, or encouragement of violence.
- Violence graphic: graphic, gory, or explicit depictions of violence.

Important rules:
- Distinguish figurative language, jokes, quotations, and benign context from genuine harmful intent.
- Keep scores between 0 and 1.
- Set is_flagged to true if any category boolean is true.
- Use low scores when the content is clearly benign.
""";

    /// <summary>
    /// Moderates a piece of text using Claude and returns a structured result compatible with the existing moderation model.
    /// </summary>
    /// <param name="text">Raw text to evaluate for safety issues.</param>
    /// <param name="apiKey">API key used for the request when not overridden by <paramref name="options"/>.</param>
    /// <param name="options">Optional moderation options; defaults are applied when not provided.</param>
    /// <param name="httpClient">HTTP client to use.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Structured moderation result.</returns>
    internal static async Task<ModerationResult> ModerateAsync(string text, string? apiKey, ModerationOptions? options = null, HttpClient? httpClient = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ModerationResult();
        }

        options ??= new();

        var completionOptions = new ChatCompletionOptions
        {
            Model = options.Model,
            ApiKey = options.ApiKey,
            MaxAttempts = options.MaxAttempts,
            MaxOutputTokens = options.MaxOutputTokens,
            Temperature = 0,
            ResponseType = typeof(ClaudeModerationResponse)
        };

        var response = await ChatCompletion.CompleteAsync<Chat, ChatMessage, FunctionCall, FunctionResult>(
            chat: new Chat(ModerationPrompt)
            {
                Messages =
                {
                    new ChatMessage(ChatRole.User, $"Classify this content for moderation:\n<content>{text}</content>")
                }
            },
            apiKey: apiKey,
            options: completionOptions,
            usageTracker: null,
            httpClient: httpClient,
            cancellationToken: cancellationToken);

        var moderation = JsonSerializer.Deserialize<ClaudeModerationResponse>(response, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Claude moderation returned an empty response.");

        return new ModerationResult
        {
            IsFlagged = moderation.IsFlagged,
            IsSexual = moderation.IsSexual,
            SexualScore = moderation.SexualScore,
            IsSexualMinors = moderation.IsSexualMinors,
            SexualMinorsScore = moderation.SexualMinorsScore,
            IsHarassment = moderation.IsHarassment,
            HarassmentScore = moderation.HarassmentScore,
            IsHarassmentThreatening = moderation.IsHarassmentThreatening,
            HarassmentThreateningScore = moderation.HarassmentThreateningScore,
            IsHate = moderation.IsHate,
            HateScore = moderation.HateScore,
            IsHateThreatening = moderation.IsHateThreatening,
            HateThreateningScore = moderation.HateThreateningScore,
            IsIllicit = moderation.IsIllicit,
            IllicitScore = moderation.IllicitScore,
            IsIllicitViolent = moderation.IsIllicitViolent,
            IllicitViolentScore = moderation.IllicitViolentScore,
            IsSelfHarm = moderation.IsSelfHarm,
            SelfHarmScore = moderation.SelfHarmScore,
            IsSelfHarmIntent = moderation.IsSelfHarmIntent,
            SelfHarmIntentScore = moderation.SelfHarmIntentScore,
            IsSelfHarmInstruction = moderation.IsSelfHarmInstruction,
            SelfHarmInstructionScore = moderation.SelfHarmInstructionScore,
            IsViolence = moderation.IsViolence,
            ViolenceScore = moderation.ViolenceScore,
            IsViolenceGraphic = moderation.IsViolenceGraphic,
            ViolenceGraphicScore = moderation.ViolenceGraphicScore
        };
    }

    private sealed record ClaudeModerationResponse
    {
        [JsonPropertyName("is_flagged")]
        public bool IsFlagged { get; init; }

        [JsonPropertyName("is_sexual")]
        public bool IsSexual { get; init; }

        [JsonPropertyName("sexual_score")]
        public double SexualScore { get; init; }

        [JsonPropertyName("is_sexual_minors")]
        public bool IsSexualMinors { get; init; }

        [JsonPropertyName("sexual_minors_score")]
        public double SexualMinorsScore { get; init; }

        [JsonPropertyName("is_harassment")]
        public bool IsHarassment { get; init; }

        [JsonPropertyName("harassment_score")]
        public double HarassmentScore { get; init; }

        [JsonPropertyName("is_harassment_threatening")]
        public bool IsHarassmentThreatening { get; init; }

        [JsonPropertyName("harassment_threatening_score")]
        public double HarassmentThreateningScore { get; init; }

        [JsonPropertyName("is_hate")]
        public bool IsHate { get; init; }

        [JsonPropertyName("hate_score")]
        public double HateScore { get; init; }

        [JsonPropertyName("is_hate_threatening")]
        public bool IsHateThreatening { get; init; }

        [JsonPropertyName("hate_threatening_score")]
        public double HateThreateningScore { get; init; }

        [JsonPropertyName("is_illicit")]
        public bool IsIllicit { get; init; }

        [JsonPropertyName("illicit_score")]
        public double IllicitScore { get; init; }

        [JsonPropertyName("is_illicit_violent")]
        public bool IsIllicitViolent { get; init; }

        [JsonPropertyName("illicit_violent_score")]
        public double IllicitViolentScore { get; init; }

        [JsonPropertyName("is_self_harm")]
        public bool IsSelfHarm { get; init; }

        [JsonPropertyName("self_harm_score")]
        public double SelfHarmScore { get; init; }

        [JsonPropertyName("is_self_harm_intent")]
        public bool IsSelfHarmIntent { get; init; }

        [JsonPropertyName("self_harm_intent_score")]
        public double SelfHarmIntentScore { get; init; }

        [JsonPropertyName("is_self_harm_instruction")]
        public bool IsSelfHarmInstruction { get; init; }

        [JsonPropertyName("self_harm_instruction_score")]
        public double SelfHarmInstructionScore { get; init; }

        [JsonPropertyName("is_violence")]
        public bool IsViolence { get; init; }

        [JsonPropertyName("violence_score")]
        public double ViolenceScore { get; init; }

        [JsonPropertyName("is_violence_graphic")]
        public bool IsViolenceGraphic { get; init; }

        [JsonPropertyName("violence_graphic_score")]
        public double ViolenceGraphicScore { get; init; }
    }
}
