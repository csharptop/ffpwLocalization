namespace Localization.Common;

public interface ILocalizer
{
    Language CurrentLanguage { get; set; }
    string Get(string literal);
}