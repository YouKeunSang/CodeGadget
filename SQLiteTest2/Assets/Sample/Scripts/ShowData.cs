using UnityEngine;
using System.Collections;
using System.IO;


public class ShowData : MonoBehaviour {
    string DBLocation;
    DBWrapper _db = null;
    SCSampleInfo[] items = new SCSampleInfo[8];

	// Use this for initialization
	void Start () {
        if(Application.isEditor)
        {
            DBLocation = "Assets/Database/Resources/SCGameDB.txt";
            _db = new DBWrapper("URI=file://" + DBLocation,DBMODE.READ_MODE);
        }
        else //if(Application.platform == RuntimePlatform.Android)
        {
            TextAsset asset = Resources.Load("default") as TextAsset;
            if(null == asset)
            {
                Debug.LogWarning("DB file not found!");
            }
            DBLocation = Application.persistentDataPath+"/default.db";
            File.WriteAllBytes(DBLocation,asset.bytes);
            Resources.UnloadAsset(asset);
            _db = new DBWrapper("URI=file://" + DBLocation, DBMODE.READ_MODE);
        }
        
	    for(int i=10000;i<10008;i++)
        {
            items[i - 10000] = _db.GetRecord<SCSampleInfo>("Sample", i, "dropgroup_index");
        }
        _db.CloseDB();
	}
	
    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.TextArea(CombineColumn("dropgroup_index"));
        GUILayout.TextArea(CombineColumn("drop_group"));
        GUILayout.TextArea(CombineColumn("item"));
        GUILayout.TextArea(CombineColumn("skill"));
        GUILayout.TextArea(CombineColumn("priority"));
        GUILayout.TextArea(CombineColumn("rate"));
        //GUILayout.TextArea(CombineColumn("quantity_min"));
        //GUILayout.TextArea(CombineColumn("quantity_max"));
        GUILayout.TextArea(CombineColumn("note"));
        GUILayout.EndHorizontal();
    }

    string CombineColumn(string field)
    {
        string output = field + "\n------------\n";
        foreach (SCSampleInfo d in items)
        {
            output += d.GetType().GetField(field).GetValue(d).ToString() + "\n";
        }
        return output;
    }

}
