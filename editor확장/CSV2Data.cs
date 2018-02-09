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
using UnityEditor.Callbacks;

public class CSV2Data : Editor{
    const string codeTmplLoc = "Assets/Editor/";
    const string dataAssetPath = "Assets/Resources/DataAssets";
    const string codeTargetPath = "Assets/Script/DataStruct/";
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

        //MakeDB(path);
        SyncMakeDB(path);
        //EditorUtility.DisplayDialog("CSV파일 만들기 완료", "변환완료", "OK");
    }
    [MenuItem("Tools/All CSV2Data")]
    public static void MakeAllDB()
    {
        string path = EditorUtility.OpenFolderPanel("CSV폴더를 선택하세요\n파일이름이 데이터 테이블이 됩니다", "Datas/Table", "");
        if (null == path || "" == path)
            return;
        string[] _csvFiles = Directory.GetFiles(path, "*.csv");
        if (0 < _csvFiles.Length)
        {
            foreach (string s in _csvFiles)
            {
                MakeDB(path);
            }
            EditorUtility.DisplayDialog("모든 CSV파일 만들기 완료", "변환완료", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("CSV파일 없네요", "장난하심?", "OK");
        }
    }

    private static string MakeLines(ref string path)
    {
        path = path.Replace("\\", "/");
        lines = File.ReadAllLines(path, Encoding.Default);
        string[] url = path.Split('/');
        string filename = url[url.Length - 1];

        //0. 필요한 디렉토리가 있는지 확인한다
        CheckAndCreateDirectory();


        lines[0] = lines[0].Replace("//", "");
        lines[1] = lines[1].Replace("//", "");
        fieldNames = lines[0].Split(',');
        fieldTypes = lines[1].Split(',');
        //코멘트까지 있을지 모르지만 있다면 해보자
        if (lines[2].StartsWith("//"))
        {
            lines[2] = lines[2].Replace("//", "");
            comments = lines[2].Split(',');
            _dataStartPos++;
        }
        return filename;
    }

    public static void MakeDB(string path)
    {
        string table_name = MakeLines(ref path).Split('.')[0];
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
        string tmplFullPath = codeTargetPath + tmplDataFile + tmplExt;
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
        string targetPath = codeTargetPath + tmplDataHolderFile + ".cs";
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
        //EditorApplication.update += OnEditorUpdate;
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

        //string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(dataAssetPath + "/" + typeof(T).ToString() + "Asset.asset");
        string assetPathAndName = dataAssetPath + "/" + typeof(T).ToString() + "Asset.asset";

        //파일을 만들기전에 파일이 존재하면 지우고 새로 만들어 (1)같은 파일을 만들지 않는다
        if (File.Exists(assetPathAndName))
        {
            File.Delete(assetPathAndName);
        }
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

    //utils

    //필요한 디렉토리를 미리 만든다
    static void CheckAndCreateDirectory()
    {
        if(!Directory.Exists(dataAssetPath))
        {
            Directory.CreateDirectory(dataAssetPath);
        }
        if(!Directory.Exists(codeTargetPath))
        {
            Directory.CreateDirectory(codeTargetPath);
        }
    }
    static void CreateLock(string pathName,string className,string keyName)
    {
        StreamWriter _sw = File.CreateText(".Lock.txt");
        _sw.WriteLine("pathName=" + pathName);
        _sw.WriteLine("className=" + className);
        _sw.WriteLine("keyName=" + keyName);
        _sw.Close();
    }
    static bool SecureLock(out string pathName,out string className,out string keyName)
    {
        pathName = null;
        className = null;
        keyName = null;

        if (!File.Exists(".Lock.txt"))
        {
            return false;
        }
        string[] _lines = File.ReadAllLines(".Lock.txt");
        foreach(string s in _lines)
        {
            string[] _oneLine = s.Split('=');
            switch(_oneLine[0])
            {
                case "pathName":
                    pathName = _oneLine[1];
                    break;
                case "className":
                    className = _oneLine[1];
                    break;
                case "keyName":
                    keyName = _oneLine[1];
                    break;
                default:
                    Debug.LogWarning("모르는 구별자 있음");
                    break;
            }
        }
        File.Delete(".Lock.txt");
        return true;
    }
    ////////////////////////////////////////////////////
    // 타입을 만드는 부분에 sleep()을 넣을수 없어서
    // 코루틴으로 만들어봄
    ////////////////////////////////////////////////////
    public static void SyncMakeDB(string path)
    {
        string table_name = MakeLines(ref path).Split('.')[0];
        GenerateDataStructTemplete(table_name);

        //Type T = GenerateDataHolderTemplete(table_name, fieldNames[0]);
        CoroutineGenerateDataHolderTemplete(table_name, fieldNames[0],fieldTypes[0],path);
    }

    [DidReloadScripts(0)]
    static void PostProcess()
    {
        string _path;
        string _className;
        string _tableName;
        string _keyName;

        if (SecureLock(out _path,out _className, out _keyName))
        {
            _tableName = MakeLines(ref _path).Split('.')[0];
            Type T = Type.GetType(_className + ",Assembly-CSharp");
            MethodInfo method = typeof(CSV2Data).GetMethod("CreateAsset", BindingFlags.Static | BindingFlags.Public);
            MethodInfo generic = method.MakeGenericMethod(T);
            generic.Invoke(null, new object[] { _tableName, _keyName });
            EditorUtility.DisplayDialog("CSV파일 만들기 완료", "변환완료", "OK");
        }
    }
    static void CoroutineGenerateDataHolderTemplete(string tableName, string primaryKey,string keyType,string pathName)
    {
        //1. 템플릿파일을 데이터형이 있는곳에 복사한다.
        string targetPath = codeTargetPath + tmplDataHolderFile + ".cs";
        string codeTemplete = File.ReadAllText(codeTmplLoc + tmplDataHolderFile);
        string targetClass = tmplDataHolderFile;

        targetPath = targetPath.Replace("[TableName]", tableName);
        targetClass = targetClass.Replace("[TableName]", tableName);
        codeTemplete = codeTemplete.Replace("[TableName]", tableName);
        codeTemplete = codeTemplete.Replace("[KeyField]", primaryKey);
        codeTemplete = codeTemplete.Replace("[KeyType]", keyType);

        File.WriteAllText(targetPath, codeTemplete);

        //UnityEditorInternal.InternalEditorUtility.RequestScriptReload();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        AssetDatabase.SaveAssets();
        CreateLock(pathName,targetClass, primaryKey);
    }
}
