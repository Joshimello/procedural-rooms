using UnityEditor;
using UnityEngine;
using System.IO;

public static class PackageExporter
{
    private const string MenuPath = "Tools/Export ProceduralRooms.unitypackage";
    private const string OutputFileName = "ProceduralRooms.unitypackage";

    // Called via: Unity -executeMethod PackageExporter.ExportBatch
    // Writes ProceduralRooms.unitypackage next to the Assets folder.
    public static void ExportBatch()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string savePath = Path.Combine(projectRoot, OutputFileName);

        string[] folders = new[]
        {
            "Assets/Furnitures",
            "Assets/Scripts",
        };

        AssetDatabase.ExportPackage(
            folders,
            savePath,
            ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);

        Debug.Log($"Batch export complete: {savePath}");
    }

    [MenuItem(MenuPath)]
    public static void Export()
    {
        string[] folders = new[]
        {
            "Assets/Furnitures",
            "Assets/Scripts",
        };

        string defaultName = "ProceduralRooms.unitypackage";
        string savePath = EditorUtility.SaveFilePanel(
            "Export Unity Package",
            "",
            defaultName,
            "unitypackage");

        if (string.IsNullOrEmpty(savePath))
        {
            return;
        }

        AssetDatabase.ExportPackage(
            folders,
            savePath,
            ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);

        Debug.Log($"Package exported to: {savePath}");
        EditorUtility.RevealInFinder(savePath);
    }
}
