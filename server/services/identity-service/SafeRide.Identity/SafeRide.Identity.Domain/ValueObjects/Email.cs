using System.Text.RegularExpressions;

namespace SafeRide.Identity.Domain.ValueObjects;

public sealed record Email
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Email cannot be empty.");

        var normalized = input.Trim().ToLowerInvariant();

        if (!Regex.IsMatch(normalized, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            throw new ArgumentException($"'{input}' is not a valid email address.");

        return new Email(normalized);
    }

    public override string ToString() => Value;
}
