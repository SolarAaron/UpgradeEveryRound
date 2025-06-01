# Changelog

## [2.0.3] - 2025-6-1
### Fixed
- Bug causing upgrades to do nothing for non-hosts in multiplayer on beta branch

## [2.0.2] - 2025-5-31
### Fixed
- Incompatibility with ItemBundles resulting in levels not loading

## [2.0.1] - 2025-5-31
### Added
- Compatibility with new beta upgrades
### Fixed
- Bug causing MoreUpgrades upgrades to be unselectable and brick the menu

## [2.0.0] - 2025-5-30
### Added
- Compatibility with REPOLib based upgrade mods thanks to [SolarAaron](https://github.com/SolarAaron)
### Changed
- UI Refactored, can now handle more than 8 upgrades.
### Code
- More refactoring and moving stuff around
### Fixed
- Only host upgrades applying
- Clients being able to select more upgrades after reloading a save

## [1.3.0] - 2025-5-29
### Code
- Major refactor in how networking is handled to improve reliability.
### Fixed
- Bugs related race conditions and networking

## [1.2.4] - 2025-5-28
### Fixed
- Upgrade menu is now always closeable by pressing ESC in case of plugin malfunction
- Bug causing plugin config to not properly initialize on new saves.
- Hopefully make menu popping up more reliable

## [1.2.3] - 2025-5-27
### Fixed
- Bug causing only host to recieve upgrades

## [1.2.2] - 2025-5-26
### Fixed
- Bug causing players to spawn in incorrect positions

## [1.2.1] - 2025-5-6
### Changed
- Moved code around a little bit

## [1.2.0] - 2025-5-6
### Added
- Host config now syncs to other players in server
- Config options for toggling specific upgrades
- Stat menu now remains open while selecting an upgrade

### Changed
- If random options and multiple upgrades per round are enabled, new choices will be presented for each upgrade now
- Moved menu to the right to allow for stats to be displayed on the left

### Fixed
- Fixed bug allowing players to spam upgrade buttons to get multiple before the menu closed

## [1.1.0] - 2025-3-29
### Added

- Config options for increasing number of upgrades as well as making selection random

### Changed

- Players can now recieve the menu to choose an upgrade at any level in case it was accidentally closed, or they join late.

### Fixed

- Bug causing menus to break under various circumstances.