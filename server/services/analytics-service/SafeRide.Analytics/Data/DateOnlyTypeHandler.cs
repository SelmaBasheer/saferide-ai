using System.Data;
using Dapper;

namespace SafeRide.Analytics.Data;

/// <summary>
/// Dapper has no built-in mapping between DateOnly and a SQL DATE column, so any
/// record with a DateOnly property fails to materialise. Registering this once
/// covers every query rather than forcing DateTime into models that mean a date.
/// </summary>
public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value) =>
        value switch
        {
            DateTime dt => DateOnly.FromDateTime(dt),
            DateOnly d => d,
            string s => DateOnly.Parse(s),
            _ => throw new InvalidCastException(
                $"Cannot convert {value?.GetType().Name ?? "null"} to DateOnly."
            ),
        };
}
