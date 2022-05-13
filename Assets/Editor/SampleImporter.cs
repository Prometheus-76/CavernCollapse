using UnityEngine;
using System.IO;
using UnityEditor.AssetImporters;

[ScriptedImporter(1, "ccd")]
public class SampleImporter : ScriptedImporter
{
    // Automatically convert .ccd files to .txt when importing into the resources folder
    public override void OnImportAsset(AssetImportContext context)
    {
        // Construct the new TextAsset (.txt) file
        TextAsset ccdFile = new TextAsset(File.ReadAllText(context.assetPath));

        // Tell Unity that this newly created file is what we want to import instead
        context.AddObjectToAsset("main", ccdFile);
        context.SetMainObject(ccdFile);
    }
}