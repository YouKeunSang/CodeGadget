using UnityEditor;
using UnityEngine;
using System.Collections;
using Mono.Data.Sqlite;
using System.IO;
using System.Text;
using System;

public class CSV2DB : Editor {
    static string dbPath = "Assets/Database/Resources/SCGameDB.txt";
    const string tmplLoc = "Assets/Database/";
    const string targetLoc = "Assets/Database/DataStruct/";
    const string tmplFile = "SC[TableName]Info";
    const string tmplExt = ".cs";
    static string[] fieldNames = null;
    static string[] fieldTypes = null;
    static DBWrapper DB = null;

    [MenuItem("Tools/CSV2DB")]
    public static void MakeDB()
    {
        string path = EditorUtility.OpenFilePanel("choose CSV file", "", "csv");
        if (null == path || "" == path)
            return;
        MakeDB(path);
        EditorUtility.DisplayDialog("DB transction finished", "convert to DB complete", "OK");
        
    }

    public static void MakeDB(string path)
    {
        path = path.Replace("\\", "/");
        string[] lines = File.ReadAllLines(path, Encoding.Default);
        string[] url = path.Split('/');
        string filename = url[url.Length - 1];

        lines[0] = lines[0].Replace("//", "");
        lines[1] = lines[1].Replace("//", "");
        fieldNames = lines[0].Split(',');
        fieldTypes = lines[1].Split(',');
        string table_name = filename.Split('.')[0];

        using (DB = new DBWrapper("URI=file://" + dbPath, DBMODE.WRITE_MODE))
        {
            if (CreateTable(DB, table_name))
            {
                for (int i = 2; i < lines.Length; i++)
                {
                    DB.InsertRecord(table_name, lines[i].Split(','), fieldTypes);
                }
            }
            else
            {
                Debug.LogError("DB create error");
            }
            
            DB.CloseDB();
            DB.Dispose();
        }
        
        
        //GenerateDataStructTemplete(table_name);
    }

    public static bool CreateTable(DBWrapper db,string tableName)
    {

        return db.CreateTable(tableName, fieldNames, fieldTypes);
    }
    /// <summary>
    /// 템플릿 파일에서 [@TableName] 과 [@Field]를 바꿔서 데이터 형을 만든다.
    /// </summary>
    /// <param name="tableName"></param>
    public static void GenerateDataStructTemplete(string tableName)
    {
        string tmplFullPath = targetLoc + tmplFile + tmplExt;
        string publicMembers = null;

        tmplFullPath = tmplFullPath.Replace("[TableName]", tableName);
        string codeTemplete = File.ReadAllText(tmplLoc+tmplFile);
        for(int i=0;i<fieldNames.Length;i++)
        {
            publicMembers += "\tpublic " + fieldTypes[i] + "\t" + fieldNames[i] + ";\n";
        }

        codeTemplete = codeTemplete.Replace("[@TableName]", tableName);
        codeTemplete = codeTemplete.Replace("[@Field]", publicMembers);
        File.WriteAllText(tmplFullPath, codeTemplete);
    }
}
