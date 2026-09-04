using System.ComponentModel.DataAnnotations;

namespace ShitpostBot.ConversationBackprocessor;

public sealed record ConversationBackprocessorOptions
{
    [Required]
    public required string InputPath { get; init; }
}
