/*
 * 젠킨스를 통한 외부 콘솔창에서 에셋들을 자동으로 빌드하기 위한 스크립트
 * 물론 내부에서도 동작해야 한다.
 */
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace AssemblyCSharpEditor
{
    public struct AssetNode
    {
        public enum TYPE {
            SCENE,
            FILE,
            DIRECTORY
        }
        public string name;
        public TYPE type;
        public string location;
    }

    public class AssetBuildScript : MonoBehaviour
    {
        const string BUNDLE_LOCATION = "./Bundles/";
        const string LIST_LOCATION = "./Assets/Editor/AssetList.csv";
        static BuildTarget platform = BuildTarget.Android;

        /// <summary>
        ///  각 플랫폼 별로 빌드를 시작하는 트리거 함수들
        /// </summary>
        [MenuItem("AssetBundle/build Android")]
        public static void BuildAssetsAndroid()
        {
            platform = BuildTarget.Android;
            CommonBuild();
        }
        [MenuItem("AssetBundle/build iPhone")]
        public static void BuildAssetsIPhone()
        {
            platform = BuildTarget.iPhone;
            CommonBuild();
        }
        [MenuItem("AssetBundle/build PC")]
        public static void BuildAssetsPC()
        {
            platform = BuildTarget.StandaloneWindows;
            CommonBuild();
        }

        static void CommonBuild()
        {
            string strSaveLoc = BUNDLE_LOCATION + platform.ToString();
            //빌드환경을 USE_ASSETBUNDLE로 강제로 지정한다.
            ProjectBuilder.DefineUseAssetBundle();

            //에셋을 저장할 디렉토리를 만든다.
            Directory.CreateDirectory(strSaveLoc);

            AssetNode[] assets = ParseList(File.ReadAllLines(LIST_LOCATION));
            

            foreach (AssetNode node in assets)
            {
                if (node.type == AssetNode.TYPE.DIRECTORY)
                {
                    DirectoryInfo dir = new DirectoryInfo("."+node.location);
                    FileInfo[] prefabFiles = dir.GetFiles("*.prefab", SearchOption.AllDirectories);
                    FileInfo[] xmlFiles = dir.GetFiles("*.xml", SearchOption.AllDirectories);


                    Object[] objects = new Object[prefabFiles.Length + xmlFiles.Length];

                    for (int i = 0; i < prefabFiles.Length; i++)
                    {
                        string _path = prefabFiles[i].FullName;
                        _path = _path.Replace("\\", "/");
                        _path = "Assets"+_path.Replace(Application.dataPath, "");
                        objects[i] = AssetDatabase.LoadMainAssetAtPath(_path);
                        UnityEngine.Debug.Log(_path);
                    }
                    for (int i = prefabFiles.Length; i < prefabFiles.Length + xmlFiles.Length; i++)
                    {
                        string _path = xmlFiles[i].FullName;
                        _path = _path.Replace("\\", "/");
                        _path = "Assets" + _path.Replace(Application.dataPath, "");
                        objects[i] = AssetDatabase.LoadMainAssetAtPath(_path);
                        UnityEngine.Debug.Log(_path);
                    }
                    BuildPipeline.BuildAssetBundle(null, objects, strSaveLoc+"/" + node.name, BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets | BuildAssetBundleOptions.DeterministicAssetBundle, platform);
                }
            }
            EditorApplication.Beep();
            EditorUtility.DisplayDialog("Scene Asset Bundle Creation", "Done!", "OK");
        }

        static AssetNode[] ParseList(string[] lists)
        {
            List<string> _lists = new List<string>();
            foreach (string s in lists)
            {
                if (!s.StartsWith("//"))
                {
                    _lists.Add(s);
                }
            }
            AssetNode[] assets = new AssetNode[_lists.Count];
            for (int i = 0; i < _lists.Count; i++)
            {
                string[] _temp = _lists[i].Split(',');
                if(3==_temp.Length)
                {
                    assets[i].name = _temp[0];
                    assets[i].location = _temp[2];
                    switch (_temp[1].ToLower())
                    {
                        case "scene":
                            assets[i].type = AssetNode.TYPE.SCENE;
                            break;
                        case "file":
                            assets[i].type = AssetNode.TYPE.FILE;
                            break;
                        case "directory":
                            assets[i].type = AssetNode.TYPE.DIRECTORY;
                            break;
                        default:
                            UnityEngine.Debug.LogError("알수 없는 타입이 들어옴:"+_temp[1]);
                            break;
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError("CVS파일의 필드수가 다릅니다.");
                }
            }

            return assets;
        }
    }
}