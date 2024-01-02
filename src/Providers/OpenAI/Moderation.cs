using System.Text.Json;
using System.Text.Json.Nodes;
using GenerativeCS.Constants;
using GenerativeCS.Models;
using GenerativeCS.Options.OpenAI;
using GenerativeCS.Utilities;

namespace GenerativeCS.Providers.OpenAI;

internal static class Moderation
{
    internal static async Task<ModerationResult> ModerateAsync(string text, string apiKey, ModerationOptions? options = null, HttpClient? httpClient = null, CancellationToken cancellationToken = default)
    {
        options ??= new();
        httpClient ??= new();

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

        if (options.Model != ModerationModels.TEXT_MODERATION_LATEST)
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
            IsSexualMinors = categories.GetProperty("sexual/minors").GetBoolean(),
            IsViolence = categories.GetProperty("violence").GetBoolean(),
            IsViolenceGraphic = categories.GetProperty("violence/graphic").GetBoolean(),
            IsHate = categories.GetProperty("hate").GetBoolean(),
            IsHateThreatening = categories.GetProperty("hate/threatening").GetBoolean(),
            IsHarassmentThreatening = categories.GetProperty("harassment/threatening").GetBoolean(),
            IsHarassment = categories.GetProperty("harassment").GetBoolean(),
            IsSelfHarm = categories.GetProperty("self-harm").GetBoolean(),
            IsSelfHarmIntent = categories.GetProperty("self-harm/intent").GetBoolean(),
            IsSelfHarmInstruction = categories.GetProperty("self-harm/instructions").GetBoolean(),
            SexualScore = categoryScores.GetProperty("sexual").GetDouble(),
            SexualMinorsScore = categoryScores.GetProperty("sexual/minors").GetDouble(),
            ViolenceScore = categoryScores.GetProperty("violence").GetDouble(),
            ViolenceGraphicScore = categoryScores.GetProperty("violence/graphic").GetDouble(),
            HateScore = categoryScores.GetProperty("hate").GetDouble(),
            HateThreateningScore = categoryScores.GetProperty("hate/threatening").GetDouble(),
            HarassmentThreateningScore = categoryScores.GetProperty("harassment/threatening").GetDouble(),
            HarassmentScore = categoryScores.GetProperty("harassment").GetDouble(),
            SelfHarmScore = categoryScores.GetProperty("self-harm").GetDouble(),
            SelfHarmIntentScore = categoryScores.GetProperty("self-harm/intent").GetDouble(),
            SelfHarmInstructionScore = categoryScores.GetProperty("self-harm/instructions").GetDouble()
        };
    }
}
