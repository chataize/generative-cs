namespace GenerativeCS.Interfaces;

public interface IChatFunction
{
    string? Name { get; set; }

    string? Description { get; set; }

    Delegate? Function { get; set; }
}
