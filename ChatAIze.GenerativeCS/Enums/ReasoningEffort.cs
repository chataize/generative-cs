namespace ChatAIze.GenerativeCS.Enums;

/// <summary>
/// Indicates how much reasoning effort the model should apply to a task.
/// </summary>
public enum ReasoningEffort
{
    /// <summary>
    /// No reasoning effort applied; behave as a standard generation.
    /// </summary>
    None,

    /// <summary>
    /// Minimal reasoning effort.
    /// </summary>
    Minimal,

    /// <summary>
    /// Low reasoning effort.
    /// </summary>
    Low,

    /// <summary>
    /// Medium reasoning effort.
    /// </summary>
    Medium,

    /// <summary>
    /// High reasoning effort.
    /// </summary>
    High
}
