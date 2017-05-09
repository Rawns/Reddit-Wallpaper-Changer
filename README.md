# [Reddit Wallpaper Changer](https://www.reddit.com/r/rwallpaperchanger/)
Based on [RWC-Source](https://github.com/JosephRobidoux/RWC-Source) originally created by [Joseph Robidoux](https://github.com/JosephRobidoux)

![Reddit Wallpaper Changer](http://i.imgur.com/jVhWthE.jpg "Reddit Wallpaper Changer")

# About
Reddit Wallpaper Changer is a lightweight C# application for Windows that will scrape Reddit for fresh desktop wallpapers. You can specify which subs to scrape from and how oftern to rotate your wallpaper.

# Current Version - 1.0.13.0
- Added: Migrated from XML files to SQLite for storing wallpaper data
- Added: Automatic migration of existing Blacklist to new SQLite database
- Added: You can now 'Favourite' wallpapers for reuse!
- Added: Option to auto-save Favourite wallpapers
- Added: Endless wallpaper history instead of 'Per Session'
- Added: Added some database housekeeping options
- Added: Backup/Restore options for database
- Added: You can now disable automatic update checks
- Changed: Increased wallpaper thumbnail size in History, Favourites and Blacklist menus
- Changed: Wallpaper titles now multi-lined in History, Favourites and Blacklist menus 
- Changed: Wallpaper thumbs now saved to SQLite instead of being downloaded every time
- Changed: 'Suppress Duplicates' will still be limited to 'per session'
- Changed: New icon set for the Menu buttons 
- Changed: UI adjustments to encorporate Favourites and Database options 
- Changed: Import/Export routine updated to include 1.0.13 features
- Changed: 'Monitors' menu should now support up to four detected monitors 
- Changed: Settings 'Save' button now saves changes but won't trigger a wallpaper change 
- Changed: Changes to the backend code for searching for/applying wallpapers 
- Fixed: Unable to save wallpapers with special characters in their name  
- Fixed: Some spelling errors in log file
- Fixed: Changes to the 'Fade' setting were not actually being saved
- Fixed: Various code changes
- Fixed: Some minor logging errors
- Fixed: Other small bugs

# Installation
Download the latest MSI installer from the [releases page](https://github.com/Rawns/Reddit-Wallpaper-Changer/releases) or install using [Chocolatey](https://chocolatey.org/packages/reddit-wallpaper-changer/)

You will need to have the **Microsoft Visual C++ 2010 Redistributable Package (x86)** package installed, which is a prerequisite for SQLite. You can [download the installer from Microsoft](https://www.microsoft.com/en-gb/download/details.aspx?id=5555). 
