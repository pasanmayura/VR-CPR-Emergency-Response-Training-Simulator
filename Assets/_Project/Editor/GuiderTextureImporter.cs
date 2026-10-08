using UnityEditor;

public class GuiderTextureImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Resources/guider/") ||
            !assetPath.EndsWith("rp_mei_posed_001_norm_2k.jpg"))
            return;

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.NormalMap;
    }
}
