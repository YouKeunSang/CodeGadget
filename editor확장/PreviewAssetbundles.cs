using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections;


/*
 * 
 * 에쎗번들 미리보기를 위한 에디터스크립입니다.
 *
 * 1. 미리보려는 번들파일은 Assets폴더하위에 두고
 * 2. 프로젝트탭에서 번들파일을 선택하고 
 * 3. Window/Show Assetbundles메뉴를 클릭하여 뜨는 윈도우에서
 * 4. 확인하려는 에쎗을 더블클릭하여 인스펙트창에서 확인하시면 됩니다.
 * 
 * 
 *
 */

class PreviewAssetbundles:EditorWindow 
{
    [MenuItem("Window/Show Assetbundles")]
    static void Execute()
    {		
		PreviewAssetbundles window = (PreviewAssetbundles)EditorWindow.GetWindow (typeof (PreviewAssetbundles));    
		window.autoRepaintOnSceneChange=true;
		window.wantsMouseMove=false;
		window.Show();
		assetpaths.Clear();
		ProcessDirectory(Application.dataPath);
    }
	
	static string setpath="";
	static bool isloading=false;
	static Dictionary<string, WWW> WWWs = new Dictionary<string, WWW>();
	static Dictionary<string, Object[]> assets = new Dictionary<string, Object[]>();
	static List<string> assetpaths = new List<string>();
	void Update() {
		
        foreach (Object o in Selection.GetFiltered(typeof (Object), SelectionMode.Assets))
        {			
            if (!(AssetDatabase.GetAssetPath(o).Contains(".assetbundle") || AssetDatabase.GetAssetPath(o).Contains(".unity3d"))) continue;
			
			string path = "file://" + Application.dataPath + "/"+AssetDatabase.GetAssetPath(o).Substring(7);
			if(setpath!=path){
				setpath=path;
				if(!WWWs.ContainsKey(setpath)){
					WWWs.Add(setpath,new WWW(setpath));
					isloading=true;
					System.GC.Collect();
					this.Focus();
				}
			}	
			break;
        }
		if(isloading){
			try{
				Object[] olist=WWWs[setpath].assetBundle.LoadAll();
				assets.Add(setpath,olist);
				isloading=false;
			}catch{
			}
		}
	}
	public Vector2 scrollPosition;
	void OnGUI () {
			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical();	
			if(GUILayout.Button("Refresh")){
				assetpaths.Clear();
				ProcessDirectory(Application.dataPath);
			}
			scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(360), GUILayout.Height(500));
	        foreach (string p in assetpaths){
				if(GUILayout.Button(p.Replace("file://"+Application.dataPath,""))){
					if(setpath!=p){
						setpath=p;
						if(!WWWs.ContainsKey(setpath)){
							WWWs.Add(setpath,new WWW(setpath));
							isloading=true;
							try{
								if(!assets.ContainsKey(setpath))
								{
									//WWWs[setpath].assetBundle.Unload(true);
									Object[] olist=WWWs[setpath].assetBundle.LoadAll();
									assets.Add(setpath,olist);
									isloading=false;
								}
							}catch{
							}
						}
					}	
				}
			}
			GUILayout.EndScrollView();
			GUILayout.EndVertical();
		if(assets.ContainsKey(setpath)){
			GUILayout.BeginScrollView(new Vector2(0,0));
			GUILayout.BeginVertical();	
			try{
				GUILayout.TextField(setpath);
				EditorGUILayout.ObjectField(WWWs[setpath].assetBundle.mainAsset,WWWs[setpath].assetBundle.mainAsset.GetType(),true);
			}catch{}
	        foreach (Object oc in assets[setpath]){
				try{
					int _widhth;
					int _height;
					GUILayout.BeginHorizontal();
					EditorGUILayout.TextField(oc.ToString());
					if(oc.GetType()==typeof(Texture2D)){
						Texture2D temp = oc as Texture2D;
						_widhth = temp.width;
						_height = temp.height;
						EditorGUILayout.ObjectField(oc,oc.GetType(),true,GUILayout.Width(_widhth/4),GUILayout.Height(_height/4));
					}
					else
					{
						EditorGUILayout.ObjectField(oc,oc.GetType(),true);
					}
					if(oc.GetType()==typeof(GameObject)){
						if(GUILayout.Button("Instantiate")){
							Selection.activeGameObject = GameObject.Instantiate(oc as GameObject) as GameObject;
						}
					}
					if(oc.GetType()==typeof(Texture2D)){
						if(GUILayout.Button("export")){
							Texture2D temp = oc as Texture2D;
							Texture2D newTex = new Texture2D(temp.width,temp.height,TextureFormat.ARGB32,false);
							newTex.SetPixels(0,0,temp.width,temp.height,temp.GetPixels());
							//newTex=FillInClear(newTex);
							newTex.Apply();
							string outname = Application.dataPath+"/output.png";
							Debug.Log("output=>"+outname);
							File.WriteAllBytes(outname,newTex.EncodeToPNG());
						}
					}
					GUILayout.EndHorizontal();
				}catch{}
			}
			GUILayout.EndVertical();
			GUILayout.EndScrollView();
		}
		GUILayout.EndHorizontal();
	}
	
	void OnDestroy()
	{
		foreach(string path in assetpaths)
		{
			if(assets.ContainsKey(path))
			{
				WWWs[path].assetBundle.Unload(true);
			}
		}
	}
	void OnSelectionChange()
	{
	}
	
	public static void find() 
    {		
		string[] args=null;
        foreach(string path in args) 
        {
            if(File.Exists(path)) 
                ProcessFile(path); 
            else if(Directory.Exists(path)) 
                ProcessDirectory(path);
        }        
    }


    public static void ProcessDirectory(string targetDirectory) 
    {
        string [] fileEntries = Directory.GetFiles(targetDirectory);
        foreach(string fileName in fileEntries)
            ProcessFile(fileName);

        string [] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
        foreach(string subdirectory in subdirectoryEntries)
            ProcessDirectory(subdirectory);
    }

    public static void ProcessFile(string path) 
    {	
        if (path.Contains(".assetbundle") || path.Contains(".unity3d")) 
			assetpaths.Add("file://" +path.Replace("\\","/"));
    }

}