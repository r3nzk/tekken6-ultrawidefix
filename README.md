# Tekken 6 Ultrawide Fix
[![Github license](https://img.shields.io/github/license/r3nzk/tekken6-ultrawidefix.svg)](LICENSE) [![GitHub release](https://img.shields.io/github/v/release/R3nzk/tekken6-ultrawidefix?style=label=release)](https://github.com/R3nzk/tekken6-ultrawidefix/releases) ![Version](https://img.shields.io/badge/version-0.1-blue.svg)

<p align="center">
  <img width="800" height="337" alt="t6-preview" src="https://github.com/user-attachments/assets/edf8ddec-00d9-429d-b699-f18f63a81e30"/>
</p>

A simple utility that automatically injects ultrawide aspect ratio patches into Tekken 6 memory on the RPCS3 emulator, either automatically in the background or manually.

## Features
- 21:9 and 31:9 aspect ratio support for Tekken 6 (RPCS3)
- Standby tray behaviour (check for RPCS3/Tekken6 process on the background).
- Revert patch on exit or keep after closing.

## Usage
### Prerequisite (RPCS3 Setup)
Before running the tool, you must allow RPCS3 to stretch the display area:
1. Go to **Config** (or custom config the game) > **GPU**.
2. Under **Additional Settings**, enable **"Stretch to Display Area"**.

### Applying the patch
1. Run `tekken6ultrawidefix.exe`.
2. Open RPCS3 and launch Tekken 6.
3. Toggle the **Enable Ultrawide Fix** checkbox.
4. Select your preferred aspect ratio preset from the dropdown.
> **Using Manual Scan:** If you disable **Automatic Scan** in the settings, the tool will be passive. You must launch the game, then click the **Manual Scan** button to hook the patch.


## How to build it yourself
You can compile the tool yourself from the source code:
1. Install the [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Clone or download this repository to your PC.
3. Open a terminal inside the project folder.
4. Run the command to compile and launch:
```
dotnet run
```

## Why a standalone tool instead of a RPCS3 GamePatch
An external injection tool is a bit overkill for an ultrawide patch, I know. But Tekken 6 uses dynamic addresses for aspect ratio, and I don't have time to track down stable pointers for a static patch. So I repurposed an old memory scanner instead of relying on a CE table every time.

It can be expanded to add anything too, any cheat or extra option.

## Libraries Used
* [Eto.Forms](https://github.com/picoe/eto)
 
## Known Issues
- Some character/stage models might disappear near the edges of the screen on specific maps (the game engine thinks they are off-screen and stops rendering them).
-The Versus/loading screen is still stretched.
- UI elements are stretched to fit the screen.
- If you close the tool while the game is patched with the "Restore On Exit" option disabled, when reopening the tool on the same process, it won't be able to find the address again, and you will have to restart the game.

## To Do
- **Linux Support:** The UI and backend was built to be cross-platform, but I haven't had the time to implement the Linux memory scanner backend and process tracking.

## License
MIT License – Free to use, modify, and distribute.
