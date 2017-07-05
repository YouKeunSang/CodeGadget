#define SEPARATE_DEPENDENCY

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System;
using System.Net;
using System.Diagnostics;
using FTP;

public class PatchMaker:EditorWindow
{
	private Vector2 SP1 =new Vector2(100, 100);
	private Vector2 SP3 =new Vector2(0, 0);
	int iSelect=0;
	string g_strConsol = null;
	
	private enum enumProtocol{
		LocalServer,
		Ftp,
	};
	private enum enumPlatform{
		Android,
		iOS,
		Windows,
	};
	
	private static string strRootDir;			//=Appliatin.dataPath+"/../AssetBundle"
	private static string strOutputDir;			//=strRootDir+"/output/";
	private static string strTempDir;			//=strRootDir+"/diff";
	private static string strXMLLocation;		//=strRootDir+"/config.xml";
	private static string strVersionXMLLocation;//=strRootDir+"/version.xml";
	private static string strLocalServer;		//=strRootDir+"/LocalServer";
	private static string strPatchObjDir;		
	
	//XML save/load value 
	private static string strSVNAddr="http://127.0.0.1/trunk/cube/Assets/prefab";
	private string strSVNRevision = "11";
	private string strPatchVersion="1.00";
	private string strUploadServer = @"s:\wwwRoot";
	private enumProtocol eUploadProtocol=enumProtocol.LocalServer;
	private enumPlatform ePlatform=enumPlatform.Android;
	
	public string strWebID = "ftp_user";
	public string strWebPW = "p@ssw0rd";
	/// <summary>
	/// Application.data가 project/Assets에서 시작한다. 만약 이게 바뀌게 될 경우에 대해서 Asset의 Root디렉토리의 이름을 
	/// 사용자가 설정할 수 있도록 수정 20130405
	/// </summary>
	string strAssetIdentifier = "Assets";
	
	
	//global variable
	private List<FileInfo> lstAsset = new List<FileInfo>();
	int STEP=0; //OnGui에서 시간이 걸리는 작없을 두번하지 않도록 한다.
	List<string> lstOutputAssetBundles = new List<string>();
	
	
	//dependency info variables
	class AssetNode
	{
		public AssetNode pParent{get; private set;}
		public AssetNode(AssetNode parent)
		{
			this.pParent = parent;
		}
		public int iLevel;
		public string strMainAsset;
		public List<AssetNode> children;
	}
	List<AssetNode> lstLinkedAssets=new List<AssetNode>();
	
	[MenuItem("Assets/PatchMaker")]
	public static void init()
	{
		strRootDir = Application.dataPath+"/../AssetBundles";
		strOutputDir=strRootDir+"/output/";
		strTempDir=strRootDir+"/diff/Assets";
		strXMLLocation=Application.dataPath+"/AssetBundlePatcher/config.xml";
		strVersionXMLLocation=strRootDir+"/version.xml";
		strLocalServer=strRootDir+"/LocalServer";
		
		DirectoryInfo dir = new DirectoryInfo(strTempDir);
		if(!dir.Exists)
		{
			dir.Create();
		}
		PatchMaker window =  (PatchMaker)EditorWindow.GetWindow(typeof(PatchMaker));
		window.LoadSetting();
		window.Show();
	}
	
	public void LoadSetting()
	{		
		//step1: load config value from xml
		FileInfo file = new FileInfo(strXMLLocation);
		if(file.Exists)
		{
			XmlReader xmlReader = new XmlTextReader(strXMLLocation);
			while(xmlReader.Read())
			{
				if (xmlReader.IsStartElement())
	            {
	                switch (xmlReader.Name)
	                {
	                case "SVN_address":
						strSVNAddr = xmlReader.ReadString();
						break;
					case "SVN_revision":
						strSVNRevision = xmlReader.ReadString();
						break;
					case "patch_version":
						strPatchVersion = xmlReader.ReadString();
						break;
					case "upload_server":
						strUploadServer = xmlReader.ReadString();
						break;
					case "upload_protocol":
						eUploadProtocol =  (enumProtocol)Enum.Parse(typeof(enumProtocol),xmlReader.ReadString());
						break;
					case "Platform":
						ePlatform =  (enumPlatform)Enum.Parse(typeof(enumPlatform),xmlReader.ReadString());
						break;
					}
				}
			
			}
			xmlReader.Close();
		}
		else
		{
			SaveSetting();
		}
		
	}
	public void SaveSetting()
	{
		TextWriter stream = new StreamWriter(strXMLLocation);
		XmlTextWriter xmlWriter = new XmlTextWriter(stream);
			
		xmlWriter.Formatting = Formatting.Indented;
		xmlWriter.Indentation = 4;

		xmlWriter.WriteStartDocument();
		xmlWriter.WriteStartElement("config"); //need one root or multiful error
		SaveNode(xmlWriter,"SVN_address",strSVNAddr);
		SaveNode(xmlWriter,"SVN_revision",strSVNRevision);
		SaveNode(xmlWriter,"patch_version",strPatchVersion);
		SaveNode(xmlWriter,"upload_server",strUploadServer);
		SaveNode(xmlWriter,"upload_protocol",Enum.GetName(typeof(enumProtocol),eUploadProtocol));
		SaveNode(xmlWriter,"Platform",Enum.GetName(typeof(enumPlatform),ePlatform));
		xmlWriter.WriteEndElement();
		xmlWriter.WriteEndDocument();
		xmlWriter.Close();
		stream.Close();
	}
	/**********************************************
	 * 
	 * Patch asset dependent analysis functions
	 * 
	 * ********************************************/
	/// <summary>
	/// assetName이 하부 node에 있는지 검사한다.
	/// </summary>
	/// <returns>
	/// if assetName exist in sub-node, it means assetName is depend on that sub-node, else it is unique
	/// </returns>
	private bool ExistInChild(string assetName,List<AssetNode> children)
	{
		bool ret = false;
		
		//if it is leaf node return false
		if(null==children)
			return false;
		
		foreach(AssetNode n in children)
		{	
			foreach(AssetNode n2 in n.children)
			{
				if(assetName.Equals(n2.strMainAsset))
					return true;
			}
			
			ret = ExistInChild(assetName,n.children);
			if(true==ret)
			{
				return true;
			}
		}
		return false;
	}
	
	private AssetNode MakeDependencyNode(string mainAsset,AssetNode parent,int level)
	{
		AssetNode node = new AssetNode(parent);
		string[] assetName = new string[1];
		
		//UnityEngine.Debug.Log("MakeDependency:"+mainAsset);
		node.strMainAsset = mainAsset;
		node.strMainAsset=node.strMainAsset.Replace("\\","/");
		node.iLevel = level;
		assetName[0]=node.strMainAsset;
		node.children = new List<AssetNode>();
		foreach(string s in AssetDatabase.GetDependencies(assetName))
		{
			if((!s.Contains(node.strMainAsset))&&(!s.EndsWith(".cs"))&&(!s.EndsWith(".js"))&&(!s.EndsWith(".boo")))
			{
				node.children.Add(MakeDependencyNode(s,node,level+1));
			}
		}
		//remove child if it showd
		if(0<node.children.Count)
		{
			List<AssetNode> lstRemove = new  List<AssetNode>();
			
			foreach(AssetNode n in node.children)
			{
				if(ExistInChild(n.strMainAsset,node.children))
				{
					lstRemove.Add(n);
				}
			}
			
			if(0<lstRemove.Count)
			{
				foreach(AssetNode r in lstRemove)
				{
					node.children.Remove(r);
				}
			}
		}
		return node;
	}
	
	void LinkedAssetPrint(AssetNode node)
	{
		string strOut=null;
		string strDepth=null;
		
		for(int i=0;i<node.iLevel;i++)
			strDepth=strDepth+"\t";
		strOut=strDepth+node.iLevel.ToString()+":name="+node.strMainAsset+"[child="+node.children.Count.ToString()+"]";
		AssetNode parent = node.pParent;
		g_strConsol=g_strConsol+strOut+"\n";
		if(null!=parent)
		{
			g_strConsol=g_strConsol+strDepth+"has parent:"+parent.strMainAsset+"\n";
		}
		
		if(0<node.children.Count)
		{
			foreach(AssetNode n in node.children)
			{
				LinkedAssetPrint(n);
			}
		}
	}
	/// <summary>
	/// Makes Asset bundle with dependency order
	/// each level must have at lease one time or more
	/// </summary>
	/// <param name='root'>
	/// Root must be level==0
	/// </param>
	void BuildDependtAsset(AssetNode root)
	{
		int depth = DeepestNode(root);
		int pushCount =0;
		
		//output directory check
		DirectoryInfo dir = new DirectoryInfo(strOutputDir);
		if(!dir.Exists)
			dir.Create();
		
		for(int i =depth;i>=0;i--)
		{
			List<AssetNode> lstLevel = GetNodesInLevel(root,i);
			BuildPipeline.PushAssetDependencies();
			pushCount++;
			
			foreach(AssetNode n in lstLevel)
			{
				//BuildPipeline.PushAssetDependencies();
				UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(n.strMainAsset);
				g_strConsol = g_strConsol+n.strMainAsset+"\n";
				string filename = n.strMainAsset.Remove(0,n.strMainAsset.LastIndexOf("/")+1);
				int _index= filename.LastIndexOf(".");
				BuildTarget platform=BuildTarget.Android;;
				
				switch(ePlatform)
				{
				case enumPlatform.Android:
					platform = BuildTarget.Android;
					break;
				case enumPlatform.iOS:
					platform = BuildTarget.iPhone;
					break;
				case enumPlatform.Windows:
					platform = BuildTarget.StandaloneWindows;
					break;
				}
				
				filename=filename.Remove(_index,filename.Length-_index);
				string strOut = strOutputDir+filename+ ".unity3d";
				BuildPipeline.BuildAssetBundle(obj,null,strOut,BuildAssetBundleOptions.CollectDependencies|BuildAssetBundleOptions.CompleteAssets|BuildAssetBundleOptions.DeterministicAssetBundle,platform);
				//BuildPipeline.PopAssetDependencies();
			}
		}
		
		for(int i=0;i<pushCount;i++)
		{
			BuildPipeline.PopAssetDependencies();
		}
	}
	
	int DeepestNode(AssetNode node)
	{
		if(0==node.children.Count)
			return node.iLevel;
		else
		{
			List<int> results = new List<int> ();
			foreach(AssetNode n in node.children)
			{
				results.Add(DeepestNode(n));
			}
			results.Sort();
			return results[results.Count-1];
		}
	}

	List<AssetNode> GetNodesInLevel(AssetNode root,int level)
	{
		List<AssetNode> results = new List<AssetNode>();
		if(root.iLevel == level)
		{
			results.Add(root);
		}
		if(root.iLevel<level)
		{
			foreach(AssetNode n in root.children)
			{
				List<AssetNode> temp = GetNodesInLevel(n,level);
				if(0<temp.Count)
				{
					foreach(AssetNode n2 in temp)
					{
						results.Add(n2);
					}
				}
			}
		}
		return results;
	}
	/// <summary>
	/// Gets the Path,file name and ext.
	/// </summary>
	/// <returns>
	/// return[0]= path, return[1]=filename without ext, return[2]=extention name
	/// </returns>

	string[] GetFileNameAndExt(string file)
	{
		string[] result=new string[3];
		int _index;
		
		_index=file.LastIndexOf(".");
		if(0<_index)
			result[2]=file.Substring(_index,file.Length-_index);
		
		_index=file.LastIndexOf("/");
		if(0<_index)
			result[1]=file.Substring(_index+1,file.Length-_index-result[2].Length-1);
		
		if(0<_index)
			result[0]=file.Substring(0,_index);
		
		return result;
	}
	
	/// <summary>
	/// Deletes the unchanged asset bundle.
	/// </summary>
	void DeleteUnchangedAssetBundle()
	{
		foreach(AssetNode n in lstLinkedAssets)
		{
			int depth = DeepestNode(n);
			string remoteDir = strTempDir.Remove(strTempDir.LastIndexOf(strAssetIdentifier),strAssetIdentifier.Length);
			
			for(int i =depth;i>=0;i--)
			{
				List<AssetNode> lstLevel = GetNodesInLevel(n,i);

				foreach(AssetNode n2 in lstLevel)
				{
					FileInfo localFile = new FileInfo(n2.strMainAsset);
					FileInfo remoteFile = new FileInfo(remoteDir+n2.strMainAsset);
					if(FilesContentsAreEqual(localFile,remoteFile))
					{
						//delete asset bundle
						string[] fileName = GetFileNameAndExt(n2.strMainAsset);
						string targetFile = strOutputDir+fileName[1]+".unity3d";
						FileInfo file = new FileInfo(targetFile);
						
						try
						{
							file.Delete();
						}
						catch(FileNotFoundException)
						{
							//file can be over-deleted if it is shared asset
							//so this is not harmless
						}
					}
				}
			}
		}
	}
	/// <summary>
	/// patch될 오브젝트를 모아서 dependency순으로 linked list를 만든다
	/// </summary>
	/// <returns>
    /// 지정된 폴더가 존재하면 동작, 없으면 false
    /// </returns>
	public bool MakeAssetDependencyList()
	{
		bool ret = false;
		int _index = strSVNAddr.IndexOf(strAssetIdentifier);
		if(0<_index)
		{
			string localDir = strSVNAddr.Remove(0,_index);
			
			if(Directory.Exists(localDir))
			{
				if(0==lstLinkedAssets.Count)
				{
					System.IO.FileInfo[] targets=GetFiles(localDir);
					foreach(FileInfo level0 in targets)
					{
						_index = level0.FullName.LastIndexOf(strAssetIdentifier);
						string assetName =  level0.FullName.Remove(0,_index);
						lstLinkedAssets.Add(MakeDependencyNode(assetName,null,0));
					}
				}
				ret = true;
				//debug print
				g_strConsol=g_strConsol+"\n****** show asset dependencies *****\n";
				foreach(AssetNode n in lstLinkedAssets)
				{
					g_strConsol=g_strConsol+"strRootDir="+n.strMainAsset+" child="+n.children.Count+"\n";
					g_strConsol=g_strConsol+"Deepest depth in assetnode="+DeepestNode(n).ToString()+"\n";
					
					LinkedAssetPrint(n);
				}
			}
			//make all assets include dependency
			g_strConsol=g_strConsol+"\n****** BuildDependtAsset *****\n";
			foreach(AssetNode n in lstLinkedAssets)
			{
				BuildDependtAsset(n);
			}
		}
		return ret;
	}
	
	private void SaveNode(XmlWriter xmlWriter,string element, string Value)
	{
		xmlWriter.WriteStartElement(element);
		xmlWriter.WriteValue(Value);
		xmlWriter.WriteEndElement();
	}
	

	private FileInfo[] GetFiles(string path)
	{
		DirectoryInfo dir = new DirectoryInfo(path);
		FileInfo[] files= dir.GetFiles("*.*",SearchOption.AllDirectories);
		List<FileInfo> ret = new List<FileInfo>();
		
		foreach(FileInfo file in files)
		{
			if((file.Extension != ".meta")&&(file.Extension != ".exr"))
			{
				ret.Add(file);
			}
		}
		return ret.ToArray();	
	}
	private void MakeAssetBundle(List<FileInfo> assetFile)
	{
		foreach(FileInfo file in assetFile)
		{
			string outFileName =file.FullName.Remove(0,file.FullName.IndexOf(strAssetIdentifier));
			UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(outFileName);
			g_strConsol = g_strConsol+outFileName+"\n";
			string strOut = strOutputDir+file.Name.Replace(file.Extension,"")+ ".unity3d";
			BuildPipeline.BuildAssetBundle(obj,null,strOut,BuildAssetBundleOptions.CollectDependencies|BuildAssetBundleOptions.CompleteAssets|BuildAssetBundleOptions.DeterministicAssetBundle,BuildTarget.Android);
		}
	}
	private bool MakeAssetList()
	{
		bool ret =false;
		int _index = strSVNAddr.LastIndexOf(strAssetIdentifier);
		if(0<_index)
		{
			string localDir = strSVNAddr.Remove(0,_index);
			string remoteDir = strTempDir.Substring(0,strTempDir.LastIndexOf(strAssetIdentifier))+"/"+localDir;
			
			if((Directory.Exists(remoteDir))&&(Directory.Exists(localDir)))
			{
				if(0==lstAsset.Count)
				{
					System.IO.FileInfo[] targets=GetFiles(localDir);
					System.IO.FileInfo[] downloads=GetFiles(remoteDir);

					foreach(FileInfo currentFile in targets)
					{
						FileInfo compareFile = null;
						foreach(FileInfo f in downloads)
						{
							if(f.Name == currentFile.Name)
							{
								compareFile = f;
								break;
							}
						}
						if(!FilesContentsAreEqual(currentFile,compareFile))
						{
							if(-1==lstAsset.IndexOf(currentFile))
							{
								//todo: CRC32 check before add
								lstAsset.Add(currentFile);
							}
						}
					}
				}
				ret = true;
			}
		}
		return ret;
	}
	/// <summary>
	/// Resets the build settings except "Default.unity" scene.
	/// </summary>
	private void ResetBuildSettings()
	{
		List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene> (EditorBuildSettings.scenes);
		foreach(EditorBuildSettingsScene scene in scenes)
		{
			g_strConsol+= scene.path+"\t:"+scene.enabled.ToString()+"\n";
			if(!scene.path.Contains("Default"))
			{
				scene.enabled = false;
			}
		}
		EditorBuildSettings.scenes = scenes.ToArray ();
	}
	//*********************
	//generic util function
	//*********************
	bool DuplicateFileFinder(out List<string[]> duplicateNames)
	{
		bool ret = false;
		FileInfo[] rootFiles= GetFiles(strPatchObjDir);
		List<string> files=new List<string>();
		duplicateNames = new List<string[]>();
		
		foreach(FileInfo f in rootFiles)
		{
			string[] _filename=new string[1];
			_filename[0]=f.FullName.Remove(0,f.FullName.LastIndexOf(strAssetIdentifier));
			files.AddRange(AssetDatabase.GetDependencies(_filename));
		}
		foreach(string origin in files)
		{
			foreach(string compare in files)
			{
				string[] _origin = GetFileNameAndExt(origin);
				string[] _compare = GetFileNameAndExt(compare);
				
				if(_origin[1]==_compare[1])
				{
					//same name, possible candidate
					if((_origin[0]!=_compare[0])||(_origin[2]!=_compare[2]))
					{
						//relative path nor extention is not same,duplicate name
						string[] _duplicateName=new string[2];
						
						_duplicateName[0]=origin;
						_duplicateName[1]=compare;
						duplicateNames.Add(_duplicateName);
						ret=true;
					}
				}
			}
		}
		return ret;
	}
	public static void DeleteDirectory(string target_dir)
    {
        string[] files = Directory.GetFiles(target_dir);
        string[] dirs = Directory.GetDirectories(target_dir);

        foreach (string file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (string dir in dirs)
        {
            DeleteDirectory(dir);
        }

        Directory.Delete(target_dir, false);
    }
	public static bool FilesContentsAreEqual(FileInfo _currentFile, FileInfo _previousFile)
    {
        bool result;
		//incase prefab has newly added, consider this changed file
		if(null==_previousFile)
		{
			return false;
		}
		try
		{
	        if (_currentFile.Length != _previousFile.Length)
	        {
	            result = false;
	        }
	        else
	        {
	            using (var file1 = _currentFile.OpenRead())
	            {
	                using (var file2 = _previousFile.OpenRead())
	                {
	                    result = StreamsContentsAreEqual(file1, file2);
	                }
	            }
	        }
		}catch(FileNotFoundException e)
		{
			EditorUtility.DisplayDialog("FileNotFound error!!","\""+_currentFile.FullName+"\" may be renamed or newly added\n please check this file","close");
			UnityEngine.Debug.LogException(e);
			return false;
		}

        return result;
    }

    private static bool StreamsContentsAreEqual(Stream stream1, Stream stream2)
    {
        const int bufferSize = 2048 * 2;
        var buffer1 = new byte[bufferSize];
        var buffer2 = new byte[bufferSize];

        while (true)
        {
            int count1 = stream1.Read(buffer1, 0, bufferSize);
            int count2 = stream2.Read(buffer2, 0, bufferSize);

            if (count1 != count2)
            {
                return false;
            }

            if (count1 == 0)
            {
                return true;
            }

            int iterations = (int)Math.Ceiling((double)count1 / sizeof(Int64));
            for (int i = 0; i < iterations; i++)
            {
                if (BitConverter.ToInt64(buffer1, i * sizeof(Int64)) != BitConverter.ToInt64(buffer2, i * sizeof(Int64)))
                {
                    return false;
                }
            }
        }
    }
	void UpdateXML(string xmlPath,string version,List<AssetNode> assets)
	{
		XmlDocument doc = new XmlDocument();
		FileInfo file = new FileInfo(xmlPath);
		
		if (file.Exists)
		{
			doc.Load(xmlPath);
		}
		
		XmlNode root = doc.SelectSingleNode("/main");
	    if (null == root)
	    {
	        root=doc.CreateElement("main");
	        doc.AppendChild(root);
	    }
		
	    XmlNode versionNode = doc.SelectSingleNode("/Main/current_version");
	    if (null == versionNode)
	    {
	        versionNode = doc.CreateElement("current_version");
	        root.AppendChild(versionNode);
	    }
		versionNode.InnerText=version;
		
		XmlNode patchs = doc.CreateNode(XmlNodeType.Element,"version",null);
		XmlNode patchAttribe = doc.CreateAttribute("ver");
	    patchAttribe.Value = version;
	    patchs.Attributes.SetNamedItem(patchAttribe);
				
		//step1: add patchObjDir-lele0 obj=>must be cached first
		List<string> lstLevel0Assets = new List<string>();
		
		foreach(AssetNode n in assets)
		{
			if(0==n.iLevel)
			{
				string[] assetName = GetFileNameAndExt(n.strMainAsset);
				lstLevel0Assets.Add(assetName[1]+".unity3d");
			}
		}
		foreach(FileInfo f in GetFiles(strOutputDir))
		{
			if(!lstLevel0Assets.Contains(f.Name))
			{
				XmlNode _temp = doc.CreateElement("primitive");
				_temp.InnerText = f.Name;
				patchs.AppendChild(_temp);
			}
		}

		//step2: add lebel0 with dependencies order
		foreach(AssetNode n in assets)
		{
			patchs.AppendChild(MakeDependencyNode(doc,n));
		}
		root.AppendChild(patchs);
	
	    doc.Save(xmlPath);

	}
	
	XmlNode MakeDependencyNode(XmlDocument doc,AssetNode node)
	{
		XmlNode _temp;
		string[] assetName = GetFileNameAndExt(node.strMainAsset);
		
		if(0==node.iLevel)
		{
		 	_temp= doc.CreateElement("patch");
		}
		else
		{
			_temp=doc.CreateElement("depend");
		}
		
		_temp.InnerText = assetName[1]+".unity3d";
		if(0<node.children.Count)
		{
			foreach(AssetNode n in node.children)
			{
				_temp.AppendChild((MakeDependencyNode(doc,n)));
			}
		}
		
		return _temp;
	}
	
	private void MakeXml(string xmlPath,string version,FileInfo[] files)
	{
		/*
		XmlDocument doc =new XmlDocument();
		doc.Load(xmlPath);
		doc.ReadNode(
		*/
		TextWriter stream = new StreamWriter(xmlPath);
		XmlTextWriter xmlWriter = new XmlTextWriter(stream);
		xmlWriter.Formatting = Formatting.Indented;
		xmlWriter.Indentation = 4;
		
		xmlWriter.WriteStartDocument();
		xmlWriter.WriteStartElement("main"); //need one root or multiful error
		xmlWriter.WriteStartElement("history");
		xmlWriter.WriteStartElement("version");
		xmlWriter.WriteAttributeString("ver",version);
		foreach(FileInfo file in files)
		{
			SaveNode(xmlWriter,"patch",file.Name);
		}
		xmlWriter.WriteEndElement();//version
		xmlWriter.WriteEndElement();//history
		xmlWriter.WriteEndElement();//main
		xmlWriter.WriteEndDocument();
		xmlWriter.Close();
		stream.Close();
	}
	/// <summary>
	/// SVN에서 특정 리비젼의 패치될 에셋을 다운받으며 그에 의존되는 모든 에셋을 받아서 나중에 비교후 필요없는 파일은 지운다..
	/// </summary>
	void SVNCheckOut()
	{
		string _strDownloadDir = strTempDir+strSVNAddr.Remove(0,strSVNAddr.IndexOf(strAssetIdentifier)+strAssetIdentifier.Length);
		//download default level assets
		Process process = Process.Start("CMD","/C svn export -r "+strSVNRevision+" "+strSVNAddr+" "+_strDownloadDir);
		process.EnableRaisingEvents = true;
		
		if(process.WaitForExit(20000))
		{
			UnityEngine.Debug.Log("checkout normaly exited");
		}
		else
		{
			UnityEngine.Debug.Log("checkout time expired");
		}
		//파일의 이름은 /Assets폴더내에서 찾아서 SVN에 있는 파일을 상대좌표로 찾아서 checkout
		DirectoryInfo dir = new DirectoryInfo(strTempDir);
		FileInfo[] files= dir.GetFiles("*.*",SearchOption.AllDirectories);
		foreach(FileInfo file in files)
		{
			string[] _fileList=new string[1];
			List<string> _dependencyList;
			_fileList[0]= file.FullName.Remove(0,file.FullName.IndexOf("diff")+5); //exclude "/" at head,so it works as relative path
			_fileList[0]=_fileList[0].Replace("\\","/");
			_dependencyList=new List<string>(AssetDatabase.GetDependencies(_fileList));
			//step1. remove level0 asset itself
			for(int _i=0;_i<_dependencyList.Count;_i++)
			{
				if((_dependencyList[_i].EndsWith(file.Name))||(_dependencyList[_i].EndsWith(".cs"))||(_dependencyList[_i].EndsWith(".js"))||(_dependencyList[_i].EndsWith(".boo")))
				{
					_dependencyList.RemoveAt(_i);
				}
			}
			foreach(string s in _dependencyList)
			{
				//patch될 오프젝트들의 의존성 파일들을 일괄 다운받는다.
				string strSVNRoot = strSVNAddr.Substring(0,strSVNAddr.IndexOf(strAssetIdentifier));
				//UnityEngine.Debug.Log("checkout asset depends:"+s);
				DirectoryInfo tempDir = new DirectoryInfo(strTempDir.Substring(0,strTempDir.LastIndexOf(strAssetIdentifier))+s.Substring(0,s.LastIndexOf("/")));
				if(!tempDir.Exists)
				{
					tempDir.Create();
				}
				Process.Start("CMD","/C svn export -r "+strSVNRevision+" "+strSVNRoot+s+" "+strTempDir.Substring(0,strTempDir.LastIndexOf(strAssetIdentifier))+s+" --depth empty");
			}
		}
	}
	
	void OnGUI()
	{
		GUILayout.Label("Step1: SVN repository",EditorStyles.boldLabel);
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.BeginVertical();
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("SVN: ");strSVNAddr=EditorGUILayout.TextField(strSVNAddr);
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Rev: ");strSVNRevision=EditorGUILayout.TextField(strSVNRevision);
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Asset Identifier: ");strAssetIdentifier=EditorGUILayout.TextField(strAssetIdentifier);
		EditorGUILayout.EndHorizontal();
		{
			//it depends on "strAssetIdentifier" value
			strPatchObjDir = strSVNAddr.Remove(0,strSVNAddr.IndexOf(strAssetIdentifier));
		}
		ePlatform=(enumPlatform)EditorGUILayout.EnumPopup("Platform: ",ePlatform);
		EditorGUILayout.EndVertical();
		if(GUILayout.Button("CheckOut",GUILayout.Height(48)))
		{
			//todo: SVN ckeckout to certain directory
			if(Directory.Exists(strTempDir))
			{
				//direcory must be deleted if it is exist
	            DeleteDirectory(strTempDir);
			}
			SVNCheckOut();
			
			//check duplicate name and show error
			List<string[]>lstDuplicateName;
			if(DuplicateFileFinder(out lstDuplicateName))
			{
				string _outstring="these files are duplicate\n";
				foreach(string[] dup in lstDuplicateName)
				{
					_outstring = "file "+dup[0]+" and "+dup[1]+"\n";
				}
				EditorUtility.DisplayDialog("Duplicate file name ERROR",_outstring,"Close");
			}
			STEP=1;
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.Separator();
		
		GUILayout.Label("Step2: Diff",EditorStyles.boldLabel);
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.BeginVertical();
		SP1 = EditorGUILayout.BeginScrollView(SP1,false,true,GUILayout.Height(200));
		//show AssetBundle candidates

		if(1==STEP)
		{
			STEP=2;
			MakeAssetDependencyList();
			if("1.00"!=strPatchVersion)
			{
				//초기배포판에서는 모든 에셋번들을 지우지 않고 유지한다.
				DeleteUnchangedAssetBundle();
			}
			FileInfo[] files=GetFiles(strOutputDir);
			
			foreach(FileInfo asset in files)
			{
				lstOutputAssetBundles.Add(asset.Name);
			}
			
			
		}
		iSelect=GUILayout.SelectionGrid(iSelect,lstOutputAssetBundles.ToArray(),1);

		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();
		EditorGUILayout.EndHorizontal();
		
		GUILayout.Label("OUTPUT:");
		SP3 = EditorGUILayout.BeginScrollView(SP3,false,true,GUILayout.Height(200));
		//use GUILayout.TextArea for multiful lines
		g_strConsol=EditorGUILayout.TextArea(g_strConsol);
		EditorGUILayout.EndScrollView();
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("version: ");strPatchVersion=EditorGUILayout.TextField(strPatchVersion);
		EditorGUILayout.EndHorizontal();
		
		EditorGUILayout.Separator();
		
		GUILayout.Label("Step3: Upload",EditorStyles.boldLabel);
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.BeginVertical();
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Server: ");strUploadServer=EditorGUILayout.TextField(strUploadServer);
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.BeginHorizontal();
		eUploadProtocol=(enumProtocol)EditorGUILayout.EnumPopup("Protocol: ",eUploadProtocol);
		EditorGUILayout.EndHorizontal();
		if(enumProtocol.Ftp==eUploadProtocol)
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("ID: ");strWebID=EditorGUILayout.TextField(strWebID);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("PW: ");strWebPW=EditorGUILayout.PasswordField (strWebPW);
			EditorGUILayout.EndHorizontal();
		}
		EditorGUILayout.EndVertical();
		if(GUILayout.Button("Upload",GUILayout.Height(32)))
		{
			//STEP=3;
			UnityEngine.Debug.Log("Server="+strUploadServer);
			UnityEngine.Debug.Log("Protocol="+eUploadProtocol);
			if(enumProtocol.Ftp==eUploadProtocol)
			{
				ftpUtil ftp = new ftpUtil(strUploadServer,strWebID,strWebPW,"21");
				if(!ftp.Ping(strUploadServer))
				{
					EditorUtility.DisplayDialog("Invalid Server",strUploadServer+" is not valid","close");
					return;
				}
				ftp.MakeDir(strPatchVersion);
				FileInfo[] files = GetFiles(strOutputDir);
				
				foreach(FileInfo file in files)
				{
					bool result = ftp.Upload(strPatchVersion,file.FullName);
					if(!result)
					{
						UnityEngine.Debug.LogError("fail to upload");
					}
				}
				
				//xml file download,update and upload
				string[] _serverFiles= ftp.GetFileList("");
				foreach(string _file in _serverFiles)
				{
					if(_file.Contains("version.xml"))
					{
						ftp.Download(strVersionXMLLocation,"version.xml");
					}
				}
				UpdateXML(strVersionXMLLocation,strPatchVersion,lstLinkedAssets);
				ftp.Upload("",strVersionXMLLocation);
			}
			else if(enumProtocol.LocalServer==eUploadProtocol)
			{
				DirectoryInfo _localServer = new DirectoryInfo(strLocalServer);
				FileInfo[] assetFiles= GetFiles(strOutputDir);
				if(!_localServer.Exists)
					_localServer.Create();
				_localServer.CreateSubdirectory(strPatchVersion);
				foreach(FileInfo f in assetFiles)
				{
					try
					{
						f.CopyTo(strLocalServer+"/"+strPatchVersion+"/"+f.Name);
					}catch
					{
						//already exist file, no harmless
					}
				}
				UpdateXML(strLocalServer+"/version.xml",strPatchVersion,lstLinkedAssets);
			}
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.Separator();
		
		EditorGUILayout.BeginHorizontal();
		if(GUILayout.Button("Save Setting",GUILayout.Height(32)))
		{
			//todo: SVN ckeckout to certain directory
			SaveSetting();
		}
		if(GUILayout.Button("cancle",GUILayout.Height(32)))
		{
			STEP=0;
			this.Close();
		}
		EditorGUILayout.EndHorizontal();
	}
}
