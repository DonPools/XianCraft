using Microsoft.Xna.Framework;
using XianCraft.Components;

namespace XianCraft;

public class Helper
{
    public static Vector2 WorldToAbsScreenCoords(float tileX, float tileY, int tileWidth, int tileHeight)
    {
        float screenX = (tileX - tileY) * tileWidth / 2f;
        float screenY = (tileX + tileY) * tileHeight / 2f;
        return new Vector2(screenX, screenY);
    }

    public static Vector2 WorldToScreenCoords(
        Vector2 worldPos, int tileWidth, int tileHeight,
        Camera camera, Vector2 origin = default(Vector2))
    {
        float screenX = (worldPos.X - worldPos.Y) * tileWidth / 2f;
        float screenY = (worldPos.X + worldPos.Y) * tileHeight / 2f;
        var absScreenPos = new Vector2(screenX, screenY);
        var relPos = (absScreenPos + origin - camera.Position) * camera.Zoom;

        return new Vector2(relPos.X + camera.ViewportWidth / 2f, relPos.Y + camera.ViewportHeight / 2f);
    }


    public static Vector2 ScreenToTileCoords(float x, float y, int tileWidth, int tileHeight)
    {
        float tileY = y / tileHeight;
        float tileX = x / tileWidth;
        return new Vector2(tileY + tileX, tileY - tileX);
    }
}