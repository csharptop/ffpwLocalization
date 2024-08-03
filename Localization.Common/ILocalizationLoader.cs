namespace Localization.Common;

public interface ILocalizationLoader
{
    Dictionary<string, string> Get(Language language);
}