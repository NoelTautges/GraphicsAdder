# GraphicsAdder

A program that converts the DirectX shaders of [Outer Wilds](https://www.mobiusdigitalgames.com/outer-wilds.html) to OpenGL as a first step to port it to other platforms.

## Important notes: read before using

- **Do not use the modified version of Outer Wilds if you are photosensitive.** The intro and main menu are no longer subject to blue strobe lights, but I cannot guarantee the safety of the game after this point.

- **You convert your files at your own risk.** While I have ironed out all discovered bugs where Outer Wilds crashes as a result of using DirectX with converted files, I cannot guarantee it will never crash.

- **This tool will not corrupt your saves**, even if you replace the game files, because they are stored in a separate folder.

## How do I use it?

1.) Make sure you're on Windows and have the Steam version of Outer Wilds installed

2.) Download the latest version of GraphicsAdder from the releases tab to the right

3.) Back up your `OuterWilds_Data` folder (open by right clicking on Outer Wilds in Steam > Local Files > Browse) in case something goes wrong

4.) Extract the zip file, run `GraphicsAdder.exe`, click Load Steam and Convert, and wait for it to finish

5.) Drag the files from `OuterWilds_Data_replacement` to `OuterWilds_Data` and hit replace

6.) Right click on Outer Wilds in Steam > Properties > Add `-force-glcore` to Launch Options

7.) Launch Outer Wilds from Steam as usual and see the beauty (?) of a world without proper lighting effects, most of the in-game GUI, and far too dithery geysers!

8.) (optional) Remove the Launch Options in Properties if you want to go back to the original game

## How do I build it?

Open `GraphicsAdder.sln` in Visual Studio and build the `GraphicsAdder` project. Only x64 is supported.

Non-NuGet dependencies are included in `Libraries`.

### Dependencies

1.) [AssetTools.NET](https://github.com/nesrak1/AssetsTools.NET) (commit >= `3a84f92`)

2.) [HLSLccWrapper](https://github.com/NoelTautges/HLSLccWrapper) (my fork without architecture suffixes if you don't feel like changing it yourself)

3.) [uTinyRipper and its component DXShaderRestorer](https://github.com/NoelTautges/UtinyRipper) (my fork with write support for shader blobs)

## I have a question!

Shoot me a DM over at [my Twitter](https://twitter.com/NoelTautges) or [my Reddit](https://www.reddit.com/message/compose/?to=u/NoelTautges) or [open an issue in this repository](https://github.com/NoelTautges/GraphicsAdder/issues/new)!

## Future plans

1.) Epic Games Store support

2.) Automatic backup and restore for game files

3.) Generalization to all DirectX-exclusive Unity games

4.) Make all shaders actually work (low priority)