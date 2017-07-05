/*
 * CVS파일을 읽어서 데이터 형태를 만들고 데이터를 모은 scriptable object를 만든다.
 */
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
using System;
using System.Reflection;

public class CSV2Data : Editor{
    const string codeTmplLoc = "Assets/Editor/";
    const string dataAssetPath = "Assets/Resources/DataAssets";
    const string targetLoc = "Assets/Script/DataStruct/";
    const string tmplDataFile = "[TableName]Info";
    const string tmplDataHolderFile = "[TableName]DataHolder";
    const string tmplExt = ".cs";
    static string[] fieldNames = null;
    static string[] fieldTypes = null;
    static string[] comments = null;
    static string[] lines = null;

    static int _dataStartPos = 2;

    [MenuItem("Tools/CSV2Data")]
    public static void MakeDB()
    {
        string path = EditorUtility.OpenFilePanel("CSV파일을 선택하세요\n파일이름이 데이터 테이블이 됩니다", "Datas/Table", "csv");
        if (null == path || "" == path)
            return;
        MakeDB(path);
        EditorUtility.DisplayDialog("CSV파일 만들기 완료", "변환완료", "OK");
    }
    public static void MakeDB(string path)
    {
        path = path.Replace("\\", "/");
        lines = File.ReadAllLines(path, Encoding.Default);
        string[] url = path.Split('/');
        string filename = url[url.Length - 1];

        lines[0] = lines[0].Replace("//", "");
        lines[1] = lines[1].Replace("//", "");
        fieldNames = lines[0].Split(',');
        fieldTypes = lines[1].Split(',');
        //코멘트까지 있을지 모르지만 있다면 해보자
        if(lines[2].StartsWith("//"))
        {
            lines[2] = lines[2].Replace("//", "");
            comments = lines[2].Split(',');
            _dataStartPos++;
        }
        string table_name = filename.Split('.')[0];
        GenerateDataStructTemplete(table_name);
        Type T = GenerateDataHolderTemplete(table_name, fieldNames[0]);
        MethodInfo method = typeof(CSV2Data).GetMethod("CreateAsset",BindingFlags.Static|BindingFlags.Public);
        MethodInfo generic = method.MakeGenericMethod(T);
        generic.Invoke(null, new object[] { table_name, fieldNames[0] });
    }

    /// <summary>
    /// 템플릿 파일에서 [@TableName] 과 [@Field]를 바꿔서 데이터 형을 만든다.
    /// </summary>
    /// <param name="tableName"></param>
    public static void GenerateDataStructTemplete(string tableName)
    {
        string tmplFullPath = targetLoc + tmplDataFile + tmplExt;
        string publicMembers = null;

        tmplFullPath = tmplFullPath.Replace("[TableName]", tableName);
        string codeTemplete = File.ReadAllText(codeTmplLoc + tmplDataFile);
        for (int i = 0; i < fieldNames.Length; i++)
        {
            publicMembers += "\tpublic " + fieldTypes[i].ToLower() + "\t" + fieldNames[i] + ";";
            if(null != comments && !string.IsNullOrEmpty(comments[i]))
            {
                publicMembers += "\t//" + comments[i];
            }
            publicMembers += "\n";
        }

        codeTemplete = codeTemplete.Replace("[@TableName]", tableName);
        codeTemplete = codeTemplete.Replace("[@Field]", publicMembers);
        File.WriteAllText(tmplFullPath, codeTemplete);
        //TODO: 생성된 데이터 파일을 어셈블리에서 인식하도록 해야 다음 작업 가능
     }

    static Type GenerateDataHolderTemplete(string tableName, string primaryKey)
    {
        //1. 템플릿파일을 데이터형이 있는곳에 복사한다.
        string targetPath = targetLoc + tmplDataHolderFile + ".cs";
        string codeTemplete = File.ReadAllText(codeTmplLoc + tmplDataHolderFile);
        string targetClass = tmplDataHolderFile;

        targetPath = targetPath.Replace("[TableName]", tableName);
        targetClass = targetClass.Replace("[TableName]", tableName);
        codeTemplete = codeTemplete.Replace("[TableName]", tableName);
        codeTemplete = codeTemplete.Replace("[KeyField]", primaryKey);

        File.WriteAllText(targetPath, codeTemplete);

        //UnityEditorInternal.InternalEditorUtility.RequestScriptReload();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        AssetDatabase.SaveAssets();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        return Type.GetType(targetClass + ",Assembly-CSharp");
    }
    public static void CreateAsset<T>(string tableName, string primaryKey) where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();
        FieldInfo arrayData = asset.GetType().GetField("arrayData");
        Type dataType = arrayData.FieldType;
        Array dataElements;

        dataType = dataType.GetElementType();
        dataElements = Array.CreateInstance(dataType, lines.Length - _dataStartPos);

        MethodInfo method = typeof(CSV2Data).GetMethod("GetRecord",BindingFlags.Static|BindingFlags.Public);
        MethodInfo generic = method.MakeGenericMethod(dataType);

        for (int i = _dataStartPos; i < lines.Length; i++)
        {
            object result = (object)generic.Invoke(null, new object[] { fieldNames,lines[i].Split(',')});
            dataElements.SetValue(result, i - _dataStartPos);
        }
        arrayData.SetValue(asset, dataElements);

        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(dataAssetPath + "/" + typeof(T).ToString() + "Asset.asset");
        AssetDatabase.CreateAsset(asset, assetPathAndName);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }

    public static T GetRecord<T>(string[] fieldName,string[] dataList) where T : new()
    {
        T _record = new T();
        //레코드 필드에 ","가 있을경우 "\"" 으로 감싼 데이터로 변환된다. 이를 합치는 경우
        string[] _dataList = new string[fieldName.Length];
        int _curIdx = 0;
        for(int i=0;i<dataList.Length;i++)
        {
            if(dataList[i].StartsWith("\""))
            {
                dataList[_curIdx] = dataList[i];

                do
                {
                    _dataList[_curIdx] += dataList[++i];
                }
                while (!dataList[i].EndsWith("\""));
                _dataList[_curIdx] = _dataList[_curIdx].Replace("\"", "").Trim();
            }
            else
            {
                _dataList[_curIdx] = dataList[i];
            }
            _curIdx++;
        }

        FieldInfo[] fields = _record.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldInfo f in fields)
        {
            for (int i = 0; i < fieldName.Length; i++)
            {
                if (f.Name.Equals(fieldName[i]))
                {
                    try
                    {
                        //f.SetValue(_record, Convert.ChangeType(dataList[i], f.FieldType));
                        if (!string.IsNullOrEmpty(_dataList[i]))
                        {
                            f.SetValue(_record, Convert.ChangeType(_dataList[i], f.FieldType));
                        }
                        //else
                        //{
                        //    f.SetValue(_record, Activator.CreateInstance(f.FieldType));
                        //}
                    }
                    catch
                    {
                        Debug.LogError("line:["+i+"]"+f.Name + "->" + f.FieldType);
                    }
                }
            }
        }

        return _record;
    }
}
