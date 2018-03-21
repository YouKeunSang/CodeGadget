using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Text;

[CustomEditor(typeof(MultiLanguageLabel))]
public class MultiLanguageLabelEditor : Editor {
    MultiLanguageLabel _target;
    bool _isDirty = false;
    bool _isSnapFoldShow = false;

    private void OnEnable()
    {
        _target = (MultiLanguageLabel)target;
    }
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Multi Language", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        _target.uiID = EditorGUILayout.TextField("UI ID",_target.uiID);
        _target.langType = (LANGUAGE_TYPE)EditorGUILayout.EnumPopup("language:", _target.langType);
        _target.uiType = (UI_TEXT_TYPE)EditorGUILayout.EnumPopup("type:", _target.uiType);

        //저장된 스냅샷을 표시함
        _isSnapFoldShow = EditorGUILayout.Foldout(_isSnapFoldShow, "snapshot");
        if(_isSnapFoldShow)
        {
            bool _isDelete = false;
            LANGUAGE_TYPE _shouldDelete = LANGUAGE_TYPE.KOREAN;

            if(null ==_target.defaultTransform)
            {
                _target.defaultTransform = new EssentialRectTransfom(_target);
            }
            foreach(KeyValuePair<LANGUAGE_TYPE, EssentialRectTransfom> t in _target.positionSnaps)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.SelectableLabel(t.Key.ToString());
                if(GUILayout.Button("-"))
                {
                    _isDelete = true;
                    _shouldDelete = t.Key;
                }
                EditorGUILayout.EndHorizontal();
            }
            if(_isDelete)
            {
                _target.positionSnaps.Remove(_shouldDelete);
                _isDelete = false;
            }
            //스냅샷과 디폴트 위치를 설정하는 버튼들
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Snapshot"))
            {
                _target.positionSnaps[_target.langType] = new EssentialRectTransfom(_target);
            }
            if (GUILayout.Button("Make this Default"))
            {
                _target.defaultTransform = new EssentialRectTransfom(_target);
            }
            if (GUILayout.Button("Restore to Default"))
            {
                _target.defaultTransform.RestoreRect(_target);
            }
            EditorGUILayout.EndHorizontal();
        }

        if (EditorGUI.EndChangeCheck())
        {
            _target.OnChangeLanguage();
        }

        base.OnInspectorGUI();
        
    }

}

public class LanguageChanger : Editor
{
    [MenuItem("Language/reflesh lagnuages", false, 20)]
    public static void LanguageChange()
    {
        string scriptFile = "Assets/Script/Editor/LanguageItemMenu.cs";
        string[] menuItems = Enum.GetNames(typeof(LANGUAGE_TYPE));

        // The class string
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// This class is Auto-Generated");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEditor;");
        sb.AppendLine("");
        sb.AppendLine("  public static class GeneratedMenuItems {");
        sb.AppendLine("");

        // loops though the array and generates the menu items
        for (int i = 0; i < menuItems.Length; i++)
        {
            sb.AppendLine("    [MenuItem(\"Language/" + menuItems[i] + "\")]");
            sb.AppendLine("    private static void MenuItem" + i.ToString() + "() {");
            sb.AppendLine("        LanguageChanger.RebuildLanguage( LANGUAGE_TYPE." + menuItems[i] + ");");
            sb.AppendLine("    }");
            sb.AppendLine("");
        }

        sb.AppendLine("");
        sb.AppendLine("}");

        // writes the class and imports it so it is visible in the Project window
        System.IO.File.Delete(scriptFile);
        System.IO.File.WriteAllText(scriptFile, sb.ToString(), System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset(scriptFile);

    }

    public static void RebuildLanguage(LANGUAGE_TYPE type)
    {
        MultiLanguageLabel[] _objects = FindObjectsOfType<MultiLanguageLabel>();
        foreach (MultiLanguageLabel o in _objects)
        {
            o.OnChangeLanguage(type);
            EditorUtility.SetDirty(o);
        }
        SceneView.RepaintAll();
    }
}

