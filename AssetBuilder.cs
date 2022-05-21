using UnityEngine;
using UnityEditor;
using System.IO;
using Microsoft.Win32;
using System.Collections.Generic;

/********************************************************
 * Fiodor's Fabulous NEBULOUS: Flee Command asset bundler
 * Currently this assumes all assets being included
    in the bundle are ship components
 * To use, put this in Assets/Editor in your project
 * After adding it, you will have 'Tools > Build bundles' option in the menu
 * Building with this tool will auto-generate & include the manifest file,
    as well as the ModInfo file.
 * After that you only need to copy to your games mod folder
    either manually or via the copy to game button
 *********************************************************
 */
public class AssetBuilderWin : EditorWindow
{
  static int index, fieldWidth = 300;
  static bool compress = false, showCfg = true, appendmodname = true, writemanifest = true;
  static string exportpath = "Assets/AssetBundles", gamepath = "";

  static ModInfo modinfo = new ModInfo();
  GUIContent assbunTooltip = new GUIContent("Asset bundle:", "Select Asset Bundle to build");
  GUIContent appmodnameTooltip = new GUIContent("Write to its own folder?", "Saves all asset bundles to a folder with the name of the mod");
  GUIContent genmanifestTooltip = new GUIContent("Generate manifest file?", "Auto Generates & includes manifest file");

  [MenuItem("Tools/Build Asset Bundle %b")]
  static void Init() {
    // Get existing open window or if none, make a new one:
    AssetBuilderWin window = (AssetBuilderWin) EditorWindow.GetWindow(typeof(AssetBuilderWin));
    window.Show();

    // Get registry value for game installl path
    string key = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Steam App 887570";
    gamepath = (string) Registry.GetValue(key, "InstallLocation", "");
  }

  void OnGUI() {
    GUILayout.Space(10);
    GUILayout.Label("NEBULOUS: Fleet Command mod export", EditorStyles.whiteLabel, Utils.width(300));
    GUILayout.Space(3);
    if (AssetDatabase.GetAllAssetBundleNames().Length == 0) {
      EditorGUILayout.HelpBox("No asset bundle found.\n"+
                              "No options available until an asset bundle has been created.",
                              MessageType.Warning);
      return;
    }

    // Configuration
    showCfg  = GUILayout.Button("Show config options") ? !showCfg : showCfg;
    if (showCfg) {
      EditorGUILayout.HelpBox("Store mod name, version & description in the editor preferences using the asset bundle as the key.",
                              MessageType.Info);
      GUILayout.Space(10);
      fieldWidth = EditorGUILayout.IntSlider(fieldWidth, 0, 1000);
      if (GUILayout.Button("Load", Utils.width(200))) getConfig();
      if (GUILayout.Button("Save", Utils.width(200))) saveConfig();
    }

    // Mod data proper
    GUILayout.Space(15);
    index    = EditorGUILayout.Popup(assbunTooltip, index, AssetDatabase.GetAllAssetBundleNames());
    GUILayout.Space(5);
    GUILayout.Label("Export bundle to folder (inside Assets):", Utils.width(300));
    GUILayout.Space(2);
    exportpath     = EditorGUILayout.TextField(exportpath, Utils.width(300));
    compress       = EditorGUILayout.Toggle("Compress?", compress, Utils.width(200));
    GUILayout.Space(2);
    modinfo.name        = EditorGUILayout.TextField("Mod name", modinfo.name, Utils.width(fieldWidth));
    modinfo.version         = EditorGUILayout.TextField("Mod ver", modinfo.version, Utils.width(fieldWidth));
    modinfo.gameversion        = EditorGUILayout.TextField("Game ver", modinfo.gameversion, Utils.width(fieldWidth));
    modinfo.description = EditorGUILayout.TextField("Mod description", modinfo.description, Utils.width(fieldWidth));
    GUILayout.Space(5);
    appendmodname  = EditorGUILayout.Toggle(appmodnameTooltip, appendmodname, Utils.width(fieldWidth));
    writemanifest  = EditorGUILayout.Toggle(genmanifestTooltip, writemanifest, Utils.width(300));
    GUILayout.Space(5);
    if (GUILayout.Button("Build", Utils.width(200))) build(exportpath, compress);

    GUILayout.Space(10);
    gamepath       = EditorGUILayout.TextField("Game path", gamepath);
    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("Locate game folder", Utils.width(150))) {
      string installpath = EditorUtility.OpenFolderPanel("NEBULOUS Fleet install folder", gamepath, "");
      if (installpath != null) gamepath = installpath;
    }
    // locate game
    if (GUILayout.Button("Copy to game", Utils.width(150))) {
      string bundlename = AssetDatabase.GetAllAssetBundleNames()[index];
      Utils.copyToMods(validate(exportpath), $"{gamepath}/Mods/{modinfo.name}", bundlename, modinfo.name);
    }
    EditorGUILayout.EndHorizontal();
  }


  static string validate(string path) {
    // add Assets to the path if it's not already there
    path = path.StartsWith("Assets/")? path : $"Assets/{path}";
    if (appendmodname) {
      // make it so the path always includes a folder with the modinfo.name
      path = path.EndsWith(modinfo.name)? path : $"{path}/{modinfo.name}";
    }
    path.Replace("//","/");
    path.Replace("\\","/");
    return path;
  }

  static void build(string bundlepath, bool compress) {
    if (string.IsNullOrEmpty(modinfo.name) ||  string.IsNullOrEmpty(modinfo.version)) {
      Debug.LogError("One of the values for mod name, mod description or mod version was left empty.");
      return;
    }
    bundlepath = validate(bundlepath);
    if(!Directory.Exists(bundlepath)) {
      Directory.CreateDirectory(bundlepath);
    }

    string targetBundle = AssetDatabase.GetAllAssetBundleNames()[index];
    // get all assets associated to this asset bundle
    string[] assetNames = AssetDatabase.GetAssetPathsFromAssetBundle(targetBundle);
    // write mod manifest
    if (writemanifest) Utils.WriteManifest(bundlepath, targetBundle, assetNames);
    // obtain assets again, to include the new/updated manifest file
    assetNames = AssetDatabase.GetAssetPathsFromAssetBundle(targetBundle);
    AssetBundleBuild[] assBunBuild = new AssetBundleBuild[1];
    assBunBuild[0].assetBundleName = targetBundle;
    assBunBuild[0].assetNames = assetNames;

    var options = compress? BuildAssetBundleOptions.UncompressedAssetBundle : BuildAssetBundleOptions.ChunkBasedCompression;
    BuildPipeline.BuildAssetBundles(bundlepath,
                                    assBunBuild,
                                    options,
                                    BuildTarget.StandaloneWindows);
    // write asset manifest mod info
    modinfo.assetbundle = targetBundle;
    Utils.WriteModInfo(bundlepath, modinfo);
    // correct the path as it appears in the text field
    exportpath = bundlepath;
    // TODO delete manifest?
  }

 // per bundle config in editor prefs, too lazy to use c# overcomplicated config
  static void getConfig() {
    string targetBundle = AssetDatabase.GetAllAssetBundleNames()[index];
    if (!EditorPrefs.HasKey(targetBundle)) {
      EditorUtility.DisplayDialog("Error", $"No config found for this Asset Bundle '{targetBundle}'.", "OK");
      return;
    }

    modinfo.name        = EditorPrefs.GetString($"{targetBundle}-modinfo.name");
    modinfo.version     = EditorPrefs.GetString($"{targetBundle}-modinfo.version");
    modinfo.description = EditorPrefs.GetString($"{targetBundle}-modinfo.description");
    modinfo.gameversion = EditorPrefs.GetString($"{targetBundle}-modinfo.gameversion");
    gamepath            = EditorPrefs.GetString($"{targetBundle}-gamepath");
    appendmodname       = EditorPrefs.GetBool($"{targetBundle}-appendmodname");
  }

  static void saveConfig() {
    string targetBundle = AssetDatabase.GetAllAssetBundleNames()[index];
    // store somethign as the bundle name to use as main key
    EditorPrefs.SetString(targetBundle, "x");
    EditorPrefs.SetString($"{targetBundle}-modinfo.name",        modinfo.name);
    EditorPrefs.SetString($"{targetBundle}-modinfo.version",     modinfo.version);
    EditorPrefs.SetString($"{targetBundle}-modinfo.description", modinfo.description);
    EditorPrefs.SetString($"{targetBundle}-modinfo.gameversion", modinfo.gameversion);
    EditorPrefs.SetString($"{targetBundle}-gamepath",        gamepath);
    EditorPrefs.SetBool($"{targetBundle}-appendmodname",    appendmodname);
  }

}
