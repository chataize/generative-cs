namespace ChatAIze.GenerativeCS.Models;

public record ModerationResult
{
    public bool IsFlagged { get; set; }

    public bool IsSexual { get; set; }

    public bool IsSexualMinors { get; set; }

    public bool IsViolence { get; set; }

    public bool IsViolenceGraphic { get; set; }

    public bool IsHate { get; set; }

    public bool IsHateThreatening { get; set; }

    public bool IsHarassmentThreatening { get; set; }

    public bool IsHarassment { get; set; }

    public bool IsSelfHarm { get; set; }

    public bool IsSelfHarmIntent { get; set; }

    public bool IsSelfHarmInstruction { get; set; }

    public double SexualScore { get; set; }

    public double SexualMinorsScore { get; set; }

    public double ViolenceScore { get; set; }

    public double ViolenceGraphicScore { get; set; }

    public double HateScore { get; set; }

    public double HateThreateningScore { get; set; }

    public double HarassmentThreateningScore { get; set; }

    public double HarassmentScore { get; set; }

    public double SelfHarmScore { get; set; }

    public double SelfHarmIntentScore { get; set; }

    public double SelfHarmInstructionScore { get; set; }
}
