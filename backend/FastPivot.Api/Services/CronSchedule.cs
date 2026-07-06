using System.Globalization;

namespace FastPivot.Api.Services;

public sealed class CronSchedule
{
    private static readonly IReadOnlyDictionary<string, int> MonthAliases = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["JAN"] = 1,
        ["FEB"] = 2,
        ["MAR"] = 3,
        ["APR"] = 4,
        ["MAY"] = 5,
        ["JUN"] = 6,
        ["JUL"] = 7,
        ["AUG"] = 8,
        ["SEP"] = 9,
        ["OCT"] = 10,
        ["NOV"] = 11,
        ["DEC"] = 12
    };

    private static readonly IReadOnlyDictionary<string, int> DayOfWeekAliases = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["SUN"] = 0,
        ["MON"] = 1,
        ["TUE"] = 2,
        ["WED"] = 3,
        ["THU"] = 4,
        ["FRI"] = 5,
        ["SAT"] = 6
    };

    private readonly CronField _seconds;
    private readonly CronField _minutes;
    private readonly CronField _hours;
    private readonly CronField _daysOfMonth;
    private readonly CronField _months;
    private readonly CronField _daysOfWeek;

    private CronSchedule(
        CronField seconds,
        CronField minutes,
        CronField hours,
        CronField daysOfMonth,
        CronField months,
        CronField daysOfWeek)
    {
        _seconds = seconds;
        _minutes = minutes;
        _hours = hours;
        _daysOfMonth = daysOfMonth;
        _months = months;
        _daysOfWeek = daysOfWeek;
    }

    public static CronSchedule Parse(string expression)
    {
        var parts = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 6)
        {
            throw new FormatException("Expected six Quartz cron fields: second minute hour day-of-month month day-of-week.");
        }

        return new CronSchedule(
            CronField.Parse(parts[0], 0, 59, allowQuestionMark: false),
            CronField.Parse(parts[1], 0, 59, allowQuestionMark: false),
            CronField.Parse(parts[2], 0, 23, allowQuestionMark: false),
            CronField.Parse(parts[3], 1, 31, allowQuestionMark: true),
            CronField.Parse(parts[4], 1, 12, allowQuestionMark: false, aliases: MonthAliases),
            CronField.Parse(parts[5], 0, 7, allowQuestionMark: true, aliases: DayOfWeekAliases));
    }

    public IReadOnlyList<DateTimeOffset> GetOccurrences(DateOnly startDate, DateOnly endDate, TimeZoneInfo timeZone)
    {
        var occurrences = new List<DateTimeOffset>();

        for (var date = startDate; date.DayNumber <= endDate.DayNumber; date = date.AddDays(1))
        {
            if (!MatchesDate(date))
            {
                continue;
            }

            foreach (var hour in _hours.Values)
            {
                foreach (var minute in _minutes.Values)
                {
                    foreach (var second in _seconds.Values)
                    {
                        var local = date.ToDateTime(new TimeOnly(hour, minute, second), DateTimeKind.Unspecified);
                        var offset = timeZone.GetUtcOffset(local);
                        occurrences.Add(new DateTimeOffset(local, offset));
                    }
                }
            }
        }

        return occurrences
            .OrderBy(occurrence => occurrence)
            .ToArray();
    }

    private bool MatchesDate(DateOnly date)
    {
        return _months.Matches(date.Month)
            && MatchesDayOfMonth(date)
            && MatchesDayOfWeek(date);
    }

    private bool MatchesDayOfMonth(DateOnly date)
    {
        return _daysOfMonth.IsUnspecified || _daysOfMonth.Matches(date.Day);
    }

    private bool MatchesDayOfWeek(DateOnly date)
    {
        if (_daysOfWeek.IsUnspecified)
        {
            return true;
        }

        var cronDay = date.DayOfWeek == DayOfWeek.Sunday ? 0 : (int)date.DayOfWeek;
        return _daysOfWeek.Matches(cronDay) || (cronDay == 0 && _daysOfWeek.Matches(7));
    }

    private sealed class CronField
    {
        private readonly HashSet<int> _values;

        private CronField(HashSet<int> values, bool isUnspecified)
        {
            _values = values;
            IsUnspecified = isUnspecified;
        }

        public bool IsUnspecified { get; }

        public IReadOnlyList<int> Values => _values.Order().ToArray();

        public static CronField Parse(
            string field,
            int min,
            int max,
            bool allowQuestionMark,
            IReadOnlyDictionary<string, int>? aliases = null)
        {
            if (allowQuestionMark && field == "?")
            {
                return new CronField([], isUnspecified: true);
            }

            var values = new HashSet<int>();
            var tokens = field.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var token in tokens)
            {
                AddTokenValues(token, min, max, aliases, values);
            }

            if (values.Count == 0)
            {
                throw new FormatException($"Cron field '{field}' did not produce any values.");
            }

            return new CronField(values, isUnspecified: false);
        }

        public bool Matches(int value)
        {
            return _values.Contains(value);
        }

        private static void AddTokenValues(
            string token,
            int min,
            int max,
            IReadOnlyDictionary<string, int>? aliases,
            HashSet<int> values)
        {
            var stepParts = token.Split('/', StringSplitOptions.TrimEntries);
            if (stepParts.Length > 2)
            {
                throw new FormatException($"Invalid stepped cron token '{token}'.");
            }

            var rangePart = stepParts[0];
            var step = stepParts.Length == 2 ? ParseNumber(stepParts[1], 1, max) : 1;
            int start;
            int end;

            if (rangePart == "*")
            {
                start = min;
                end = max;
            }
            else if (rangePart.Contains('-', StringComparison.Ordinal))
            {
                var range = rangePart.Split('-', StringSplitOptions.TrimEntries);
                if (range.Length != 2)
                {
                    throw new FormatException($"Invalid cron range '{rangePart}'.");
                }

                start = ParseValue(range[0], min, max, aliases);
                end = ParseValue(range[1], min, max, aliases);
            }
            else
            {
                start = ParseValue(rangePart, min, max, aliases);
                end = start;
            }

            if (start > end)
            {
                throw new FormatException($"Cron range '{rangePart}' has a start greater than its end.");
            }

            for (var value = start; value <= end; value += step)
            {
                values.Add(value);
            }
        }

        private static int ParseValue(
            string value,
            int min,
            int max,
            IReadOnlyDictionary<string, int>? aliases)
        {
            if (aliases is not null && aliases.TryGetValue(value, out var aliasValue))
            {
                return aliasValue;
            }

            return ParseNumber(value, min, max);
        }

        private static int ParseNumber(string value, int min, int max)
        {
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            {
                throw new FormatException($"'{value}' is not a valid cron value.");
            }

            if (number < min || number > max)
            {
                throw new FormatException($"Cron value '{value}' is outside the allowed range {min}-{max}.");
            }

            return number;
        }
    }
}
