using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System;
using System.Reflection;

public class DB2DATA : EditorWindow {
    static string dbPath = "Assets/Database/Resources/SCGameDB.txt";
    //템플릿파일에서 자료형을 만드는데 사용됨
    const string tmplLoc = "Assets/Database/";
    const string targetLoc = "Assets/Database/DataStruct/";
    const string tmplFile = "SC[TableName]List";
    const string dataAssetPath = "Assets/Database/Data";

    static public string[] tableList=null;
    static string[] nullList = new string[1] { "NULL" };
    static DB2DATA window = null;
    static DBWrapper DB;
    int selectedIdx = 0;
    int oldIdx = -1;
    string primaryKey = "";

    [MenuItem("Tools/DB2DATA")]
    static public void  Init()
    {
        //db에서 가능한 테이블의 리스트를 보여준다.
        DB = new DBWrapper("URI=file://" + dbPath,DBMODE.READ_MODE);
        tableList = DB.GetTableList();
        //foreach(string s in tableList)
        //{
        //    Debug.Log("table=" + s);
        //}
        window = (DB2DATA)EditorWindow.GetWindow<DB2DATA>();
        window.title = "테이블선택";
        
    }

    void OnGUI()
    {
        EditorGUILayout.TextArea("select table from DB");
		selectedIdx = EditorGUILayout.Popup("Table List:", selectedIdx, ((null != tableList)&&(0<tableList.Length)) ? tableList : nullList);
        if((null != tableList)&&(0<tableList.Length)&&(selectedIdx != oldIdx))
        {
            oldIdx = selectedIdx;
            primaryKey = DB.GetPrimaryKeyName(tableList[selectedIdx]);
        }
        EditorGUILayout.TextArea("Primary Key: " + primaryKey);
        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button("OK"))
        {
            if ((null != tableList) && (0 < tableList.Length))
            {
                Type T = GenerateDataHolderTemplete(tableList[selectedIdx], primaryKey);
                MethodInfo method = GetType().GetMethod("CreateAsset");
                MethodInfo generic = method.MakeGenericMethod(T);
                generic.Invoke(this, new object[] { tableList[selectedIdx], primaryKey });
            }
            window.Close();
        }
        if(GUILayout.Button("Cancle"))
        {
            window.Close();
        }
        EditorGUILayout.EndHorizontal();
    }

    Type GenerateDataHolderTemplete(string tableName,string primaryKey)
    {
        //1. 템플릿파일을 데이터형이 있는곳에 복사한다.
        string targetPath = targetLoc + tmplFile + ".cs";
        string codeTemplete = File.ReadAllText(tmplLoc + tmplFile);
        string targetClass = tmplFile;

        targetPath = targetPath.Replace("[TableName]", tableName);
        targetClass = targetClass.Replace("[TableName]", tableName);
        codeTemplete = codeTemplete.Replace("[TableName]", tableName);
        codeTemplete = codeTemplete.Replace("[KeyField]", primaryKey);

        File.WriteAllText(targetPath, codeTemplete);

        GC.Collect();
        GC.WaitForPendingFinalizers();

		return Type.GetType (targetClass+",Assembly-CSharp");
    }
    public void CreateAsset<T>(string tableName,string primaryKey) where T:ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();
        FieldInfo arrayData = asset.GetType().GetField("arrayData");
        Type dataType = arrayData.FieldType;
        int[] primaryKeyList=null;
        Array dataElements;

        dataType = dataType.GetElementType();
        primaryKeyList = DB.GetPrimaryKeyList(tableName, primaryKey);
        dataElements = Array.CreateInstance(dataType, primaryKeyList.Length);

        //DB의 record를 얻어온다.
        MethodInfo method = DB.GetType().GetMethod("GetRecord");
        MethodInfo generic = method.MakeGenericMethod(dataType);
        
        for (int i = 0; i < dataElements.Length; i++)
        {
            object result = (object)generic.Invoke(DB, new object[] { tableName, primaryKeyList[i], primaryKey });
            dataElements.SetValue(result, i);
        }
        arrayData.SetValue(asset, dataElements);

        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(dataAssetPath + "/New " + typeof(T).ToString() + ".asset");
        AssetDatabase.CreateAsset(asset, assetPathAndName);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }
}
