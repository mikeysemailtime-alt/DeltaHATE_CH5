warning 1 : THIS CODE WAS EDITED BY AI TO ADD CHAPTER 5 FUNCTIONALITY, IT MAY HAVE BUGS, IF THERE ARE LET ME KNOW.

warning 2 : THIS PROGRAM OVERWRITES YOUR DELTARUNE DATA.WIN FILES, THE PROGRAM CREATES A SINGLE BACKUP COPY OF YOUR ORIGINAL DATA.WIN IN THE GAMES FOLDER, ENSURE YOU COPY THIS SOMEWHERE SAFE SO YOU CAN EASILY REPAIR YOUR GAME!

warning 3 : ONLY CH1 + CH5 SUPPORTED FOR NOW, CAN ADD MORE IF PEOPLE WOULD LIKE.

Releases are here:

https://github.com/mikeysemailtime-alt/DeltaHATE_CH5/releases

The Original Repo for the unfixed version is below :

https://github.com/mike309game/DeltaHATE_GUI



# Building (v1.6, DELTARUNE chapters 1-5)

Needs the **.NET 10 SDK** (Windows, WPF) because current UndertaleModLib targets .NET 10.

```
git clone --recurse-submodules https://github.com/UnderminersTeam/UndertaleModTool UndertaleModTool   (next to the DeltaHATE folder, inside this repo's root)
dotnet build DeltaHATE/DeltaHATE.csproj -c Release
```
If your UndertaleModTool checkout lives elsewhere: `dotnet build -p:UtmtDir=C:\path\to\UndertaleModTool\` (trailing slash).

## Using it with current DELTARUNE
DELTARUNE is now a launcher plus one folder per chapter. Load the `data.win` **inside** the chapter folder
(`DELTARUNE\chapter1_windows\data.win` ... `chapter5_windows`), not the one in the DELTARUNE root (that's Chapter Select).

* Chapter 1 has `lang\lang_en.json`, so text is shuffled through it like before.
* Chapters 2-5 have no English lang file (the English is inline in the game), so text is shuffled from the data.win strings instead. It only affects the English language setting.
* Checked against a real Chapter 5 `data.win` (build 1ebbd5ff, GameMaker 2023.6, bytecode 17): detected as "DELTARUNE Chapter 5", ~10,200 dialogue lines found by their text-box endings, sounds are only shuffled inside their own audio group, and the baked `ftex_room_*` sprites are treated as backgrounds.
* When you save over the loaded data.win, the original is kept next to it as `data.win.deltahate_backup`.

# DeltaHATE GUI version
Yet another Deltarune corruptor! (that can corrupt other GameMaker Studio games too!)

This is basically [this](https://github.com/krzys-h/UndertaleModTool/blob/master/UndertaleModTool/SampleScripts/DeltaHATE.csx) script from [UndertaleModTool](https://github.com/krzys-h/UndertaleModTool/) but portted as an standalone program with options.

The only problem is that it doesn't really have a "power meter" so you'll always get crazy results, you may get softlocked by oversized NPC's, I recommend a new save file.

##IMPORTANT
In case you get softlocked in a part of the game even when shuffling the game many times, you'd probably want to use a clean data.win of the game to progress beyond that part, the program creates a backup of your original data.win, if you do not copy this file elsewhere, it will be overwritten if you reshuffle your files, so make sure to copy it!

