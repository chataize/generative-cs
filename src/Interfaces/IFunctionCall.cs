using System.Text.Json;

namespace GenerativeCS.Interfaces;

public interface IFunctionCall
{
    string Name { get; set; }

    JsonElement Arguments { get; set; }
}
