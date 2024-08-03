using System.Text.Json;
using Localization.Common;

namespace Localization.Loader;

public class LazyLocalizationLoader:ILocalizationLoader
{
    private string localesPath;
    private string filename;
    private Dictionary<Language, Dictionary<string, string>> loaded = new();

    public LazyLocalizationLoader(string localesPath, string filename)
    {
        this.localesPath = localesPath;
        this.filename = filename;
    }

    public Dictionary<string, string> Get(Language language)
    {
        if (loaded.ContainsKey(language)) return loaded[language];
        var path = Path.Combine(localesPath, $"{filename}.{language.ToString().ToLower()}.json");
        if (!File.Exists(path))
            throw new Exception($"{path} does not exist");
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
        return loaded[language] = dict ?? throw new Exception("json is null");
    }
}