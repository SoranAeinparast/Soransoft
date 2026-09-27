using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Soransoft.Web.Infrastructure
{
    /// <summary>
    /// دریافت‌کننده تاریخ با پشتیبانی از ورودی استاندارد <c>&lt;input type="date"&gt;</c>.
    /// <para>
    /// فرهنگ برنامه <c>fa-IR</c> با تقویم شمسی تنظیم شده است؛ به همین دلیل مقدارهای میلادی ISO که مرورگر
    /// از فیلدهای تاریخ می‌فرستد (مثل <c>2026-09-01</c>) به‌اشتباه «سال ۲۰۲۶ شمسی» تفسیر و با اختلاف
    /// چند صد سال ذخیره می‌شدند. این دریافت‌کننده هر مقدار با الگوی ISO را میلادی می‌خواند و بقیه را با
    /// فرهنگ جاری (پشتیبانی از تاریخ شمسی متنی) تفسیر می‌کند.
    /// </para>
    /// </summary>
    public sealed class GregorianDateModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            var modelType = context.Metadata.ModelType;
            var underlying = Nullable.GetUnderlyingType(modelType) ?? modelType;
            if (underlying == typeof(DateTime) || underlying == typeof(DateOnly))
                return new GregorianDateModelBinder(underlying, context.Metadata.IsNullableValueType);
            return null;
        }
    }

    /// <summary>دریافت‌کننده تاریخ میلادی/شمسی — جزئیات در <see cref="GregorianDateModelBinderProvider"/>.</summary>
    public sealed class GregorianDateModelBinder : IModelBinder
    {
        /// <summary>الگوی ISO 8601 (همان چیزی که فیلدهای date/time-local مرورگر می‌فرستند)</summary>
        private static readonly Regex IsoPattern = new(@"^\s*\d{4}-\d{2}-\d{2}([T ]|$)", RegexOptions.Compiled);

        private readonly Type _underlyingType;
        private readonly bool _nullable;

        public GregorianDateModelBinder(Type underlyingType, bool nullable)
        {
            _underlyingType = underlyingType;
            _nullable = nullable;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueResult == ValueProviderResult.None) return Task.CompletedTask;
            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);

            var raw = valueResult.FirstValue;
            if (string.IsNullOrWhiteSpace(raw))
            {
                if (_nullable) bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            DateTime parsed;
            bool ok;
            if (IsoPattern.IsMatch(raw))
            {
                // خروجی استاندارد فیلدهای تاریخ مرورگر — همیشه میلادی
                ok = DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
            }
            else
            {
                // ورودی متنی (مثلاً تاریخ شمسی) — با فرهنگ جاری تفسیر می‌شود
                var culture = bindingContext.HttpContext?.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture
                              ?? CultureInfo.CurrentCulture;
                ok = DateTime.TryParse(raw, culture, DateTimeStyles.None, out parsed);
            }

            if (!ok)
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "تاریخ وارد‌شده معتبر نیست.");
                return Task.CompletedTask;
            }

            object value = _underlyingType == typeof(DateOnly) ? DateOnly.FromDateTime(parsed) : parsed;
            bindingContext.Result = ModelBindingResult.Success(value);
            return Task.CompletedTask;
        }
    }
}
