using System.Diagnostics.CodeAnalysis;
using GenerativeCS.Interfaces;

namespace GenerativeCS.Models
{
    public record FunctionResult : IFunctionResult
    {
        public FunctionResult() { }

        [SetsRequiredMembers]
        public FunctionResult(string id, string name, object? result)
        {
            Id = id;
            Name = name;
            Result = result;
        }

        public required string Id { get; set; }

        public required string Name { get; set; }

        public object? Result { get; set; }
    }
}
