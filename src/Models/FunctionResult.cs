using System.Diagnostics.CodeAnalysis;
using GenerativeCS.Interfaces;

namespace GenerativeCS.Models
{
    public record FunctionResult : IFunctionResult
    {
        public FunctionResult() { }

        [SetsRequiredMembers]
        public FunctionResult(string name, object? result)
        {
            Name = name;
            Result = result;
        }

        public required string Name { get; set; }

        public object? Result { get; set; }
    }
}
