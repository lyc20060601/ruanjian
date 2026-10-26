using UnityEditor;
using UnityEngine;

public sealed class JellyBrightArtSetup : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.EndsWith("/CandyStudio.hdr"))return;
        var importer=(TextureImporter)assetImporter;
        importer.textureShape=TextureImporterShape.TextureCube;
        importer.generateCubemap=TextureImporterGenerateCubemap.AutoCubemap;
        importer.mipmapEnabled=true;importer.maxTextureSize=256;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.sRGBTexture=false;
    }
}
