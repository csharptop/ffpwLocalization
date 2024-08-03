using Localization.Common;

namespace Localization.Loader;

public class Localizer:ILocalizer
{
    private readonly ILocalizationLoader loader;
    public Language CurrentLanguage { get; set; }

    public Localizer(ILocalizationLoader loader)
    {
        this.loader = loader;
    }

    public string Get(string literal)
    {
        var d = loader.Get(CurrentLanguage);
        var result = d.GetValueOrDefault(literal, "");
        if (string.IsNullOrEmpty(result))
            return literal;
        return result;
    }
}