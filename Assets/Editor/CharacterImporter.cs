using UnityEditor;
namespace SunsetState.Editor
{
    public sealed class CharacterImporter:AssetPostprocessor
    {
        void OnPreprocessModel(){if(!assetPath.Contains("/Characters/"))return;var importer=(ModelImporter)assetImporter;importer.animationType=ModelImporterAnimationType.Legacy;importer.importAnimation=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;}
    }
}
