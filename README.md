warning: THIS CODE SUCKS

Releases are here:

https://github.com/mike309game/DeltaHATE_GUI/releases

https://github.com/mike309game/DeltaHATE_GUI/releases

https://github.com/mike309game/DeltaHATE_GUI/releases

https://github.com/mike309game/DeltaHATE_GUI/releases


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

The only problem is that it doesn't really have a "power meter" so you'll always get [the](https://cdn.discordapp.com/attachments/507353685228126218/528569210679590912/unknown.png) [max](https://cdn.discordapp.com/attachments/299645962765205515/528632282123337759/unknown.png) [chaos.](https://cdn.discordapp.com/attachments/299645962765205515/528632474582908928/unknown.png)

##IMPORTANT
In case you get softlocked in a part of the game even when shuffling the game many times, you'd probably want to use a clean data.win of the game to progress beyond that part.

(and I'd really appreciate if you contacted me on [Discord](https://discord.gg/npBTeGu) and reported the softlock to me and I'll see what I can do.)

## TODO (there's some more in the TODO.txt file in the release)

-=DELTARUNE

-Background shuffle in unstable mode

-Remove background images in sprite list

-alright turns out unstable shuffle [really is unstable](https://cdn.discordapp.com/attachments/507353685228126218/528572036101177354/unknown.png) (this is the exact part which i saw in a video that the game softlocked). I really need to do something about this since unstable shuffle is the default option.

-=GLOBAL

-POWER METER

-Keeps running after closing, really need a fix to that

-Add an customizable sprite exception for both modes

-Add an exception for font graphics in stable mode (they're just too big and just fills up the screen in the game)

-Put the "deltahate_logo.png" file inside the exe
