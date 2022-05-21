using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Win32;

/********************************************************
 * Fiodor's Fabulous NEBULOUS: Flee Command asset bundler
 * Currently this assumes all assets being included
    in the bundle are ship components
 * To use, put this in Assets/Editor in your project
 * After adding it, you will have 'Tools > Make Resources' option in the menu
 * Remember to mark the resulting Resource file with the appropiate asset bundle
 *********************************************************
 */
public class ResourceBuilderWin : EditorWindow
{
  // TODO: read files OR save config
  private bool showCfg = false;
  private int fieldWidth = 300;
  private string defPath = "Assets/";

  private Vector2 scrollPos;
  private GUIContent unitTooltip = new GUIContent("Unit", "What unit is this resource measured in?");
  private GUIContent plusTooltip = new GUIContent("+", "Add another resource definition");
  private GUIContent minusTooltip = new GUIContent("-", "Remove this resource definition");

  static private List<ShipResource> resources = new List<ShipResource>();
  [MenuItem("Tools/Build Resources file %r")]
  static void Init() {
    ResourceBuilderWin window = (ResourceBuilderWin) EditorWindow.GetWindow(typeof(ResourceBuilderWin));
    window.Show();
  }

  void OnGUI() {
    GUILayout.Space(10);
    GUILayout.Label("NEBULOUS: Fleet Command Ship Resource builder", EditorStyles.whiteLabel, Utils.width(300));
    GUILayout.Space(3);
    showCfg  = GUILayout.Button("Show config options") ? !showCfg : showCfg;
    if (showCfg) {
      fieldWidth = EditorGUILayout.IntSlider(fieldWidth, 0, 1000);
      if (GUILayout.Button("Load", Utils.width(200))) getConfig();
      if (GUILayout.Button("Save", Utils.width(200))) saveConfig();
    }
    EditorGUIUtility.labelWidth = 100;

    // Resources menu
    GUILayout.Space(10);
    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.PrefixLabel("Resources", EditorStyles.boldLabel);
    if (GUILayout.Button(plusTooltip, Utils.width(25))) resources.Add(new ShipResource());
    GUILayout.Space(5);
    if (GUILayout.Button(minusTooltip, Utils.width(25))) {
      if (resources.Count > 0) resources.RemoveAt(resources.Count-1);
    }
    GUILayout.Space(5);
    if (GUILayout.Button("Clear", Utils.width(50))) resources.Clear();
    EditorGUILayout.EndHorizontal();

    defPath = EditorGUILayout.DelayedTextField("Save at:", defPath, Utils.width(fieldWidth));
    if (GUILayout.Button("Write Resources file", Utils.width(200))) {
      string path = EditorUtility.SaveFilePanel("Resources file", defPath, "resources", "xml");
      if (path != null && !string.IsNullOrEmpty(path)){
        defPath = path;
        WriteResourceFile(path);
      }
    }
    GUILayout.Space(5);
    EditorGUILayout.BeginVertical();
    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
    for (int i=0; i<resources.Count; i++) {
      ShipResource res = resources[i];
      res.name = EditorGUILayout.DelayedTextField("Name:", res.name, Utils.width(fieldWidth));
      res.unit = EditorGUILayout.DelayedTextField(unitTooltip, res.unit, Utils.width(fieldWidth));
      res.type = (ShipResource.ResourceType) EditorGUILayout.EnumPopup("Resource type:", res.type, Utils.width(fieldWidth));
      EditorGUILayout.BeginHorizontal();
      GUILayout.Space(fieldWidth-20);
      if (GUILayout.Button("-", Utils.width(25))) resources.Remove(res);
      EditorGUILayout.EndHorizontal();
    }
    EditorGUILayout.EndScrollView();
    EditorGUILayout.EndVertical();
  }

  void WriteResourceFile(string manifestpath) {
    // string manifestpath = $"{path}/resources.xml";
    XmlWriter writer = XmlWriter.Create(manifestpath, new XmlWriterSettings(){Indent=true});
    writer.WriteStartDocument();
    writer.WriteStartElement("ResourceFile");
    writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
    writer.WriteAttributeString("xmlns", "xds", null, "http://www.w3.org/2001/XMLSchema");
    writer.WriteStartElement("Resources");
    foreach (var resource in resources) {
      writer.WriteStartElement("ResourceType");
      writer.WriteElementString("Name", resource.name);
      writer.WriteElementString("Unit", resource.unit);
      writer.WriteElementString("ScheduleMode", resource.type.ToString());
      writer.WriteEndElement();
    }
    writer.WriteEndElement();
    writer.WriteEndDocument();
    writer.Close();
  }

  void getConfig() {
    if (!EditorPrefs.HasKey("resourcebuilder")) {
      EditorUtility.DisplayDialog("Error", "No config found for the Resource Builder.", "OK");
      return;
    }
    resources.Clear();
    int count  = EditorPrefs.GetInt("resourcebuilder-count");
    fieldWidth = EditorPrefs.GetInt("resourcebuilder-fieldwidth");
    for (int i=0; i<count; i++) {
      var res = new ShipResource();
      res.name = EditorPrefs.GetString($"resourcebuilder-{i}-name");
      res.unit = EditorPrefs.GetString($"resourcebuilder-{i}-unit");
      Enum.TryParse(EditorPrefs.GetString($"resourcebuilder-{i}-type"), true, out res.type);
      resources.Add(res);
    }
  }

  void saveConfig() {
    // store somethign as key
    EditorPrefs.SetString("resourcebuilder", "x");
    EditorPrefs.SetInt("resourcebuilder-count", resources.Count);
    EditorPrefs.SetInt("resourcebuilder-fieldwidth", fieldWidth);

    for (int i=0; i<resources.Count; i++) {
      var res = resources[i];
      EditorPrefs.SetString($"resourcebuilder-{i}-name", res.name);
      EditorPrefs.SetString($"resourcebuilder-{i}-unit", res.unit);
      EditorPrefs.SetString($"resourcebuilder-{i}-type", res.type.ToString());
    }
  }
}

public class ShipResource {
  public string name;
  public string unit;
  public enum ResourceType {
    Ticked,
    Pooled
  }
  public ResourceType type;
}
