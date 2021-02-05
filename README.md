# GraphicsAdder

A program that converts the DirectX shaders of [Outer Wilds](https://www.mobiusdigitalgames.com/outer-wilds.html) to OpenGL as a first step to port it to other platforms.

## Important notes: read before using

- **Do not use the modified version of Outer Wilds if you are photosensitive.** The intro and main menu are currently subject to a bug with flashing blue lights. I cannot guarantee the safety of the game after this point either; for example, black boxes flicker frequently while on the Ash Twin's surface.

- **You convert your files at your own risk.** While I have ironed out all discovered bugs where Outer Wilds crashes as a result of using DirectX with converted files, I cannot guarantee it will never crash.

- **This tool will not corrupt your saves**, even if you replace the game files, because they are stored in a separate folder.

## How do I use it?

1.) Make sure you're on Windows and have the Steam version of Outer Wilds installed

2.) Download the latest release version of GraphicsAdder from the releases tab to the right

3.) Back up your `OuterWilds_Data` folder (open by right clicking on Outer Wilds in Steam > Local Files > Browse) in case something goes wrong

4.) Extract the zip file, run `GraphicsAdder.exe`, click Load Steam and Convert, and wait for it to finish

5.) Drag the files from `OuterWilds_Data_replacement` to `OuterWilds_Data` and hit replace

6.) Right click on Outer Wilds in Steam > Properties > Add `-force-glcore -force-gfx-without-build` to Launch Options

7.) Launch Outer Wilds from Steam as usual and see the beauty (?) of a world without proper lighting effects, most of the in-game GUI, and a spaceship with all the RGB lights Slate could afford from Hearthian Fry's Electronics!

8.) (optional) Remove the Launch Options in Properties if you want to go back to the original game

## How do I build it?

Open GraphicsAdder.sln in Visual Studio and hit build. Only x64 is supported at the moment.

Non-NuGet dependencies are included in `GraphicsAdder\Libraries`.

### Dependencies

1.) [AssetTools.NET](https://github.com/nesrak1/AssetsTools.NET) (commit >= `3a84f92`)

2.) [HLSLccWrapper](https://github.com/spacehamster/HLSLccWrapper)

3.) [uTinyRipper and its component DXShaderRestorer](https://github.com/NoelTautges/UtinyRipper) (my fork with write support for shader blobs)

## I have a question!

Shoot me a DM over at [my Twitter](https://twitter.com/NoelTautges) or [my Reddit](https://www.reddit.com/message/compose/?to=u/NoelTautges) or [open an issue in this repository](https://github.com/NoelTautges/GraphicsAdder/issues/new)!

## Future plans

1.) Epic Games Store support

2.) Automatic backup, and restore for game files

3.) Generalization to all DirectX-exclusive Unity games

4.) Make all shaders actually work (low priority)