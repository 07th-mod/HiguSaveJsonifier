# HiguSaveJsonifier
Converts Higurashi saves to JSON so you can read them

## Usage
Run with `dotnet run --project HiguSaveJsonifier/HiguSaveJsonifier.csproj gameNumber mysave.dat` where gameNumber is a number representing the game, with Onikakushi being 1, Watanagashi 2, up to Minagoroshi at 7.  The only game whose save format is actually different at the moment is Minagoroshi, so the important thing is that you use 7 for Minagoroshi and a number less than 7 for everything else.

The json will be printed to stdout, so if you want to save it to a file, do `dotnet run --project HiguSaveJsonifier/HiguSaveJsonifier.csproj gameNumber mysave.dat > mysave.json`
