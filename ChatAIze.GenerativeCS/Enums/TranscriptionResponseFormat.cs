namespace ChatAIze.GenerativeCS.Enums;

/// <summary>
/// Supported output formats for transcription and translation responses.
/// </summary>
public enum TranscriptionResponseFormat
{
    /// <summary>
    /// JSON response payload.
    /// </summary>
    Json,
    /// <summary>
    /// Plain text response payload.
    /// </summary>
    Text,
    /// <summary>
    /// SubRip subtitle format.
    /// </summary>
    SRT,
    /// <summary>
    /// Verbose JSON response payload including timestamps.
    /// </summary>
    VerboseJson,
    /// <summary>
    /// Web Video Text Tracks subtitle format.
    /// </summary>
    VTT
}
