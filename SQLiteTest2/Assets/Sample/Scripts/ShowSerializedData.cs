using UnityEngine;
using System.Collections;

public class ShowSerializedData : MonoBehaviour {

    public SCSampleList data = null;

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
        foreach (SCSampleInfo d in data.arrayData)
        {
            output += d.GetType().GetField(field).GetValue(d).ToString() + "\n";
        }
        return output;
    }
}
