using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using _MaxedOutBase.Scripts.Editor.Enums;
using UnityEditor;

namespace _MaxedOutBase.Scripts.Editor.Utilities
{
  public class CodeUtilities
  {
    private static Encoding _encoding;

    private const string ScriptRoot = "Assets/Scripts/";

    private static readonly string[] _searchInFolders =
    {
      "Assets/Scripts/",
      "Assets/Tests/Base"
    };

    private static List<string> GetContextNames(string[] filter)
    {
      string[] guids = AssetDatabase.FindAssets("t:Script", filter);
      List<string> paths = new List<string>();

      foreach (string guid in guids)
      {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        if (path.Contains("Context.cs"))
          paths.Add(path.Substring(path.LastIndexOf("/", StringComparison.Ordinal) + 1).Replace(".cs", ""));
      }

      return paths;
    }

    public static List<string> GetContextNames()
    {
      return GetContextNames(_searchInFolders);
    }

    public static string GetContextPath(int index)
    {
      string[] guids = AssetDatabase.FindAssets("t:Script", _searchInFolders);
      List<string> paths = new List<string>();

      foreach (string guid in guids)
      {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        if (path.Contains("Context.cs"))
          paths.Add(path);
      }

      return paths[index];
    }

    public static bool HasSelectedFolder()
    {
      if (Selection.objects.Length == 1)
      {
        foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
        {
          var path = AssetDatabase.GetAssetPath(obj);
          if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
          {
            return true;
          }
        }
      }

      return false;
    }

    public static string GetSelectedFolder(TemplateType type)
    {
      return GetSelectedFolder(type.ToString());
    }

    public static string GetSelectedFolder(string type)
    {
      if (Selection.objects == null)
      {
        return SelectedFolder(type);
      }

      if (Selection.objects.Length == 1)
      {
        foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
        {
          var path = AssetDatabase.GetAssetPath(obj);
          if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
          {
            return path;
          }
        }
      }

      return SelectedFolder(type);
    }

    private static string SelectedFolder(string type)
    {
      if (type == TemplateType.Context.ToString())
        return ScriptRoot + "Config";
      if (type == TemplateType.Property.ToString())
        return ScriptRoot + "Properties";

      return ScriptRoot + type;
    }

    public static void SaveFile(string data, string filename)
    {
//      if (File.Exists(filename) == false)
//      {
      using (StreamWriter outfile = new StreamWriter(new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write),
        Encoding.Default))
      {
        outfile.Write(data);
      }
//      }

//      AssetDatabase.Refresh();
    }

    public static string LoadScript(string path)
    {
//      Debug.Log(path);
      try
      {
        _encoding = GetEncoding(path);

        string data;
        using (var theReader = new StreamReader(path, _encoding))
        {
          data = theReader.ReadToEnd();
          theReader.Close();
        }

        return data;
      }
      catch (Exception e)
      {
        Console.WriteLine("{0}\n", e.Message);
        return string.Empty;
      }
    }

    public static string SetTemplateVar(string template, string variableName, string value)
    {
      return template.Replace("%" + variableName + "%", value);
    }

    public static Encoding GetEncoding(string filename)
    {
      // Read the BOM
      var bom = new byte[4];
      using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read))
      {
        file.Read(bom, 0, 4);
      }

      // Analyze the BOM
      if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
      if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
      if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
      if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
      if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
      return Encoding.ASCII;
    }
  }
}