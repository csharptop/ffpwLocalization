# ffpw Localization Tools

## Overview
`ffpw Localization Tools` is a command-line utility designed to extract string literals from C# source files and organize them into JSON files for localization purposes. It provides a variety of options for filtering, excluding specific languages, and visualizing the results.

## Features
- Extracts string literals from `.cs` files in a specified directory.
- Supports filtering of string literals based on length and content.
- Allows inclusion or exclusion of specific languages.
- Generates output in JSON format for easy integration with localization systems.
- Provides a visual representation of the generated files if desired.

## Installation
1. Clone the repository or download the source code.
2. Open the project in Visual Studio or your preferred C# IDE.
3. Build the solution to compile the application.

## Usage
Run the application from the command line with the following options:

```bash
dotnet run -- -d <directory> -f <filename> -m <minlength> -e <excludeSpecialCharsOnly> -p <progressBarStyle> -x <excludeLanguages> -i <includeLanguages> -v <visualize>
```

### Options
- `-d`, `--directory`: **Required**. The directory path to search for `.cs` files.
- `-f`, `--filename`: **Optional**. Base name for the output JSON files (default is `strings`).
- `-m`, `--minlength`: **Optional**. Minimum length of string literals to include (default is `1`).
- `-e`, `--excludeSpecialCharsOnly`: **Optional**. Exclude strings that contain only special characters (default is `false`).
- `-p`, `--progressBarStyle`: **Optional**. Progress bar style (1, 2, or 3; default is `1`).
- `-x`, `--excludeLanguages`: **Optional**. Comma-separated list of languages to exclude (e.g., `en,fr`).
- `-i`, `--includeLanguages`: **Optional**. Comma-separated list of languages to include (e.g., `ru,ua`).
- `-v`, `--visualize`: **Optional**. Visualize the generated files (default is `false`).

### Example
To extract string literals from the `src` directory and generate JSON files for English and Spanish while excluding French, run:

```bash
dotnet run -- -d src -f strings -x FR -i EN,ES
```

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Legal Notice
This software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose, and non-infringement. In no event shall the authors be liable for any claim, damages, or other liability, whether in an action of contract, tort, or otherwise, arising from, out of, or in connection with the software or the use or other dealings in the software.

## Contact
For questions, comments, or contributions, please contact:

- **Project ffpwLocalization**
- Email: opensource@fishydino.net