using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RefalinaLaundry.Web.Infrastructure;

public sealed class FlexibleDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);
        ValueProviderResult value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (value == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, value);
        string? rawValue = value.FirstValue?.Trim();
        if (string.IsNullOrEmpty(rawValue))
        {
            if (Nullable.GetUnderlyingType(bindingContext.ModelType) is not null)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }

            return Task.CompletedTask;
        }

        string normalized = Normalize(rawValue);
        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(parsed);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Gunakan angka yang valid, misalnya 2,5 atau 2.5.");
        }

        return Task.CompletedTask;
    }

    private static string Normalize(string value)
    {
        int lastComma = value.LastIndexOf(',');
        int lastDot = value.LastIndexOf('.');
        if (lastComma >= 0 && lastDot >= 0)
        {
            return lastComma > lastDot
                ? value.Replace(".", string.Empty, StringComparison.Ordinal).Replace(',', '.')
                : value.Replace(",", string.Empty, StringComparison.Ordinal);
        }

        return value.Replace(',', '.');
    }
}

public sealed class FlexibleDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Type modelType = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;
        return modelType == typeof(decimal) ? new FlexibleDecimalModelBinder() : null;
    }
}
