# Aethermancer-Mod-Wiki-Helper
Bepinex mod for Aethermancer to help with wiki management

## Skill Scraper
Press `F2` to run the Skill Scraper. You must be in Pilgrim's rest or in the Overworld.

After scraping the data, it will parse it and generate the database Lua modules to be uploaded to the wiki. These files are stored in the same location as the plugin.

### Caveats
* This does not capture any unique actions like `Living Tome` or the `Wishes` yet, so make sure to not overwrite those on the wiki
* Signature Traits do not capture their monster yet, this is an enhancement to include (probably will be done at the same time I scrape Monster data)
* I do some post processing on the descriptions (see `ReformatDescription`), but there are still some quirks with some skills that are built into the game.
* This covers only some of the `key` words in the Lua module.
* This does not set any `categories` in the Lua module. Both this and the previous point make reconciling the changes extremely tedious currently.