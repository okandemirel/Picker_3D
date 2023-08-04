using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public abstract class BasicEditorUI : EditorWindow
    {
        public static Texture2D Logo
        {
            get
            {
                if (_logo == null) LoadLogo();
                return _logo;
            }
        }

        private static Texture2D _logo;

        private static void LoadLogo()
        {
            string logo = AssetDatabase.FindAssets("t:Texture2d asset-inventory-logo").FirstOrDefault();
            _logo = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(logo));
        }

        public virtual void OnGUI()
        {
            EditorGUILayout.Space();
        }
    }
}