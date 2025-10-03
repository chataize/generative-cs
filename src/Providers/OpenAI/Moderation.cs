using System.Text.Json;
using System.Text.Json.Nodes;
using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.OpenAI;
using ChatAIze.GenerativeCS.Utilities;

namespace ChatAIze.GenerativeCS.Providers.OpenAI;

internal static class Moderation
{
    internal static async Task<ModerationResult> ModerateAsync(string text, string? apiKey, ModerationOptions? options = null, HttpClient? httpClient = null, CancellationToken cancellationToken = default)
    {
        options ??= new();
        httpClient ??= new()
        {
            Timeout = TimeSpan.FromMinutes(15)
        };

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            apiKey = options.ApiKey;
        }

        var request = CreateModerationRequest(text, options);

        using var response = await httpClient.RepeatPostAsJsonAsync("https://api.openai.com/v1/moderations", request, apiKey, options.MaxAttempts, cancellationToken);
        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseDocument = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

        return ParseModerationResponse(responseDocument);
    }

    private static JsonObject CreateModerationRequest(string text, ModerationOptions options)
    {
        var request = new JsonObject
        {
            { "input", text }
        };

        if (options.Model != ModerationModels.OpenAI.TextModerationLatest)
        {
            request.Add("model", options.Model);
        }

        return request;
    }

    private static ModerationResult ParseModerationResponse(JsonDocument response)
    {
        var result = response.RootElement.GetProperty("results")[0];
        var categories = result.GetProperty("categories");
        var categoryScores = result.GetProperty("category_scores");

        return new ModerationResult
        {
            IsFlagged = result.GetProperty("flagged").GetBoolean(),
            IsSexual = categories.GetProperty("sexual").GetBoolean(),
            SexualScore = categoryScores.GetProperty("sexual").GetDouble(),
            IsSexualMinors = categories.GetProperty("sexual/minors").GetBoolean(),
            SexualMinorsScore = categoryScores.GetProperty("sexual/minors").GetDouble(),
            IsHarassment = categories.GetProperty("harassment").GetBoolean(),
            HarassmentScore = categoryScores.GetProperty("harassment").GetDouble(),
            IsHarassmentThreatening = categories.GetProperty("harassment/threatening").GetBoolean(),
            HarassmentThreateningScore = categoryScores.GetProperty("harassment/threatening").GetDouble(),
            IsHate = categories.GetProperty("hate").GetBoolean(),
            HateScore = categoryScores.GetProperty("hate").GetDouble(),
            IsHateThreatening = categories.GetProperty("hate/threatening").GetBoolean(),
            HateThreateningScore = categoryScores.GetProperty("hate/threatening").GetDouble(),
            IsIllicit = categories.GetProperty("illicit").GetBoolean(),
            IllicitScore = categoryScores.GetProperty("illicit").GetDouble(),
            IsIllicitViolent = categories.GetProperty("illicit/violent").GetBoolean(),
            IllicitViolentScore = categoryScores.GetProperty("illicit/violent").GetDouble(),
            IsSelfHarm = categories.GetProperty("self-harm").GetBoolean(),
            SelfHarmScore = categoryScores.GetProperty("self-harm").GetDouble(),
            IsSelfHarmIntent = categories.GetProperty("self-harm/intent").GetBoolean(),
            SelfHarmIntentScore = categoryScores.GetProperty("self-harm/intent").GetDouble(),
            IsSelfHarmInstruction = categories.GetProperty("self-harm/instructions").GetBoolean(),
            SelfHarmInstructionScore = categoryScores.GetProperty("self-harm/instructions").GetDouble(),
            IsViolence = categories.GetProperty("violence").GetBoolean(),
            ViolenceScore = categoryScores.GetProperty("violence").GetDouble(),
            IsViolenceGraphic = categories.GetProperty("violence/graphic").GetBoolean(),
            ViolenceGraphicScore = categoryScores.GetProperty("violence/graphic").GetDouble(),
        };
    }
}
