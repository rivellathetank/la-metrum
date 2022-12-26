# la-metrum: Damage Meter for Lost Ark

- [Screenshots](#screenshots)
- [Installation](#installation)
- [Usage](#usage)
- [Logs](#logs)
- [Updates](#updates)
- [Limitations](#limitations)
- [Compiling](#compiling)
- [License](#license)

The high-level architecture is the same as in [shalzuth/LostArkLogger](
  https://github.com/shalzuth/LostArkLogger). The binary protocol (symmetric encryption key,
opcodes and message layout) is imported from [lost-ark-dev/meter-core](
  https://github.com/lost-ark-dev/meter-core).

## Screenshots

### Encounter Damage Report

![Battle](
  https://raw.githubusercontent.com/rivellathetank/la-metrum/master/Screenshots/battle.jpg)

### Player Damage Report

![Player](
  https://raw.githubusercontent.com/rivellathetank/la-metrum/master/Screenshots/player.jpg)

## Installation

1. Download `la-metrum.zip` from [the latest release](
     https://github.com/rivellathetank/la-metrum/releases/latest).
2. Extract it.
3. *Optional*: Pin `LaMetrum.exe` to the taskbar after launching it.

## Usage

1. Launch `LaMetrum.exe`.
2. If it asks for access to public and private networks, allow both.
3. Move the window by dragging with the left mouse button.
4. Play Lost Ark.
5. Left mouse click to drill down, right mouse click to go up one level.
6. Focus the window and press <kbd>Q</kbd> to quit.

You can manually rotate stats (end the current battle and start a new one) by
focusing the window and pressing <kbd>R</kbd>.

## Logs

Damage stats are dumped to a text log whenever the first person dies or battle ends. You can find
them in `%LOCALAPPDATA%\rivellathetank\la-metrum`.

## Updates

When Lost Ark client is updated, the damage meter may stop working. Once the damage meter is in turn
updated and a new version is released, you'll need to upgrade to it. [Installation](#installation)
instructions work for upgrading to the latest version, too.

If you want to be notified about new releases, click *Watch* at the top, then *Custom* and tick
*Releases*. You'll need a GitHub account for that.

## Limitations

Only Steam client is supported.

## Compiling

```shell
git clone --config core.autocrlf=true https://github.com/rivellathetank/la-metrum.git
cd la-metrum
dotnet.exe publish -c Release -r win-x64 --self-contained
zip la-metrum.zip bin/x64/Release/net7.0-windows/win-x64/publish/*
```

This is how releases are built. Builds are reproducible.

## License

GPLv3.
