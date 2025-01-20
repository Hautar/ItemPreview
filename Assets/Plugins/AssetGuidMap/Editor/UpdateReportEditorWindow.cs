#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace AssetGuidMap.Editor
{
    internal class UpdateReportEditorWindow : EditorWindow
    {
        private UpdateReport report;
        private int          entriesPerPage = 50;
        private int          currentPage = 0;
        private int          pagesMax;
        private Vector2 scrollValue;

        public bool result;

        private void OnGUI()
        {
            DrawPagination();
            DrawLog();
            DrawButtons();
        }

        private void DrawPagination()
        {
            if (report.Count == 0)
            {
                GUILayout.Label("Empty");
                return;
            }

            pagesMax = report.Count / entriesPerPage + (report.Count % entriesPerPage > 0 ? 0 : -1);
            
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Page:", GUILayout.Width(50));
                
                if (GUILayout.Button("<", GUILayout.Width(25)))
                {
                    currentPage--;
                }

                currentPage = EditorGUILayout.IntField(currentPage, GUILayout.Width(50));
                
                if (GUILayout.Button(">", GUILayout.Width(25)))
                {
                    currentPage++;
                }
                if (GUILayout.Button(">>", GUILayout.Width(25)))
                {
                    currentPage = pagesMax;
                }

                currentPage = Mathf.Clamp(currentPage, 0, pagesMax);
                
                GUILayout.Label($"of {pagesMax}", GUILayout.Width(50));

            }
            GUILayout.EndHorizontal();
        }

        private void DrawLog()
        {
            if (report.Count == 0)
                return;
            
            int start = currentPage * entriesPerPage;
            int end = Math.Min(start + entriesPerPage - 1, report.Count - 1);

            GUILayout.BeginVertical();
            GUILayout.Space(20);            
            GUILayout.Label("Report:");
            GUILayout.Space(10);

            scrollValue = GUILayout.BeginScrollView(scrollValue);
            {
                for (int i = start; i <= end; i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(i.ToString(), GUILayout.Width(50));
             
                    var initialColor = GUI.color;
                    switch (report.Type(i))
                    {
                        case UpdateReportMessageType.Error:
                            GUI.contentColor = Color.red;
                            break;
                        case UpdateReportMessageType.Warning:
                            GUI.contentColor = Color.yellow;
                            break;
                    }
                
                    GUILayout.Label(report.Message(i));
                
                    GUI.contentColor = initialColor;
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        private void DrawButtons()
        {
            if (currentPage != pagesMax &&
                report.Count != 0)
                return;

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Apply", GUILayout.Width(200)))
                {
                    result = true;
                    Close();
                }

                if (GUILayout.Button("Cancel", GUILayout.Width(200)))
                {
                    result = false;
                    Close();
                }
            }
            GUILayout.EndHorizontal();
        }

        public static bool Show(UpdateReport report)
        {
            var window = GetWindow<UpdateReportEditorWindow>("Update report");
            window.minSize = new Vector2(900, 500);
            window.maxSize = new Vector2(900, 1050);
            window.report = report;
            window.ShowModal();

            return window.result;
        }
    }
}
#endif