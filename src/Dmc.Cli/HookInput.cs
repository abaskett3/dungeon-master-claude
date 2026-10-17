using System.Text.Json;

namespace Dmc;

/// <summary>
/// Reading the JSON object Claude Code passes to a hook on standard input, for the commands that run in a hook
/// (<c>context-check</c>, <c>redraw</c>). One reader, so they agree on what counts as a field.
/// </summary>
public static class HookInput
{
    /// <summary>
    /// One field of the hook's input. Empty or blank input counts as <c>{}</c>.
    /// </summary>
    /// <param name="input">The hook's JSON.</param>
    /// <param name="name">The field's name.</param>
    /// <returns>
    /// The field's JSON kind, and its text when it is a string. <see cref="JsonValueKind.Undefined"/> when the input is
    /// not an object or has no such field.
    /// </returns>
    /// <exception cref="JsonException">The input is not valid JSON. A command that must never fail lets it through to <see cref="CommandLine"/>, which turns it into silence; one that can carry on catches it.</exception>
    public static (JsonValueKind Kind, string? Text) Field(string input, string name)
    {
        using var hook = JsonDocument.Parse(string.IsNullOrWhiteSpace(input) ? "{}" : input);
        if (hook.RootElement.ValueKind != JsonValueKind.Object || !hook.RootElement.TryGetProperty(name, out var value))
            return (JsonValueKind.Undefined, null);
        return (value.ValueKind, value.ValueKind == JsonValueKind.String ? value.GetString() : null);
    }
}
