using UnityEngine;
using UnityEditor;

namespace AssemblyCSharpEditor
{
	public class AssetBuilder
	{
        [MenuItem("Assets/AssetBundle/build stream bundle-IPhone")]
        public static void StreamAssetBuilderIPhone()
        {
            string assetPath;

            //set a save location for the bundle:
			string savePath = EditorUtility.SaveFolderPanel("Save Asset Folder",null,null);
			if (savePath == "")
			{
				return;
			}
	
			foreach(Object obj in Selection.objects)
			{
				assetPath = AssetDatabase.GetAssetPath(obj);
				string _tempSavePath = savePath+"/"+obj.name;
				//save asset bundles for iphone and android:
				if(!_tempSavePath.EndsWith(".unity3d"))
				{
					_tempSavePath=_tempSavePath+".unity3d";
				}

            	BuildPipeline.BuildStreamedSceneAssetBundle(new string[] { assetPath }, _tempSavePath, BuildTarget.iPhone);
			}

            //complete:
            EditorApplication.Beep();
            EditorUtility.DisplayDialog("Scene Asset Bundle Creation", "Done!", "OK");
        }

        [MenuItem("Assets/AssetBundle/build patch bundle-IPhone")]
        public static void PatchAssetBuilderIPhone()
        {
			//string assetPath;
		
			//set a save location for the bundle:
			string savePath = EditorUtility.SaveFolderPanel("Save Asset Folder",null,null);
			if (savePath == "")
			{
				return;
			}
	
			foreach(Object obj in Selection.objects)
			{
				//assetPath = AssetDatabase.GetAssetPath(obj);
				string _tempSavePath = savePath+"/"+obj.name;
				//save asset bundles for iphone and android:
				if(!_tempSavePath.EndsWith(".unity3d"))
				{
					_tempSavePath=_tempSavePath+".unity3d";
				}
				BuildPipeline.BuildAssetBundle(obj,null,_tempSavePath,BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets | BuildAssetBundleOptions.DeterministicAssetBundle, BuildTarget.iPhone);
			}
			//complete:
			EditorApplication.Beep();
			EditorUtility.DisplayDialog("Scene Asset Bundle Creation", "Done!", "OK");
        }
        [MenuItem("Assets/AssetBundle/build multi-pack patch bundle-IPhone")]
        public static void MultiPackPatchAssetBuilder_IPhone()
        {
            //string assetPath;

            //set a save location for the bundle:
            //string savePath = EditorUtility.SaveFolderPanel("Save Asset Folder", null, null);
            string savePath = EditorUtility.SaveFilePanel("묶음번들 저장", null, null, "unity3d");
            if (savePath == "")
            {
                return;
            }

            BuildPipeline.BuildAssetBundle(null, Selection.objects, savePath, BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets | BuildAssetBundleOptions.DeterministicAssetBundle, BuildTarget.iPhone);

            //complete:
            EditorApplication.Beep();
            EditorUtility.DisplayDialog("Scene Asset Bundle Creation", "Done!", "OK");
        }
		[MenuItem("Assets/AssetBundle/build stream bundle-Android")]
		public static void StreamAssetBuilder ()
		{
			string assetPath;
		
			//set a save location for the bundle:
			string savePath = EditorUtility.SaveFolderPanel("Save Asset Folder",null,null);
			if (savePath == "")
			{
				return;
			}
	
			foreach(Object obj in Selection.objects)
			{
				assetPath = AssetDatabase.GetAssetPath(obj);
				string _tempSavePath = savePath+"/"+obj.name;
				//save asset bundles for iphone and android:
				if(!_tempSavePath.EndsWith(".unity3d"))
				{
					_tempSavePath=_tempSavePath+".unity3d";
				}
		
				//BuildPipeline.BuildStreamedSceneAssetBundle(new string[] { assetPath }, pathPieces[0] + "." +pathPieces[1], BuildTarget.Android);
				BuildPipeline.BuildStreamedSceneAssetBundle(new string[] { assetPath },_tempSavePath, BuildTarget.Android);
			}
			//complete:
			EditorApplication.Beep();
			EditorUtility.DisplayDialog("Scene Asset Bundle Creation", "Done!", "OK");
		}
		[MenuItem("Assets/AssetBundle/build patch bundle-Android")]
		public static void PatchAssetBuilder()
		{
			//string assetPath;
		
			//set a save location for the bundle:
			string savePath = EditorUtility.SaveFolderPanel("Save Asset Folder",null,null);
			if (savePath == "")
			{
				return;
			}
	
			foreach(Object obj in Selection.objects)
			{
				//assetPath = AssetDatabase.GetAssetPath(obj);
				string _tempSavePath = savePath+"/"+obj.name;
				//save asset bundles for iphone and android:
				if(!_tempSavePath.EndsWith(".unity3d"))
				{
					_tempSavePath=_tempSavePath+".unity3d";
				}
		
				BuildPipeline.BuildAssetBundle(obj,null,_tempSavePath,BuildAssetBundleOptions.CollectDependencies|BuildAssetBundleOptions.CompleteAssets|BuildAssetBundleOptions.DeterministicAssetBundle,BuildTarget.Android);
                //BuildPipeline.BuildAssetBundle(obj, null, _tempSavePath, BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets , BuildTarget.Android);
			}
			//complete:
			EditorApplication.Beep();
			EditorUtility.DisplayDialog("Scene Asset Bundle Creation", "Done!", "OK");
		}
        [MenuItem("Assets/AssetBundle/build multi-pack patch bundle-Android")]
        public static void MultiPackPatchAssetBuilder()
        {
            //string assetPath;

            //set a save location for the bundle:
            //string savePath = EditorUtility.SaveFolderPanel("Save Asset Folder", null, null);
            string savePath = EditorUtility.SaveFilePanel("묶음번들 저장",null, null, "unity3d");
            if (savePath == "")
            {
                return;
            }

            BuildPipeline.BuildAssetBundle(null, Selection.objects, savePath, BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets | BuildAssetBundleOptions.DeterministicAssetBundle, BuildTarget.Android);
            //BuildPipeline.BuildAssetBundle(obj, null, _tempSavePath, BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets , BuildTarget.Android);

            //complete:
            EditorApplication.Beep();
            EditorUtility.DisplayDialog("Scene Asset Bundle Creation", "Done!", "OK");
        }

		[MenuItem("Assets/AssetBundle/build stream bundle-PC")]
		public static void StreamAssetBuilderPC ()
		{
			string assetPath;
		
			//set a save location for the bundle:
			string savePath = EditorUtility.SaveFolderPanel("Save Asset Folder",null,null);
			if (savePath == "")
			{
				return;
			}
	
			foreach(Object obj in Selection.objects)
			{
				assetPath = AssetDatabase.GetAssetPath(obj);
				string _tempSavePath = savePath+"/"+obj.name;
				//save asset bundles for iphone and android:
				if(!_tempSavePath.EndsWith(".unity3d"))
				{
					_tempSavePath=_tempSavePath+".unity3d";
				}
	
				BuildPipeline.BuildStreamedSceneAssetBundle(new string[] { assetPath }, _tempSavePath, BuildTarget.StandaloneWindows);
			}
			//complete:
			EditorApplication.Beep();
			EditorUtility.DisplayDialog("Scene Asset Bundle Creation", "Done!", "OK");
		}
		
		[MenuItem("Assets/AssetBundle/build patch bundle-PC")]
		public static void PatchAssetBuilderPC ()
		{
			//string assetPath;
		
			//set a save location for the bundle:
			string savePath = EditorUtility.SaveFolderPanel("Save Asset Folder",null,null);
			if (savePath == "")
			{
				return;
			}
	
			foreach(Object obj in Selection.objects)
			{
				//assetPath = AssetDatabase.GetAssetPath(obj);
				string _tempSavePath = savePath+"/"+obj.name;
				//save asset bundles for iphone and android:
				if(!_tempSavePath.EndsWith(".unity3d"))
				{
					_tempSavePath=_tempSavePath+".unity3d";
				}
		
				BuildPipeline.BuildAssetBundle(obj,null,_tempSavePath,BuildAssetBundleOptions.CollectDependencies|BuildAssetBundleOptions.CompleteAssets|BuildAssetBundleOptions.DeterministicAssetBundle,BuildTarget.StandaloneWindows);
			}
			//complete:
			EditorApplication.Beep();
			EditorUtility.DisplayDialog("Scene Asset Bundle Creation", "Done!", "OK");
		}
        [MenuItem("Assets/AssetBundle/build multi-pack patch bundle-PC")]
        public static void MultiPackPatchAssetBuilder_PC()
        {
            //string assetPath;

            //set a save location for the bundle:
            //string savePath = EditorUtility.SaveFolderPanel("Save Asset Folder", null, null);
            string savePath = EditorUtility.SaveFilePanel("묶음번들 저장", null, null, "unity3d");
            if (savePath == "")
            {
                return;
            }

            BuildPipeline.BuildAssetBundle(null, Selection.objects, savePath, BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets | BuildAssetBundleOptions.DeterministicAssetBundle, BuildTarget.StandaloneWindows);
            //BuildPipeline.BuildAssetBundle(obj, null, _tempSavePath, BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets , BuildTarget.StandaloneWindows);

            //complete:
            EditorApplication.Beep();
            EditorUtility.DisplayDialog("Scene Asset Bundle Creation", "Done!", "OK");
        }
	}
}

