namespace ChatAIze.GenerativeCS.Models;

/// <summary>
/// Represents category-level flags and scores returned by the moderation endpoint.
/// </summary>
public record ModerationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether any category flagged the content.
    /// </summary>
    public bool IsFlagged { get; set; }

    /// <summary>
    /// Gets or sets whether the content is sexual.
    /// </summary>
    public bool IsSexual { get; set; }

    /// <summary>
    /// Gets or sets the score for sexual content.
    /// </summary>
    public double SexualScore { get; set; }

    /// <summary>
    /// Gets or sets whether the content is sexual and involves minors.
    /// </summary>
    public bool IsSexualMinors { get; set; }

    /// <summary>
    /// Gets or sets the score for sexual content involving minors.
    /// </summary>
    public double SexualMinorsScore { get; set; }

    /// <summary>
    /// Gets or sets whether the content is harassment.
    /// </summary>
    public bool IsHarassment { get; set; }

    /// <summary>
    /// Gets or sets the score for harassment.
    /// </summary>
    public double HarassmentScore { get; set; }

    /// <summary>
    /// Gets or sets whether the content is threatening harassment.
    /// </summary>
    public bool IsHarassmentThreatening { get; set; }

    /// <summary>
    /// Gets or sets the score for threatening harassment.
    /// </summary>
    public double HarassmentThreateningScore { get; set; }

    /// <summary>
    /// Gets or sets whether the content expresses hate.
    /// </summary>
    public bool IsHate { get; set; }

    /// <summary>
    /// Gets or sets the score for hate content.
    /// </summary>
    public double HateScore { get; set; }

    /// <summary>
    /// Gets or sets whether the content is threatening hate.
    /// </summary>
    public bool IsHateThreatening { get; set; }

    /// <summary>
    /// Gets or sets the score for threatening hate content.
    /// </summary>
    public double HateThreateningScore { get; set; }

    /// <summary>
    /// Gets or sets whether the content is illicit.
    /// </summary>
    public bool IsIllicit { get; set; }

    /// <summary>
    /// Gets or sets the score for illicit content.
    /// </summary>
    public double IllicitScore { get; set; }

    /// <summary>
    /// Gets or sets whether the content is illicit and violent.
    /// </summary>
    public bool IsIllicitViolent { get; set; }

    /// <summary>
    /// Gets or sets the score for illicit violent content.
    /// </summary>
    public double IllicitViolentScore { get; set; }

    /// <summary>
    /// Gets or sets whether the content contains self-harm.
    /// </summary>
    public bool IsSelfHarm { get; set; }

    /// <summary>
    /// Gets or sets the score for self-harm content.
    /// </summary>
    public double SelfHarmScore { get; set; }

    /// <summary>
    /// Gets or sets whether the content indicates self-harm intent.
    /// </summary>
    public bool IsSelfHarmIntent { get; set; }

    /// <summary>
    /// Gets or sets the score for self-harm intent.
    /// </summary>
    public double SelfHarmIntentScore { get; set; }

    /// <summary>
    /// Gets or sets whether the content contains self-harm instructions.
    /// </summary>
    public bool IsSelfHarmInstruction { get; set; }

    /// <summary>
    /// Gets or sets the score for self-harm instructions.
    /// </summary>
    public double SelfHarmInstructionScore { get; set; }

    /// <summary>
    /// Gets or sets whether the content contains violence.
    /// </summary>
    public bool IsViolence { get; set; }

    /// <summary>
    /// Gets or sets the score for violence.
    /// </summary>
    public double ViolenceScore { get; set; }

    /// <summary>
    /// Gets or sets whether the content contains graphic violence.
    /// </summary>
    public bool IsViolenceGraphic { get; set; }

    /// <summary>
    /// Gets or sets the score for graphic violence.
    /// </summary>
    public double ViolenceGraphicScore { get; set; }
}
