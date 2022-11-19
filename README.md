# Unity-Tilemap-To-Image
Convert large tilemaps to images, supports rotated/flipped tiles

Requires all images used in the tilemap to be marked as Read/Write enabled in their settings. Requires access to System.Drawing.Imaging, setting API compatibility level to .NET Framework solves this issue. 

This is a rewrite of this script https://github.com/leocub58/Tilemap-to-PNG-Unity/ fixing many issues, making it English and enabling a couple more file formats as output. I originally rewrote it because my tilemap's resulting image exceeded the 16384 pixel limit, but the original also did not have any support for tiles that were transformed (rotated/flipped).

To use: Add the script to your tilemap, and fill in the fields before pressing generate.

Follow my socials if you are interested in the development of a getting over it/jump king style climbing game.

Twitter: https://twitter.com/ZanthousStudios

Youtube: https://www.youtube.com/@ZanthousStudios
