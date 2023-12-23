using System.Text.Json;

namespace GenerativeCS.Interfaces;

public interface IFunctionCall
{
    string Id { get; set; }
    
    string Name { get; set; }

    JsonElement Arguments { get; set; }
}
