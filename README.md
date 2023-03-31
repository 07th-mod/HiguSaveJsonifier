# HiguSaveJsonifier

Converts Higurashi saves to JSON so you can read them

## Usage

Make sure to choose the correct command depending on if you are extracting a global.dat file, or a regular save file. Choosing the wrong type will cause an exception to be thrown.

----

### Converting global.dat files (modded or unmodded)

Convert global.dat files to JSON like: `dotnet run global global.dat`

This uses a modified version of the "LoadGlobals()" function in Assets.Scripts.Core.Buriko/BurikoMemory.cs from the Modded Higurashi DLL.

It will even load old modded, or unmodded vanilla `global.dat`, but the "graphicsPresetState" dictionary wiil be empty and a warning will be printed to stderr.

I think the only thing that has ever changed with the format of `global.dat` files is this extra "graphicsPresetState" dictionary which we added into the mod to store your mod graphics preset settings (like what backgrounds, sprites etc. you have customized).

----

### Converting save files like save[SAVE_NUMBER].dat or qsave[QSAVE_NUMBER].dat

Run with `dotnet run --project HiguSaveJsonifier/HiguSaveJsonifier.csproj gameNumber mysave.dat` where gameNumber is a number representing the game, with Onikakushi being 1, Watanagashi 2, up to Minagoroshi at 7.  The only game whose save format is actually different at the moment is Minagoroshi, so the important thing is that you use 7 for Minagoroshi and a number less than 7 for everything else.

The json will be printed to stdout, so if you want to save it to a file, do `dotnet run --project HiguSaveJsonifier/HiguSaveJsonifier.csproj gameNumber mysave.dat > mysave.json`
