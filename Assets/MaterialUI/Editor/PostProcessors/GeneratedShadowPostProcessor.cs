// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR

namespace MaterialUI
{
    /// <summary>
    /// Used when a new texture is created by the
    /// shadow generator. It turns the texture into a sprite and
    /// applies the right settings to apply to an image.
    /// </summary>
    public class GeneratedShadowPostProcessor : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath.Contains(ShadowGenerator.generatedShadowsDirectory))
            {
                TextureImporter importer = assetImporter as TextureImporter;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.wrapMode = TextureWrapMode.Clamp;
                if (ShadowGenerator.ShadowSpriteBorder != null)
                {
                    importer.spriteBorder = (Vector4)ShadowGenerator.ShadowSpriteBorder;
                    ShadowGenerator.ShadowSpriteBorder = null;
                }
                importer.mipmapEnabled = false;
                importer.textureType = TextureImporterType.Sprite;

                Object asset = AssetDatabase.LoadAssetAtPath(importer.assetPath, typeof(Texture2D));
                if (asset)
                {
                    EditorUtility.SetDirty(asset);
                }
                else
                {
#if UNITY_5_5_OR_NEWER
                    importer.textureType = TextureImporterType.Default;
#else
                    importer.textureType = TextureImporterType.Default;
#endif
                }
            }
        }
    }
}

#endif