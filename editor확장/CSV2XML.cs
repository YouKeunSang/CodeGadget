using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Xml;
using System.Text;
namespace AssemblyCSharpEditor
{
    public class CSV2XML : EditorWindow
    {

        [MenuItem("Assets/CSV2XML")]
        public static void MakeXML()
        {
            string path = EditorUtility.OpenFilePanel("CSV파일을 선택하시게..", "", "csv");
            if (null == path || "" == path)
                return;

            MakeXML(path);
            EditorUtility.DisplayDialog("변환이 끝났습니다", "XML로 변환이 끝났습니다.", "확인");
        }

        public static string MakeXML(string path)
        {
            path = path.Replace("\\", "/");
            string[] lines = File.ReadAllLines(path, Encoding.Default);
            string[] url = path.Split('/');
            string filename = url[url.Length - 1];
            /*
             * 1.첫번째 줄의 이름들을 field이름으로 하고 "value="이후 나머지 줄의 같은 열을 attribute로 넣는다.
             * 2.첫번째 열의 이름은은 "id = "후 attribut로 넣는다.
             * 3."//"로 시작하는 라인은 주석임으로 뺀다
             * 4.처음 테이블 이름은 파일 이름으로 한다.
             * 5.끝에 _01,_02,_03으로 끝나는 경우 서브node로 만든다.
             * 6.서브노드로 만들어 지는것은 맨 뒤에 와야한다.
             * 
             */
            lines[0] = lines[0].Replace("//", "");
            string[] fields = lines[0].ToLower().Split(',');
            string table_name = filename.Split('.')[0];
            XmlDocument doc = new XmlDocument();
            XmlNode declaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            XmlNode root = doc.CreateElement(table_name);

            doc.AppendChild(declaration);
            doc.AppendChild(root);

            //remove null field -2013.0909
            List<string> tempFields = new List<string>(fields);
            for (int idx = tempFields.Count - 1; idx >= 0; idx--)
            {
                tempFields.Remove("");
            }
            fields = tempFields.ToArray();

            //서브 노드에 쓰일 구조체를 알아낸다.
            List<string> subFields = SubFields(fields);

            //필드를 보고 sub node에 사용될 필드들을 골라낸다.
            for (int i = 1; i < lines.Length; i++)
            {
                //UnityEngine.Debug.Log(lines[i]);
                if (!lines[i].StartsWith("//"))
                {
                    string[] elements = lines[i].Split(',');
                    XmlElement head = doc.CreateElement(fields[0]);
                    XmlElement subNode = null;
                    head.SetAttribute("id", elements[0]);

                    for (int j = 1; j < fields.Length; j++)
                    {
                        //서브
                        if (IsSubField(subFields, fields[j]))
                        {
                            for (int k = 0; k < subFields.Count; k++)
                            {
                                if (0 == k)
                                {
                                    //시작
                                    subNode = doc.CreateElement(subFields[0]);
                                    subNode.SetAttribute("id", elements[j]);
                                }
                                else
                                {
                                    XmlElement child = doc.CreateElement(subFields[k]);
                                    child.SetAttribute("value", elements[j]);
                                    subNode.AppendChild(child);
                                }
                                j++;
                            }
                            head.AppendChild(subNode);
                            j--;
                        }
                        else
                        {
                            XmlElement child = doc.CreateElement(fields[j]);
                            child.SetAttribute("value", elements[j]);
                            head.AppendChild(child);
                        }
                    }
                    root.AppendChild(head);
                }
            }
            doc.Save(path.Remove(path.LastIndexOf('.')) + ".xml");
            return path.Remove(path.LastIndexOf('.')) + ".xml";
        }

        static bool IsSubField(List<string> subFields, string emement)
        {
            foreach (string s in subFields)
            {
                if (emement.StartsWith(s))
                    return true;
            }
            return false;
        }

        static List<string> SubFields(string[] fields)
        {
            List<string> subs = new List<string>();

            foreach (string s in fields)
            {
                //끝이 _숫자로 끝나거나, 같은 이름이 두개이상 있으면 반복으로 보고 sub field로 분류한다.
                string[] _temp = s.Split('_');
                int num;
                if (_temp.Length > 1 && Int32.TryParse(_temp[_temp.Length - 1], out num))
                {
                    string fieldName = s.Substring(0, s.Length - _temp[_temp.Length - 1].Length - 1);//원본 - "01" -'_'(구분자길이)
                    bool isExist = false;

                    foreach (string str in subs)
                    {
                        if (str == fieldName)
                        {
                            isExist = true;
                            break;
                        }
                    }
                    if (false == isExist)
                    {
                        subs.Add(fieldName);
                    }
                }

                //같은 이름이 2개이상 있는지 확인
                foreach (string origin in fields)
                {
                    int dup = 0;
                    foreach (string comp in fields)
                    {
                        if (comp.Equals(origin))
                            dup++;
                    }
                    if (1 < dup)
                    {
                        subs.Add(origin);
                    }
                }
            }
            return subs;
        }

        static bool isDigit(string num, out int outValue)
        {
            outValue = 0;
            return Int32.TryParse(num, out outValue);
        }
    }
}