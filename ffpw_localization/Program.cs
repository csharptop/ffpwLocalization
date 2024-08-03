using System.CommandLine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.Json;
using System.Text.Json.Serialization;
using Localization.Common;

namespace ffpw_localization;
class Program
{
    static async Task Main(string[] args)
    {
        DisplayLicenseInfo();

        var rootCommand = new RootCommand();
        var directoryOption = new Option<string>(
            new[] { "--directory", "-d" },
            "Directory path to search for .cs files.");
        var filenameOption = new Option<string>(
            new[] { "--filename", "-f" },
            () => "strings",
            "Base name for the output JSON files.");
        var minLengthOption = new Option<int>(
            new[] { "--minlength", "-m" },
            () => 1,
            "Minimum length of string literals to include.");
        var excludeSpecialCharsOnlyOption = new Option<bool>(
            new[] { "--excludeSpecialCharsOnly", "-e" },
            () => false,
            "Exclude strings that contain only special characters.");
        var progressBarStyleOption = new Option<int>(
            new[] { "--progressBarStyle", "-p" },
            () => 1,
            "Progress bar style (1, 2, or 3).");
        var excludeLanguagesOption = new Option<string>(
            new[] { "--excludeLanguages", "-x" },
            "Comma-separated list of languages to exclude.");
        var includeLanguagesOption = new Option<string>(
            new[] { "--includeLanguages", "-i" },
            "Comma-separated list of languages to include.");
        var visualizeOption = new Option<bool>(
            new[] { "--visualize", "-v" },
            () => false,
            "Visualize the generated files.");
        rootCommand.AddOption(directoryOption);
        rootCommand.AddOption(filenameOption);
        rootCommand.AddOption(minLengthOption);
        rootCommand.AddOption(excludeSpecialCharsOnlyOption);
        rootCommand.AddOption(progressBarStyleOption);
        rootCommand.AddOption(excludeLanguagesOption);
        rootCommand.AddOption(includeLanguagesOption);
        rootCommand.AddOption(visualizeOption);
        rootCommand.SetHandler(
            (directory, filename, minlength, excludeSpecialCharsOnly, progressBarStyle, excludeLanguages,
                includeLanguages, visualize) =>
            {
                RunOptions(new Options
                {
                    DirectoryPath = directory,
                    FileName = filename,
                    MinLength = minlength,
                    ExcludeSpecialCharsOnly = excludeSpecialCharsOnly,
                    ProgressBarStyle = progressBarStyle,
                    ExcludeLanguages = excludeLanguages,
                    IncludeLanguages = includeLanguages,
                    Visualize = visualize
                });
            },
            directoryOption,
            filenameOption,
            minLengthOption,
            excludeSpecialCharsOnlyOption,
            progressBarStyleOption,
            excludeLanguagesOption,
            includeLanguagesOption,
            visualizeOption);

        await rootCommand.InvokeAsync(args);
    }

    static void DisplayLicenseInfo()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  ffpw Localization Tools - Version 0.1.0-beta20230107.519");
        Console.WriteLine("  (c) 2023 Bumazhnik");
        Console.WriteLine("  Licensed under the MIT License");
        Console.WriteLine("  Contact: ffpw@pwnable.me");
        Console.WriteLine("========================================\n");
    }



    static void RunOptions(Options opts)
    {
        if (!Directory.Exists(opts.DirectoryPath))
        {
            Console.WriteLine("The specified directory does not exist.");
            return;
        }

        var outputDirectory = Path.Combine(opts.DirectoryPath, "locales");
        Directory.CreateDirectory(outputDirectory);

        var stringLiterals = new List<string>();

        try
        {
            var files = Directory.GetFiles(opts.DirectoryPath, "*.cs", SearchOption.AllDirectories);
            int totalFiles = files.Length;
            int processedFiles = 0;

            foreach (var file in files)
            {
                var code = File.ReadAllText(file);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
                var root = (CompilationUnitSyntax)tree.GetRoot();
                var stringLiteralFinder = new StringLiteralFinder();
                stringLiteralFinder.Visit(root);
                stringLiterals.AddRange(stringLiteralFinder.StringLiterals.Select(lit => lit.Token.ValueText));

                processedFiles++;
                DrawProgressBar(processedFiles, totalFiles, opts.ProgressBarStyle);
            }

            var filteredStringLiterals = stringLiterals
                .Where(str =>
                    !string.IsNullOrWhiteSpace(str) && str.Length >= opts.MinLength &&
                    (!opts.ExcludeSpecialCharsOnly || str.Any(char.IsLetterOrDigit)))
                .Distinct()
                .ToDictionary(str => str, str => "");

            var excludeLanguages = ParseLanguages(opts.ExcludeLanguages);
            var includeLanguages = ParseLanguages(opts.IncludeLanguages);

            foreach (var lang in Enum.GetValues<Language>())
            {
                if (excludeLanguages.Contains(lang) || (includeLanguages.Count > 0 && !includeLanguages.Contains(lang)))
                {
                    continue;
                }
                
                var json = JsonSerializer.Serialize(filteredStringLiterals, new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                var outputFilePath = Path.Combine(outputDirectory, $"{opts.FileName}.{lang.ToString().ToLower()}.json");
                File.WriteAllText(outputFilePath, json);
            }

            Console.WriteLine($"\nString literals extracted and written to {outputDirectory}");

            if (opts.Visualize)
            {
                VisualizeGeneratedFiles(outputDirectory);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"Access denied: {ex.Message}");
            LogError(ex);
        }
        catch (IOException ex)
        {
            Console.WriteLine($"I/O error: {ex.Message}");
            LogError(ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            LogError(ex);
        }
    }

    static void DrawProgressBar(int progress, int total, int style)
    {
        int barWidth = 50;
        float ratio = (float)progress / total;
        int filledBarWidth = (int)(ratio * barWidth);

        char filledChar = style switch
        {
            2 => '=',
            3 => '*',
            _ => '#'
        };

        char emptyChar = style switch
        {
            2 => '.',
            3 => ' ',
            _ => '-'
        };

        Console.CursorLeft = 0;
        Console.Write("[");
        for (int i = 0; i < filledBarWidth; i++)
        {
            Console.ForegroundColor = GetColor(i, barWidth);
            Console.Write(filledChar);
        }

        Console.ResetColor();
        for (int i = filledBarWidth; i < barWidth; i++)
        {
            Console.Write(emptyChar);
        }

        Console.Write($"] {progress}/{total} ({ratio:P0})");
    }

    static ConsoleColor GetColor(int position, int barWidth)
    {
        float ratio = (float)position / barWidth;
        if (ratio < 0.5)
        {
            return ConsoleColor.Red;
        }

        if (ratio < 0.75)
        {
            return ConsoleColor.Yellow;
        }

        return ConsoleColor.Green;
    }

    static List<Language> ParseLanguages(string? languages) => ParseIterator(languages).ToList();
    static IEnumerable<Language> ParseIterator(string? languages)
    {
        if(languages == null) yield break;
        foreach (var lang in languages.Split(','))
            if (Enum.TryParse<Language>(lang.Trim(), true, out var result))
                yield return result;
    }

    static void LogError(Exception ex)
    {
        var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
        var logMessage = $"{DateTime.Now}: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}";
        File.AppendAllText(logFilePath, logMessage);
    }

    static void VisualizeGeneratedFiles(string outputDirectory)
    {
        var files = Directory.EnumerateFiles(outputDirectory, "*.json").ToList();
        Console.WriteLine("\nGenerated Files:");
        foreach (var file in files)
        {
            Console.WriteLine($"- {Path.GetFileName(file)}");
        }
    }
}