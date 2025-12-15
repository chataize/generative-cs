namespace ChatAIze.GenerativeCS.Enums;

/// <summary>
/// Determines the container format for generated speech audio.
/// </summary>
public enum VoiceResponseFormat
{
    /// <summary>
    /// Use the provider default format.
    /// </summary>
    Default,
    /// <summary>
    /// MPEG-1 Audio Layer III.
    /// </summary>
    MP3,
    /// <summary>
    /// Opus encoded audio.
    /// </summary>
    Opus,
    /// <summary>
    /// Advanced Audio Coding.
    /// </summary>
    AAC,
    /// <summary>
    /// Free Lossless Audio Codec.
    /// </summary>
    FLAC
}
