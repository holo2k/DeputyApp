namespace Application.Mappers;

public static class ObjectUpdater
{
    public static void UpdateFrom<TSource, TTarget>(this TTarget target, TSource source)
    {
        var sourceProps = typeof(TSource).GetProperties();
        var targetType = typeof(TTarget);

        foreach (var prop in sourceProps)
        {
            var newValue = prop.GetValue(source);

            var targetProp = targetType.GetProperty(prop.Name);
            if (targetProp == null || !targetProp.CanWrite)
                continue;

            var oldValue = targetProp.GetValue(target);

            if (!Equals(oldValue, newValue))
            {
                targetProp.SetValue(target, newValue);
            }
        }
    }
}
