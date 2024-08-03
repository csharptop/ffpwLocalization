using Localization.Common;

namespace Localization.Loader;

public class LazyLocalizationLoader:ILocalizationLoader
{
    public Dictionary<string, string> Get(Language language)
    {
        throw new NotImplementedException();
    }
}