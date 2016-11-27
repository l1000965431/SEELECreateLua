using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Linq;
using System.IO;
using Object = UnityEngine.Object;
using System.Collections.Generic;

namespace SEELE
{
    public class CreateLuaScrip : EditorWindow
    {
        private Vector2 assetListScroll;
        Object[] selection;
        string className = "";
        string parentName = "";
        string path = "";

        List<string> formattedExports = new List<string>();
        List<GameObject> gameObjList = new List<GameObject>();
        List<GameObject> syntaxTree = new List<GameObject>();

        string classModel = "local CLASSNAME = class(\"CLASSNAME\")";
        string parentClassModel = "local CLASSNAME = class(\"CLASSNAME\",\"PARENTCLASS\")";
        string functionModel = "function CLASSNAME:FUNCTIONNAME()";
        string variableModel = "    self.VARIABLENAMEObj = Util.FindChildBreadth(self.PARENTCLASS,\"OBJNAME\")";
        string endModel = "end \n";
        string returnModel = "return CLASSNAME";

        [MenuItem("SEELE/CreateDlgLua")]
        public static void CreateLuaScripWin()
        {
            CreateLuaScrip sc = GetWindow<CreateLuaScrip>();
            sc.Show();
        }

        void OnGUI()
        {
            GUILayout.Label("设置类和父类名称,设置好保存路径,点击生成");

            className = EditorGUILayout.TextField("Class Name: ", className);
            parentName = EditorGUILayout.TextField("Parent Name: ", parentName);
            path = EditorGUILayout.TextField("Path Name: ", path);

            ScreeningGameObjectList();

            GUILayout.Label("当前需要导出的控件:");
            assetListScroll = GUILayout.BeginScrollView(assetListScroll, "box");
            foreach (string export in formattedExports)
            {
                GUILayout.Label(export);
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Create"))
            {
                WriteFile();
                EditorGUILayout.HelpBox("生成成功", MessageType.None);
            }

            this.Repaint();
        }

        private bool SameContent(Object[] a, Object[] b)
        {
            if (a == null && b == null) return true;
            if (a != null && b == null) return false;
            if (a == null && b != null) return false;
            if (a.Length != b.Length) return false;

            var shared = a.Intersect(b);
            return shared.Count() == a.Length;
        }


        void WriteFile()
        {
            StreamWriter sw;
            FileInfo file = CreateFile();

            if (file == null)
            {
                EditorGUILayout.HelpBox("生成失败，请检查文件路径", MessageType.Error);
                return;
            }

            sw = file.CreateText();

            string info = "";
            if (parentName == string.Empty)
            {
                info = classModel.Clone().ToString().Replace("CLASSNAME", className);
            }
            else
            {
                info = parentClassModel.Clone().ToString().Replace("CLASSNAME", className).Replace("PARENTCLASS", parentName);
            }
            sw.WriteLine(info);

            info = functionModel.Clone().ToString().Replace("CLASSNAME", className).Replace("FUNCTIONNAME", "Init");
            sw.WriteLine(info);
            CreateWriteList(sw);
            info = endModel.Clone().ToString();
            sw.WriteLine(info);


            info = functionModel.Clone().ToString().Replace("CLASSNAME", className).Replace("FUNCTIONNAME", "OnOpen");
            sw.WriteLine(info);
            info = endModel.Clone().ToString();
            sw.WriteLine(info);

            info = functionModel.Clone().ToString().Replace("CLASSNAME", className).Replace("FUNCTIONNAME", "OnClose");
            sw.WriteLine(info);
            info = endModel.Clone().ToString();
            sw.WriteLine(info);

            info = functionModel.Clone().ToString().Replace("CLASSNAME", className).Replace("FUNCTIONNAME", "OnDestory");
            sw.WriteLine(info);
            info = endModel.Clone().ToString();
            sw.WriteLine(info);

            info = returnModel.Clone().ToString().Replace("CLASSNAME", className);
            sw.WriteLine(info);

            sw.Close();
            sw.Dispose();
        }

        FileInfo CreateFile()
        {
            if (path == string.Empty)
            {
                return null;
            }

            DirectoryInfo dir = null;
            if (!Directory.Exists(path))
            {
                dir = Directory.CreateDirectory(path);

                if (dir == null)
                {
                    return null;
                }
            }


            FileInfo t = new FileInfo(path + "//" + className + ".lua");
            if (t.Exists)
            {
                t.Delete();
                t = new FileInfo(path + "//" + className + ".lua");
            }

            return t;
        }

        
        //筛选选择列表
        void ScreeningGameObjectList()
        {
            if (!SameContent(selection, Selection.objects))
            {
                selection = Selection.objects;
            }

            formattedExports.Clear();
            gameObjList.Clear();
            syntaxTree.Clear();
            foreach (Object obj in selection)
            {
                GameObject o = obj as GameObject;
                if ( o != null )
                {
                    gameObjList.Add(o);
                    AddSelectName(o);
                }
            }

            CreateSyntaxTree();
        }



        string getVariableString(string variableName,string parentName,string objname)
        {
            string info = variableModel.Clone().ToString();
            return info.Replace("VARIABLENAME", variableName).Replace("PARENTCLASS", parentName).Replace("OBJNAME", objname);           
        }



        //生成写入列表
        void CreateWriteList(StreamWriter sw)
        {
            foreach (GameObject obj in syntaxTree)
            {
                string variableName = obj.transform.name.Replace(" ", "").Replace("-", "").Replace("_", "");
                string info = "";
                if (obj.transform.parent.gameObject.name == "Camera" || !obj.transform.parent || 
                    !syntaxTree.Find((x) => { return x == obj.transform.parent.gameObject; }))
                {
                    info = getVariableString(variableName, "gameObj", obj.transform.name);
                }
                else
                {
                    info = getVariableString(variableName, obj.transform.parent.gameObject.name+"Obj", obj.transform.name);
                }
                
                sw.WriteLine(info);
            }
        }


        void AddSelectName(GameObject o)
        {
            if (o.transform.parent != null)
            {
                formattedExports.Add(o.transform.parent.name + "_" + o.name);
            }
            else
            {
                formattedExports.Add(o.name);
            }
        }

        void AddSyntaxTree(GameObject o,bool isHead = false)
        {
            if (syntaxTree.Find((x) => { return x == o; }))
            {
                return;
            }
            if (isHead)
            {
                syntaxTree.Insert(0,o);
            }
            else
            {
                syntaxTree.Add(o);
            }
            
        }

        void CreateSyntaxTree()
        {
            GameObject parent = null;
            foreach (GameObject obj in gameObjList)
            {
                parent = obj.transform.parent.gameObject;
                if (parent.name == "Camera" || !parent)
                {
                    AddSyntaxTree(obj);
                }
                else
                {
                    AddSyntaxTree(parent,true);
                    AddSyntaxTree(obj);
                }
            }
        }

    }
}


