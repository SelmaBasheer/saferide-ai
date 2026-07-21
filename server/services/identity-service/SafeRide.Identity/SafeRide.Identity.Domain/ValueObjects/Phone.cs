namespace SafeRide.Identity.Domain.ValueObjects;

public sealed record Phone
{
    public string Value { get; }

    private Phone(string value) => Value = value;

    public static Phone Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Phone cannot be empty.");

        var digits = new string(input.Where(char.IsDigit).ToArray());

        if (digits.Length == 12 && digits.StartsWith("91"))
            digits = digits[2..];

        if (digits.Length != 10)
            throw new ArgumentException($"'{input}' is not a valid Indian mobile number.");

        return new Phone($"+91{digits}");
    }

    public override string ToString() => Value;
}
