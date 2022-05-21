using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;

public class Utils {
  public static GUILayoutOption width(float w) => GUILayout.Width(w);


  /*
   * Group all prefab paths by component type for the manifest
   */
  public static Dictionary<string, List<string>> getTypes(string[] prefabPaths) {
    Dictionary<string, List<string>> pathTypes = new Dictionary<string, List<string>>(){
      {"Components", new List<string>()    },
      {"ResourceFile", new List<string>(1)  }
    };

    for (int i=0; i<prefabPaths.Length; i++) {
      string fileExt = Path.GetExtension(prefabPaths[i]);
      switch (fileExt) {
        case ".prefab":
          var go = PrefabUtility.LoadPrefabContents(prefabPaths[i]);
          var component = go.GetComponent<Ships.HullComponent>();
          if (component == null)
            throw new Exception("No hull component found, current version does not support other type of components.\n Please remove from Asset Bundle to build.");
          pathTypes["Components"].Add(prefabPaths[i]);
          PrefabUtility.UnloadPrefabContents(go);
        break;

        case ".xml":
          // if it is manifest, continue without doing nothing (dont't need to write the manifest in the manifest)
          if (Path.GetFileName(prefabPaths[i]).Equals("manifest.xml")) continue;
          if (Path.GetFileName(prefabPaths[i]).Equals("resources.xml")) {
            // there should only be ONE and only ONE resource file
            pathTypes["ResourceFile"] = new List<string>(){prefabPaths[i]};
          }
        break;
      }
    }
    return pathTypes;
  }

  /*
   * Copy only the files for the mod to work. No unnecesary .meta or .manifest files!
   */
  public static void copyToMods(string from, string destination, string bundlename, string modname) {
    // thanks microsoft for gracefully providing this code to copy directories
    // https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-
    // from = validate(from);

    var dir = new DirectoryInfo(from);
    if (!dir.Exists) throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
    if (!Directory.Exists(destination)) Directory.CreateDirectory(destination);
    // copy only the required files: asset bundles & modinfo
    string bundleOrigin = Path.Combine(from, bundlename);
    string bundleDest   = Path.Combine(destination, bundlename);
    File.Copy(bundleOrigin, bundleDest, true);

    string modinfoOrigin = Path.Combine(from, "ModInfo.xml");
    string modinfoDest   = Path.Combine(destination, "ModInfo.xml");
    File.Copy(modinfoOrigin, modinfoDest, true);

    string modOrigin = Path.Combine(from, modname);
    string modDest   = Path.Combine(destination, modname);
    File.Copy(modOrigin, modDest, true);
  }

  public static void WriteManifest(string manifestpath, string bundleName, string[] prefabs) {
    manifestpath = $"{manifestpath}/manifest.xml";
    XmlWriter writer = XmlWriter.Create(manifestpath, new XmlWriterSettings(){Indent=true});
    writer.WriteStartDocument();
    writer.WriteStartElement("BundleManifest");
    writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
    writer.WriteAttributeString("xmlns", "xds", null, "http://www.w3.org/2001/XMLSchema");
    writer.WriteElementString("BasePath", "");
    writer.WriteElementString("Namespace", bundleName);

    foreach (var kvp in Utils.getTypes(prefabs)) {
      Debug.Log($"{kvp.Key}:{kvp.Value[0]}");
      writeModSection(writer, kvp.Key, kvp.Value.ToArray());
    }
    writer.WriteEndDocument();
    writer.Close();
    // to make the manifest file be included in current asset bundle
    // refresh the asset database to account for new (created this frame) manifest file
    AssetDatabase.Refresh();
    var manifestasset = AssetImporter.GetAtPath(manifestpath);
    if (manifestasset == null) {
      Debug.LogWarning($"unable to get manifest file @{manifestpath}. Try again.");
    }
    // associate to current asset bundle
    manifestasset.SetAssetBundleNameAndVariant(bundleName, "");
  }

  public static void writeModSection(XmlWriter writer, string modSection, string[] prefabs) {

    switch (modSection) {
      case "Components":
        writer.WriteStartElement(modSection);
        foreach (string prefabpath in prefabs) {
          string prefabname = Path.GetFileNameWithoutExtension(prefabpath);
          writer.WriteStartElement("Entry");
          writer.WriteAttributeString("Name",    prefabname);
          writer.WriteAttributeString("Address", prefabpath);
          writer.WriteEndElement();
        }
        writer.WriteEndElement();
      break;

      case "ResourceFile":
        foreach (string prefabpath in prefabs) {
          writer.WriteElementString("ResourceFile", prefabpath);
        }
      break;

    }
  }

  public static void WriteModInfo(string path, ModInfo mod) {
    XmlWriter writer = XmlWriter.Create($"{path}/ModInfo.xml", new XmlWriterSettings(){Indent=true});
    writer.WriteStartDocument();
    writer.WriteStartElement("ModInfo");
    writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
    writer.WriteAttributeString("xmlns", "xds", null, "http://www.w3.org/2001/XMLSchema");
    writer.WriteElementString("ModName",        mod.name);
    writer.WriteElementString("ModDescription", mod.description);
    writer.WriteElementString("ModVer",         mod.version);
    writer.WriteElementString("GameVer",        mod.gameversion);
    writer.WriteStartElement("AssetBundles");
    writer.WriteElementString("string",         mod.assetbundle);
    writer.WriteEndElement(); //asset bundle
    writer.WriteEndElement(); //bundle mani
    writer.WriteEndDocument();
    writer.Close();
  }
}

public class ModInfo {
  public string name;
  public string description;
  public string version;
  public string gameversion;
  public string assetbundle;
}
