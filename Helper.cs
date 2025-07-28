using Microsoft.Xna.Framework;

namespace XianCraft;

public class Helper
{
    public static Vector2 TileToScreenCoords(int tileX, int tileY, int tileWidth, int tileHeight)
    {
        float screenX = (tileX - tileY) * tileWidth / 2f;
        float screenY = (tileX + tileY) * tileHeight / 2f;
        return new Vector2(screenX, screenY);
    }

    public static Vector2 ScreenToTileCoords(float x, float y, int tileWidth, int tileHeight)
    {
        float tileY = y / tileHeight;
        float tileX = x / tileWidth;
        return new Vector2(tileY + tileX, tileY - tileX);
    }

}