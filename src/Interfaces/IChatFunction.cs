namespace GenerativeCS.Interfaces;

public interface IChatFunction
{
    string? Name { get; set; }

    string? Description { get; set; }

    bool RequireConfirmation { get; set; }

    Delegate? Function { get; set; }
}
