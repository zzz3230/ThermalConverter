namespace GeneratedResourceClient.GenresObject.Collections;

public class TimeSeriesValue<T>
{
    public readonly DateTime Date;
    public readonly T Value;

    public TimeSeriesValue(DateTime date, T value)
    {
        Date = date;
        Value = value;
    }

    public override bool Equals(object obj) => obj != null && obj.GetHashCode() == GetHashCode();
    public override int GetHashCode() => HashCode.Combine(Date, Value);
    public override string ToString() => Date.ToString("d") + " : " + Value;
}

public class TimeSeriesValue : TimeSeriesValue<double>
{
    public TimeSeriesValue(DateTime date, double value) : base(date, value)
    {
    }
}