namespace GenerativeCS.Interfaces;

public interface IFunctionResult
{
    string Id { get; set; }

    string Name { get; set; }

    object? Result { get; set; }
}
