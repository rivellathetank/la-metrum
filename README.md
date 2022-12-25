# la-metrum: Damage Meter for Lost Ark

- [Installation](#installation)
- [Usage](#usage)
- [Logs](#logs)
- [Updates](#updates)
- [Limitations](#limitations)
- [License](#license)

The high-level architecture is the same as in [shalzuth/LostArkLogger](
  https://github.com/shalzuth/LostArkLogger). The binary protocol (symmetric encryption key,
opcodes and message layout) is imported from [lost-ark-dev/meter-core](
  https://github.com/lost-ark-dev/meter-core).

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
5. *Optional*: Focus the window and press <kbd>R</kbd> to rotate stats.
6. Focus the window and press <kbd>Q</kbd> to quit.

## Logs

Damage stats are dumped to a text log whenever the first person dies or battle ends. You can find
them in `%LOCALAPPDATA%\rivellathetank\la-metrum`.

## Updates

When Lost Ark client is updated, the damage meter may stop working until it is in turn updated.
[Installation](#installation) instructions work for updates, too.

## Limitations

Only Steam client is supported.

## License

GPLv3.
