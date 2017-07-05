using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using System.Reflection;
using System;
using System.IO;

public enum DBMODE
{
    WRITE_MODE,
    READ_MODE,
}

public class DBWrapper : IDisposable
{
    SqliteConnection _db = null;

    public void Dispose()
    {
        Debug.Log("ABCCD");
    }

    void DBDispos(object sender, EventArgs e)
    {
        Debug.Log("ABCCDFG");
        Debug.Log("CloseDB => " + _db.State);
    }

    //void DBStateChange(object sender, System.Data.StateChangeEventArgs e)
    //{
    //    Debug.Log("CloseDB => " + e.CurrentState);
    //}

    public DBWrapper(string path,DBMODE openMode)
    {
        OpenDB(path, openMode);
    }

    public void OpenDB(string path,DBMODE openMode)
    {
        if(DBMODE.READ_MODE == openMode)
        {
            string filePath = path.Replace("URI=file://","");
            if (!File.Exists(filePath))
                return;
        }
        _db = new SqliteConnection(path);
        _db.Disposed += DBDispos;
        //_db.StateChange += DBStateChange;
        _db.Open();
    }
    public void CloseDB()
    {
        _db.Close();
        Debug.Log("CloseDB => " + _db.State);

        _db.Dispose();
        SqliteConnection.ClearAllPools();
        _db = null;
    }
    #region Initialize build DB
    public bool CreateTable(string tableName,string[] fieldNames, string[] fieldTypes)
    {
        string _query = "CREATE TABLE [TABLE]([FIELD]);";
        string _fields = null;

        for(int i=0;i<fieldNames.Length;i++)
        {
            if(0==i)
            {
                _fields = fieldNames[0] + " integer primary key";
            }
            else
            {
				string _dbType = ConvertCSharp2DB(fieldTypes[i]);
				_fields += "," + MakeValue(fieldNames[i],_dbType) + " " +_dbType ;
            }
        }

        //치환한다.
        _query = _query.Replace("[TABLE]", tableName);
        _query = _query.Replace("[FIELD]", _fields);
        try
        {
            SqliteCommand cmd = new SqliteCommand(_query,_db);
            if (-1 == cmd.ExecuteNonQuery())
            {
                Debug.LogError("db excution error");
            }

            cmd.Dispose();

        }
        catch(Exception e)
        {
            Debug.LogError("Error creating DB:"+e);
            _db.Close();
            return false;
        }
        return true;
    }
    public void InsertRecord(string tableName,string[] values,string[] fieldTypes)
    {
        if(null != _db)
        {
            string _query = "INSERT INTO [TABLE] VALUES ([VALUE]);";
            string _value = null;
            for(int i=0;i<values.Length;i++)
            {
				string dbType = ConvertCSharp2DB(fieldTypes[i]);
                if(0==i)
                {
					_value = MakeValue(values[i],dbType);
                }
                else
                {
					_value += ","+MakeValue(values[i], dbType);
                }
            }
            //치환한다.
            _query = _query.Replace("[TABLE]", tableName);
            _query = _query.Replace("[VALUE]", _value);
            try
            {
                SqliteCommand cmd = new SqliteCommand(_query, _db);
                if(-1 == cmd.ExecuteNonQuery())
                {
                    Debug.LogError("db excution error");
                }

                cmd.Dispose();
            }
            catch(Exception e)
            {
                Debug.LogError("Error InsertRecord DB:"+e);
                _db.Close();
            }
        }
    }
    #endregion

    #region retrieve DATA and META
    public T GetRecord<T>(string tableName,int key,string keyName = "id") where T:new()
    {
        T _record = new T();
        string _query = "SELECT * FROM " + tableName + " WHERE "+keyName+ "=" + key.ToString();
        SqliteCommand cmd = new SqliteCommand(_query, _db);
        SqliteDataReader rdr = cmd.ExecuteReader();

        while(rdr.Read())
        {
            FieldInfo[] fields = _record.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance);
            foreach(FieldInfo f in fields)
            {
                f.SetValue(_record, Convert.ChangeType(rdr[f.Name],f.FieldType));
            }
        }
        
        return _record;
    }

    public string[] GetTableList()
    {
        if(null != _db)
        {
            List<string> tableList = new List<string>();
            string _query = "SELECT tbl_name FROM sqlite_master WHERE type = 'table';";
            SqliteCommand cmd = new SqliteCommand(_query, _db);
            SqliteDataReader rdr = cmd.ExecuteReader();

            while(rdr.Read())
            {
                string table = (string)rdr["tbl_name"];
                //Debug.Log(table);
                tableList.Add(table);
            }
            return tableList.ToArray();
        }
        return null;
    }
    public int[] GetPrimaryKeyList(string tableName,string primaryKeyName)
    {
        if (null != _db)
        {
            string _query = "SELECT [KEYNAME] FROM [TABLE];";
            _query = _query.Replace("[TABLE]", tableName);
            _query = _query.Replace("[KEYNAME]", primaryKeyName);
            SqliteCommand cmd = new SqliteCommand(_query, _db);
            SqliteDataReader rdr = cmd.ExecuteReader();

            List<int> keyList = new List<int>();
            while(rdr.Read())
            {
                int key = (int)rdr.GetInt32(0);
                //Debug.Log(key);
                keyList.Add(key);
            }
            return keyList.ToArray();
        }
        return null;
    }
    public string GetPrimaryKeyName(string tableName)
    {
        if (null != _db)
        {
            string _query = "SELECT sql FROM sqlite_master WHERE type = 'table' AND tbl_name = '[TABLE]';";
            _query = _query.Replace("[TABLE]", tableName);
            SqliteCommand cmd = new SqliteCommand(_query, _db);
            SqliteDataReader rdr = cmd.ExecuteReader();
            string tableInfo = (string)rdr[0];
            //
            // 결과는 "CREATE TABLE Sample(dropgroup_index integer primary key,drop_group integer,item integer,skill integer,priority integer,rate integer,quantity_min integer,quantity_max integer,'note' text)"
            // 위와 같이 한줄로 나온다.
            //
            int infoStartIdx = tableInfo.IndexOf('(')+1;
            tableInfo = tableInfo.Substring(infoStartIdx, tableInfo.Length - infoStartIdx-1);
            string[] fields = tableInfo.Split(','); //각 필드로 나눈다.
            //제약사항에 따라 첫번째 컬럼이 PK이고 PK는 int값이다.
            string[] primaryKey = fields[0].Split(' ');
            if (primaryKey[2].ToLower().Equals("primary")&&primaryKey[1].ToLower().Equals("integer"))
            {
                return primaryKey[0];
            }
        }
        return null;
    }
    #endregion
    #region UtilFunction
    string MakeValue(string value, string type)
    {
        if ("text" == type.ToLower())
        {
            return "'" + value + "'";
        }
        return value;
    }

    static public string CovertDB2CSharp(string dbType)
    {
        switch(dbType.ToLower())
        {
            case "integer":
                return "int";
            case "real":
                return "float";
            case "text":
                return "string";
        }

		return dbType;
    }
    static public string ConvertCSharp2DB(string csharpType)
    {
        switch(csharpType.ToLower())
        {
            case "int":
                return "integer";
            case "float":
                return "real";
            case "string":
                return "text";
        }
		return csharpType;
    }
    #endregion
}
