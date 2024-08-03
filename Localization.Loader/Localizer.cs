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
        return d.GetValueOrDefault(literal, literal);
    }
}