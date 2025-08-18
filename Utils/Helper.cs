using Microsoft.Xna.Framework;
using XianCraft.Components;
using System;
using System.Diagnostics;
using System.Reflection;

namespace XianCraft.Utils;

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

    public static string GetShaderExtension()
    {
        var assembly = typeof(Game).GetTypeInfo().Assembly;
        Debug.Assert(assembly != null);

        var shaderType = assembly.GetType("Microsoft.Xna.Framework.Graphics.Shader");
        Debug.Assert(shaderType != null);
        var shaderTypeInfo = shaderType.GetTypeInfo();
        Debug.Assert(shaderTypeInfo != null);

        // https://github.com/MonoGame/MonoGame/blob/develop/MonoGame.Framework/Graphics/Shader/Shader.cs#L47
        var profileProperty = shaderTypeInfo.GetDeclaredProperty("Profile");
        var value = (int)profileProperty.GetValue(null);

        // use reflection to figure out if Shader.Profile is OpenGL (0) or DirectX (1),
        // may need to be changed / fixed for future shader profiles

        string shaderExtension;
        switch (value)
        {
            case 0:
                // OpenGL
                shaderExtension = "ogl";
                break;
            case 1:
                // DirectX
                shaderExtension = "dx11";
                break;
            default:
                throw new InvalidOperationException("Unknown shader profile.");
        }

        return shaderExtension;
    }
}