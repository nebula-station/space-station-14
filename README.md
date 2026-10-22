<div align="center">
    <h1>✦ Nebula ✦</h1>
    <h3></h3>
</div>

<div align="center">

![](https://img.shields.io/github/last-commit/nebula-station/space-station-14?&style=for-the-badge&color=a78bfa&logo=git&logoColor=E0DEF4&labelColor=1F1A2E)
![](https://img.shields.io/github/languages/top/nebula-station/space-station-14?&style=for-the-badge&color=a78bfa&logo=data:image/svg%2Bxml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCIgZmlsbD0ibm9uZSIgc3Ryb2tlPSIjRTBERUY0IiBzdHJva2Utd2lkdGg9IjEuOCIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIiBzdHJva2UtbGluZWpvaW49InJvdW5kIj48cGF0aCBkPSJNMTIgMS41bDkuMSA1LjI1djEwLjVMMTIgMjIuNWwtOS4xLTUuMjVWNi43NXoiLz48cGF0aCBkPSJNMTIuMSA5LjI0QTMuNiAzLjYgMCAxIDAgMTIuMSAxNC43NiIvPjxwYXRoIGQ9Ik0xNS42IDkuOHY0LjRNMTcuNiA5Ljh2NC40TTE0LjYgMTEuMmg0TTE0LjYgMTIuOGg0Ii8%2BPC9zdmc%2BCg==&logoColor=E0DEF4&labelColor=1F1A2E)
![](https://img.shields.io/github/repo-size/nebula-station/space-station-14?color=c4a7e7&label=SIZE&logo=files&style=for-the-badge&logoColor=E0DEF4&labelColor=1F1A2E)

<a href="https://discord.gg/INVITE">
  <img alt="Discord" src="https://img.shields.io/badge/discord-join-a78bfa?style=for-the-badge&logo=discord&logoColor=E0DEF4&labelColor=1F1A2E">
</a>

</div>

<div align="center">
    <h2>• overview •</h2>
    <h3></h3>
</div>

<div align="center">
  This is a <a href="https://spacestation14.com/">Space Station 14</a> server, forked from <a href="https://github.com/Trauma-Station/Trauma-Station">Trauma Station</a>.
</div>
<br>

<details>
<summary>Contributing</summary>

- We are happy to accept contributions from anybody.
- Get in [Discord](https://discord.gg/INVITE) if you want to help.
- Please read the [Contribution Guidelines](/CONTRIBUTING.md) before opening a pull request.

</details>

<details>
<summary>Links</summary>

- [GitHub](https://github.com/nebula-station/space-station-14)
- [Discord server](https://discord.gg/INVITE)

</details>

<details>
<summary>How to connect</summary>

- To connect to the server, use the following address: `ss14s://ADDRESS`
- Open Space Station 14 and paste this link into the server browser or use direct connect.

</details>

<div align="center">
    <h2>• building •</h2>
    <h3></h3>
</div>

<div align="center">
  Refer to <a href="https://docs.spacestation14.com/en/general-development/setup/setting-up-a-development-environment.html">
  the Space Wizards' guide</a> on setting up a development environment for general information.
  <br><br>
  Everything below is done through a single launcher, <code>RUN_THIS.py</code>.
</div>

<details>
<summary>Build dependencies</summary>

- Git
- Python 3.5+
- .NET SDK 10.0

</details>
<details>
<summary>Quick start</summary>

1. Clone this repository
2. Run `RUN_THIS.bat` on Windows, or `python3 RUN_THIS.py` on Linux and macOS
3. Press `e` to init submodules and download the engine
4. Press `3` to build and launch the server and the client
5. Connect to localhost in the client and play

When it is not run in an interactive terminal (e.g. in CI), the launcher only inits submodules and downloads the engine.

</details>
<details>
<summary>Launcher keys</summary>

Keys also work on the Russian keyboard layout.

| Key         | Action                                          |
| ----------- | ----------------------------------------------- |
| `1`         | Launch the client                               |
| `2`         | Launch the server                               |
| `3`         | Launch the server and the client                |
| `4`         | Build only                                      |
| `c`         | Switch config: `Debug` → `Release` → `Tools`    |
| `b`         | Toggle building before launch                   |
| `l`         | Switch language: Русский / English              |
| `u`         | Update the repo (`git pull --ff-only` + engine) |
| `e`         | Update the engine to the pinned version         |
| `q` / `Esc` | Quit                                            |

</details>

<div align="center">
    <h2>• license •</h2>
    <h3></h3>
</div>

<details>
<summary>License</summary>

### Code

All code in this codebase is released under the **AGPL-3.0-or-later** license.

Some files were taken from other repositories and sublicensed from their original licenses.
Keep existing license, copyright, and provenance notices intact. New files should carry
REUSE/SPDX metadata near the top of the file:

```text
SPDX-FileCopyrightText: 2026 <copyright holder>
SPDX-License-Identifier: AGPL-3.0-or-later
```

Full license texts can be found in the `LICENSES/` directory.

---

### Assets

Most media assets are licensed under **CC-BY-SA 3.0**, unless stated otherwise.

- Assets include their own license and copyright information in metadata files
- Example: [asset metadata](https://github.com/space-wizards/space-station-14/blob/master/Resources/Textures/Objects/Tools/crowbar.rsi/meta.json)

---

### Commercial use

Some assets are licensed under **CC-BY-NC-SA 3.0** or similar non-commercial licenses.

> These assets **must be removed** if you intend to use this project commercially.

</details>

<details>
<summary>Attribution</summary>

We organize borrowed content from other forks into dedicated subdirectories.

This helps preserve proper attribution and reduces merge conflicts.

Content inside these subdirectories originates from their respective forks and may include modifications.
Changes are marked with comments around the edited lines.

---

### Fork sources

Subdirectories can be found in `Resources` (prototypes, textures, locale, audio, etc.) and in the C# projects.

| Subdirectory       | Repository                                                  | License            |
| ------------------ | ----------------------------------------------------------- | ------------------ |
| `_ADT`             | https://github.com/AdventureTimeSS14/space_station_ADT      | AGPL-3.0           |
| `_Arcane`          | https://github.com/ArcaneSS14/arcane                        | AGPL-3.0           |
| `_Corvax`          | https://github.com/space-syndicate/space-station-14         | MIT                |
| `_CorvaxGoob`      | https://github.com/space-syndicate/Goob-Station             | AGPL-3.0           |
| `_CorvaxNext`      | https://github.com/space-syndicate/space-station-14-next    | AGPL-3.0           |
| `_DV`              | https://github.com/DeltaV-Station/Delta-v                   | AGPL-3.0           |
| `_EinsteinEngines` | https://github.com/Simple-Station/Einstein-Engines          | AGPL-3.0           |
| `_Factory`         | https://github.com/Trauma-Station/Trauma-Station            | AGPL-3.0           |
| `_FarHorizons`     | https://github.com/Far-Horizons-SS14/Far-Horizons-SS14      | MIT                |
| `_Floofstation`    | https://github.com/Floof-Station/Floof-Station              | AGPL-3.0           |
| `_Gabystation`     | —                                                           | —                  |
| `_Goobstation`     | https://github.com/Goob-Station/Goob-Station                | AGPL-3.0           |
| `_Imp`             | https://github.com/impstation/imp-station-14                | AGPL-3.0           |
| `_Impstation`      | https://github.com/impstation/imp-station-14                | AGPL-3.0           |
| `_Lavaland`        | https://github.com/Goob-Station/Goob-Station                | AGPL-3.0           |
| `_Mono`            | https://github.com/Monolith-Station/Monolith                | AGPL-3.0 / MPL-2.0 |
| `_Nebula`          | https://github.com/nebula-station/space-station-14          | AGPL-3.0           |
| `_NF`              | https://github.com/new-frontiers-14/frontier-station-14     | AGPL-3.0           |
| `_Orion`           | https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14 | AGPL-3.0           |
| `_RMC14`           | https://github.com/RMC-14/RMC-14                            | MIT                |
| `_ShitChap`        | https://github.com/Goob-Station/Goob-Station                | AGPL-3.0           |
| `_Shitmed`         | https://github.com/Goob-Station/Goob-Station                | AGPL-3.0           |
| `_SimpleStation`   | —                                                           | —                  |
| `_Starfall`        | —                                                           | —                  |
| `_StarLight`       | https://github.com/ss14Starlight/space-station-14           | MIT                |
| `_Trauma`          | https://github.com/Trauma-Station/Trauma-Station            | AGPL-3.0           |
| `_White`           | https://github.com/WWhiteDreamProject/wwdpublic             | AGPL-3.0           |

Some forks have their own C# projects:

| Project                 | Repository                                       | License  |
| ----------------------- | ------------------------------------------------ | -------- |
| `Content.Factory.*`     | https://github.com/Trauma-Station/Trauma-Station | AGPL-3.0 |
| `Content.Goobstation.*` | https://github.com/Goob-Station/Goob-Station     | AGPL-3.0 |
| `Content.Lavaland.*`    | https://github.com/Trauma-Station/Trauma-Station | AGPL-3.0 |
| `Content.Medical.*`     | https://github.com/Trauma-Station/Trauma-Station | AGPL-3.0 |
| `Content.Trauma.*`      | https://github.com/Trauma-Station/Trauma-Station | AGPL-3.0 |

---

### Additional sources

The following repositories were used without creating dedicated subdirectories:

| Fork Name        | Repository                                        | License |
| ---------------- | ------------------------------------------------- | ------- |
| Space Station 14 | https://github.com/space-wizards/space-station-14 | MIT     |

</details>

<div align="center">

![License](https://img.shields.io/badge/license-AGPL--3.0-a78bfa?style=for-the-badge&labelColor=1F1A2E)

</div>
