namespace ffpw_localization;

public class Options
{
    public string? DirectoryPath { get; set; } = string.Empty; // Nullable
    public string? FileName { get; set; } = string.Empty; // Nullable
    public int MinLength { get; set; }
    public bool ExcludeSpecialCharsOnly { get; set; }
    public int ProgressBarStyle { get; set; }
    public string? ExcludeLanguages { get; set; } = string.Empty; // Nullable
    public string? IncludeLanguages { get; set; } = string.Empty; // Nullable
    public bool Visualize { get; set; }
}