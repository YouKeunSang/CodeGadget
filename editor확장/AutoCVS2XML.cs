using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System;
using System.Net;
using System.Diagnostics;

namespace AssemblyCSharpEditor
{
    public class AutoCVS2XML : EditorWindow
    {
        //***** SVN관련 *****
        //SVN을 실행하기 위한 실행파일 위치
        static string strExecCmd = @"c:/Program Files/TortoiseSVN/bin/TortoiseProc.exe";
        static string strPlanSVN = "https://172.20.46.139:8443/svn/Gladiator/trunk/plan/table";

        //***** 내부 디렉토리및 파일 *****
        //기획문서를 잠시 다운받기 위한 디렉토리
        static string strPlantDir = @"/plan/";
        //실제 기획문서들이 XML로 변환되어 있는 디렉토리
        static string strTableDir = @"/Resources/Tables/GameTables";
        static FileInfo[] currentXMLfiles = null;
        static FileInfo[] planCVSfiles = null;

        [MenuItem("Assets/전체 기획문서 적용")]
        static void Init()
        {
            string _strPlantDir = Application.dataPath + strPlantDir;
            string _strTableDir = Application.dataPath + strTableDir;

            //svn실행파일을 확인한다.
            if (!File.Exists(strExecCmd))
            {
                EditorApplication.Beep();
                EditorUtility.DisplayDialog("TortoiseSVN이 설치되어있지 않습니다\n또는 64비트 운영체제에 32비트 SVN이 설치되어 있습니다", "프로그램 확인하세요!", "내가잘못했다ㅜㅜ");
                return;
            }
            //기획문서를 받을 디렉토리를 검사한다.
            if (Directory.Exists(_strPlantDir))
            {
                Directory.Delete(_strPlantDir,true);
            }
            Directory.CreateDirectory(_strPlantDir);

            Process proc = Process.Start(strExecCmd,"/command:checkout /path:\""+_strPlantDir+"\" /url:\""+strPlanSVN+"\" /closeonend:1");
            //Process proc = Process.Start("CMD", "/C svn export " + strPlanSVN + " " + _strPlantDir);
            if (proc.WaitForExit(20000))
            {
                UnityEngine.Debug.Log("checkout normaly exited");
            }
            else
            {
                EditorApplication.Beep();
                EditorUtility.DisplayDialog("SVN이 의도치 않게 종료되었습니다", "cmdline에서 svn이 되는지 확인", "내가잘못했다ㅜㅜ");
                UnityEngine.Debug.Log("checkout time expired");
                goto end;
            }

            //현재 사용하는 xml테이블 리스트를 만든다.
            DirectoryInfo dir = new DirectoryInfo(_strTableDir);
            currentXMLfiles = dir.GetFiles("*.xml", SearchOption.TopDirectoryOnly);

            //모든 기획문서의 리스트
            dir = new DirectoryInfo(_strPlantDir);
            planCVSfiles = dir.GetFiles("*.csv", SearchOption.AllDirectories);

            //현재사용하는 xml의 이름과 같은 기획문서가 있으면 변환후 crc체크로 변경되었는지 확인
            foreach (FileInfo xmlFile in currentXMLfiles)
            {
                foreach (FileInfo cvsfile in planCVSfiles)
                {
                    if (GetFileNameWithoutExt(xmlFile) == GetFileNameWithoutExt(cvsfile))
                    {
                        //우선 cvs파일을 xml로 바꾼다.
                        string convertedXML = CSV2XML.MakeXML(cvsfile.FullName);
                        FileInfo file = new FileInfo(convertedXML);
                        if (!file.Equals(xmlFile))
                        {
                            UnityEngine.Debug.Log("파일이 다릅니다:" + xmlFile.FullName);
                            file.CopyTo(xmlFile.FullName, true);
                        }

                    }
                }
            }
            proc = Process.Start(strExecCmd, "/command:commit /path:\"" + _strTableDir + "\" /logmsg:\"변경이유를 여기에 적으세요\" " + " /closeonend:1");

            end:
                UnityEngine.Debug.Log("다운받은 디렉토리를 지웁니다");
                DeleteDirectory(_strPlantDir);
        }

        static string GetFileNameWithoutExt(FileInfo file)
        {
            return file.Name.Replace(file.Extension, "");
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

            Directory.Delete(target_dir, true);
        }
    }
}