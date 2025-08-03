


namespace XianCraft.Utils;
﻿using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Xna.Framework;

public class UtilsHelper
{
    public static string GetShaderExtension()
    {
        // use reflection to figure out if Shader.Profile is OpenGL (0) or DirectX (1),
        // may need to be changed / fixed for future shader profiles

        var shaderExtension = string.Empty;

        var assembly = typeof(Game).GetTypeInfo().Assembly;
        Debug.Assert(assembly != null);

        var shaderType = assembly.GetType("Microsoft.Xna.Framework.Graphics.Shader");
        Debug.Assert(shaderType != null);
        var shaderTypeInfo = shaderType.GetTypeInfo();
        Debug.Assert(shaderTypeInfo != null);

        // https://github.com/MonoGame/MonoGame/blob/develop/MonoGame.Framework/Graphics/Shader/Shader.cs#L47
        var profileProperty = shaderTypeInfo.GetDeclaredProperty("Profile");
        var value = (int)profileProperty.GetValue(null);

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