namespace ChatAIze.GenerativeCS.Models;

/// <summary>
/// Represents category-level flags and scores returned by the moderation endpoint.
/// </summary>
/// <remarks>
/// Boolean properties mirror the provider's category flags; score properties are confidence values between 0 and 1 where higher numbers indicate greater risk.
/// </remarks>
public record ModerationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the provider flagged the content for any category.
    /// </summary>
    public bool IsFlagged { get; set; }

    /// <summary>
    /// Gets or sets whether the provider flagged the content as sexual.
    /// </summary>
    public bool IsSexual { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0-1) for sexual content.
    /// </summary>
    public double SexualScore { get; set; }

    /// <summary>
    /// Gets or sets whether the provider flagged the content as sexual content involving minors.
    /// </summary>
    public bool IsSexualMinors { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0-1) for sexual content involving minors.
    /// </summary>
    public double SexualMinorsScore { get; set; }

    /// <summary>
    /// Gets or sets whether the provider flagged the content as harassment.
    /// </summary>
    public bool IsHarassment { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0-1) for harassment.
    /// </summary>
    public double HarassmentScore { get; set; }

    /// <summary>
    /// Gets or sets whether the provider flagged the content as threatening harassment.
    /// </summary>
    public bool IsHarassmentThreatening { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0-1) for threatening harassment.
    /// </summary>
    public double HarassmentThreateningScore { get; set; }

    /// <summary>
    /// Gets or sets whether the provider flagged the content for hate.
    /// </summary>
    public bool IsHate { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0-1) for hate content.
    /// </summary>
    public double HateScore { get; set; }

    /// <summary>
    /// Gets or sets whether the provider flagged the content for threatening hate.
    /// </summary>
    public bool IsHateThreatening { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0-1) for threatening hate content.
    /// </summary>
    public double HateThreateningScore { get; set; }

    /// <summary>
    /// Gets or sets whether the provider flagged the content as illicit.
    /// </summary>
    public bool IsIllicit { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0-1) for illicit content.
    /// </summary>
    public double IllicitScore { get; set; }

    /// <summary>
    /// Gets or sets whether the provider flagged the content as illicit and violent.
    /// </summary>
    public bool IsIllicitViolent { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0-1) for illicit violent content.
    /// </summary>
    public double IllicitViolentScore { get; set; }

    /// <summary>
    /// Gets or sets whether the provider flagged the content for self-harm.
    /// </summary>
    public bool IsSelfHarm { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0-1) for self-harm content.
    /// </summary>
    public double SelfHarmScore { get; set; }

    /// <summary>
    /// Gets or sets whether the provider flagged the content for self-harm intent.
    /// </summary>
    public bool IsSelfHarmIntent { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0-1) for self-harm intent.
    /// </summary>
    public double SelfHarmIntentScore { get; set; }

    /// <summary>
    /// Gets or sets whether the provider flagged the content for self-harm instructions.
    /// </summary>
    public bool IsSelfHarmInstruction { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0-1) for self-harm instructions.
    /// </summary>
    public double SelfHarmInstructionScore { get; set; }

    /// <summary>
    /// Gets or sets whether the provider flagged the content for violence.
    /// </summary>
    public bool IsViolence { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0-1) for violence.
    /// </summary>
    public double ViolenceScore { get; set; }

    /// <summary>
    /// Gets or sets whether the provider flagged the content for graphic violence.
    /// </summary>
    public bool IsViolenceGraphic { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0-1) for graphic violence.
    /// </summary>
    public double ViolenceGraphicScore { get; set; }
}
