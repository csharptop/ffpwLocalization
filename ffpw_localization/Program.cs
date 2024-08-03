using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;

class StringLiteralFinder : CSharpSyntaxWalker
{
  public List<LiteralExpressionSyntax> StringLiterals { get; } = new List<LiteralExpressionSyntax>();

  public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
  {
    if (node.Right is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
    {
      StringLiterals.Add(literal);
    }
    base.VisitAssignmentExpression(node);
  }

  public override void VisitArgument(ArgumentSyntax node)
  {
    if (node.Expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
    {
      StringLiterals.Add(literal);
    }
    base.VisitArgument(node);
  }
}

enum Language
{
  AF /* Afrikaans */, SQ /* Albanian */, AR /* Arabic */, EU /* Basque */, BG /* Bulgarian */, BE /* Byelorussian */, CA /* Catalan */, HR /* Croatian */,
  CS /* Czech */, DA /* Danish */, NL /* Dutch */, EN /* English */, EO /* Esperanto */, ET /* Estonian */, FO /* Faroese */, FI /* Finnish */,
  FR /* French */, GL /* Galician */, DE /* German */, EL /* Greek */, IW /* Hebrew */, HU /* Hungarian */, IS /* Icelandic */, ESKIMO /* Inuit */,
  GA /* Irish */, IT /* Italian */, JA /* Japanese */, KO /* Korean */, LV /* Latvian */, LT /* Lithuanian */, MK /* Macedonian */, MT /* Maltese */,
  NO /* Norwegian */, PL /* Polish */, PT /* Portuguese */, RO /* Romanian */, RU /* Russian */, GD /* Scottish */, SR /* Serbian cyrillic */, SK /* Slovak */,
  SL /* Slovenian */, ES /* Spanish */, SV /* Swedish */, TR /* Turkish */, UK /* Ukrainian */,
}

class Program
{
  static async Task Main(string[] args)
  {
    DisplayLicenseInfo();

    var rootCommand = new RootCommand
    {
      new Option<string>(
        new[] { "--directory", "-d" },
        "Directory path to search for .cs files."),
      new Option<string>(
        new[] { "--filename", "-f" },
        () => "strings",
        "Base name for the output JSON files."),
      new Option<int>(
        new[] { "--minlength", "-m" },
        () => 1,
        "Minimum length of string literals to include."),
      new Option<bool>(
        new[] { "--excludeSpecialCharsOnly", "-e" },
        () => false,
        "Exclude strings that contain only special characters."),
      new Option<int>(
        new[] { "--progressBarStyle", "-p" },
        () => 1,
        "Progress bar style (1, 2, or 3)."),
      new Option<string>(
        new[] { "--excludeLanguages", "-x" },
        "Comma-separated list of languages to exclude."),
      new Option<string>(
        new[] { "--includeLanguages", "-i" },
        "Comma-separated list of languages to include."),
      new Option<bool>(
        new[] { "--visualize", "-v" },
        () => false,
        "Visualize the generated files.")
    };

    rootCommand.Handler = CommandHandler.Create<string, string, int, bool, int, string, string, bool>((directory, filename, minlength, excludeSpecialCharsOnly, progressBarStyle, excludeLanguages, includeLanguages, visualize) =>
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
    });

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
      var files = Directory.EnumerateFiles(opts.DirectoryPath, "*.cs", SearchOption.AllDirectories).ToList();
      int totalFiles = files.Count;
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
        .Where(str => !string.IsNullOrWhiteSpace(str) && str.Length >= opts.MinLength && (!opts.ExcludeSpecialCharsOnly || str.Any(char.IsLetterOrDigit)) && !IsSpecialCharsOnly(str))
        .Distinct()
        .ToDictionary(str => str, str => "");

      var excludeLanguages = ParseLanguages(opts.ExcludeLanguages!);
      var includeLanguages = ParseLanguages(opts.IncludeLanguages!);

      foreach (var lang in Enum.GetValues(typeof(Language)).Cast<Language>())
      {
        if (excludeLanguages.Contains(lang) || (includeLanguages.Count > 0 && !includeLanguages.Contains(lang)))
        {
          continue;
        }

        var json = JsonConvert.SerializeObject(filteredStringLiterals, new JsonSerializerSettings
        {
          Formatting = Formatting.Indented,
          TypeNameHandling = TypeNameHandling.None,
          MetadataPropertyHandling = MetadataPropertyHandling.Ignore
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
    else if (ratio < 0.75)
    {
      return ConsoleColor.Yellow;
    }
    else
    {
      return ConsoleColor.Green;
    }
  }

  static bool IsSpecialCharsOnly(string str)
  {
    return Regex.IsMatch(str, @"^[^\w]+$");
  }

  static bool IsValidFilePath(string path)
  {
    try
    {
      var fileInfo = new FileInfo(path);
      return true;
    }
    catch (ArgumentException) { }
    catch (PathTooLongException) { }
    catch (NotSupportedException) { }
    return false;
  }

  static List<Language> ParseLanguages(string languages)
  {
    return languages?.Split(',')
      .Select(lang => Enum.TryParse<Language>(lang.Trim(), true, out var result) ? result : (Language?)null)
      .Where(lang => lang.HasValue)
      .Select(lang => lang!.Value)
      .ToList() ?? new List<Language>();
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