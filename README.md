# Hyperborea

Load and explore any zone in relative safety, mainly for taking GPoses in now inaccessible locations.

![Hyperborea UI preview](https://github.com/kawaii/Hyperborea/assets/12242877/54b5588b-9dd7-4b5d-8238-710aae65cf68)

Hyperborea operates by employing a packet filter while enabled. Only packets required to maintain your connection to the game server in such a way that you still appear online are passed through (known as "keepalive" packets). Anything else, such as movement, chat, actions - are all filtered from your client. To any observer you would just appear to be idle/AFK.

[![Hyperborea v1.0.0.4](https://markdown-videos-api.jorgenkh.no/url?url=https%3A%2F%2Fyoutu.be%2FxpX5UT7vSE0)](https://youtu.be/xpX5UT7vSE0)
[![Hyperborea v1.0.0.0](https://markdown-videos-api.jorgenkh.no/url?url=https%3A%2F%2Fyoutu.be%2FTilHuQlsNg4)](https://youtu.be/TilHuQlsNg4)

## Download & Install

1. Open the Dalamud Settings menu, this can be done through the button at the bottom of the plugin installer or by typing `/xlsettings` in the chat.
2. Under Custom Plugin Repositories (in the Experimental tab), enter `https://puni.sh/api/repository/kawaii` into one of the empty input fields.
3. Click the `+` button.
4. Save and close the settings menu.
5. Search for `Hyperborea` in the `All Plugins` section of the Plugin Installer.

## Warnings

- Many zones will spawn you under the world geometry if you use the default `0,0` coordinates. There is a built-in feature to override your position and teleport around to alleviate this a little.
- Attempting to walk through a zone loading border (i.e. the string of blue orbs seen in city states that take you from one zone to the next) will softlock your game and require a restart.
- Some zones, particularly raids and trials will appear broken or incomplete. The game uses various events, scenes, and other tricks to accomplish transitions and the like. I am actively investigating how to include tools to manage these within Hyperborea.
