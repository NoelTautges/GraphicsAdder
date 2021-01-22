# GraphicsAdder

A program that converts the Direct3D shaders of [Outer Wilds](https://www.mobiusdigitalgames.com/outer-wilds.html) to OpenGL as a first step to port it to other platforms.

## How do I use it?

1.) Make sure you're on Windows and have the Steam version of Outer Wilds installed

2.) Download the latest version of GraphicsAdder from the releases tab to the right

3.) Extract the zip file, run it, and wait for it to finish

4.) Check which files it generated in the OuterWilds_Data_replacement folder and back up those same files from your OuterWilds_Data folder if you don't want to reinstall the game in case something goes wrong

5.) Drag the files from OuterWilds_Data_replacement to OuterWilds_Data and hit replace

6.) Right click on Outer Wilds in Steam > Properties > Add `-force-glcore -force-gfx-without-build` to Launch Options

7.) Launch Outer Wilds from Steam as usual and see the beauty (?) of an upside-down world, censor bars everywhere, and a spaceship with all the RGB lights you could afford from Hearthian Fry's Electronics!

8.) (optional) Remove the Launch Options in Properties if you want to go back to the original game

## How do I build it?

Open GraphicsAdder.sln in Visual Studio and hit build. Only x64 is supported at the moment.

Non-NuGet dependencies are included in `GraphicsAdder\Libraries`.

### Dependencies

1.) [AssetTools.NET](https://github.com/nesrak1/AssetsTools.NET) (commit >= `3a84f92`)

2.) [HLSLccWrapper](https://github.com/spacehamster/HLSLccWrapper)

3.) [uTinyRipper and its component DXShaderRestorer](https://github.com/NoelTautges/UtinyRipper) (my fork with write support for shader blobs)

## I have a question!

Shoot me a DM over at [my Twitter](https://twitter.com/NoelTautges) or open an issue in this repository!

## Future plans

1.) Epic Games Store support

2.) Automatic backup, replace, and restore for game files

3.) Generalization to all Direct3D-exclusive Unity games

4.) Make all shaders actually work (low priority)