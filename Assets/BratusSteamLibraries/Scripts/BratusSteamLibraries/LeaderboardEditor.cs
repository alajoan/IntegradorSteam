using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BratusSteamLibraries;
using Steamworks;

[CustomEditor(typeof(SteamLeaderboard))]
public class LeaderboardEditor : Editor
{
    int width = 200;

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();

        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
        var styleTextArea = new GUIStyle(GUI.skin.textArea) { stretchWidth = true, wordWrap = true, alignment = TextAnchor.MiddleCenter };

        SteamLeaderboard _leaderboard = (SteamLeaderboard)target;

        GUILayout.BeginVertical("Box");
        {
            GUILayout.Space(10);

            EditorGUILayout.LabelField("-- Steam Leaderboard --", style);

            GUILayout.Space(10);

            GUILayout.BeginVertical("box");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("-- Leaderboard name --)", style);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(style);
                {
                    _leaderboard.leaderboardName = GUILayout.TextArea(_leaderboard.leaderboardName, styleTextArea);
                }
                GUILayout.EndHorizontal();


            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical("box");
            {
                GUILayout.BeginHorizontal(style);
                {
                    GUILayout.Label("-- Leaderboard Cell Prefab --", style);

                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(style);
                {
                    _leaderboard.leaderboardPrefab = (GameObject)EditorGUILayout.ObjectField(_leaderboard.leaderboardPrefab, typeof(GameObject)
                        , false);
                }
                GUILayout.EndHorizontal();

            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            {
                GUILayout.BeginHorizontal(style);
                {
                    GUILayout.Label("-- Leaderboard starting range--", style);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(style);
                {
                    _leaderboard.leaderboardEntriesRangeinit = EditorGUILayout.IntField(_leaderboard.leaderboardEntriesRangeinit, styleTextArea);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(style);
                {
                    GUILayout.Label("-- Leaderboard ending range--", style);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(style);
                {
                    _leaderboard.leaderboardEntriesRangeEnd = EditorGUILayout.IntField(_leaderboard.leaderboardEntriesRangeEnd, styleTextArea);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical("box");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("-- Distance between each cell --", style);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    _leaderboard.distanceBetweenCells = EditorGUILayout.FloatField(_leaderboard.distanceBetweenCells, styleTextArea);
                }
                GUILayout.EndHorizontal();

            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical("box");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("-- Upload score method --", style);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    _leaderboard.uploadScoreMethod = (ELeaderboardUploadScoreMethod)EditorGUILayout.EnumPopup(_leaderboard.uploadScoreMethod);
                }
                GUILayout.EndHorizontal();

            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical("box");
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("-- Request type --", style);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    _leaderboard.requestType = (ELeaderboardDataRequest)EditorGUILayout.EnumPopup(_leaderboard.requestType);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            if (GUILayout.Button("Check leaderboard exist (Runtime only)"))
            {
                try
                {
                    SteamLeaderboard.FindLeaderboard();

                }
                catch (InvalidOperationException)
                {
                    Debug.Log("Since it is a static method, it needs an instance of the steamLeaderboard. Use only at runtime");
                }
            }

        }
        GUILayout.EndVertical();

        EditorUtility.SetDirty(_leaderboard);
    }


}
