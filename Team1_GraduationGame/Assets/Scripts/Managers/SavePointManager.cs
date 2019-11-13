﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Team1_GraduationGame.Enemies;
using Team1_GraduationGame.Interaction;
using UnityEditor;
using UnityEngine;

namespace Team1_GraduationGame.SaveLoadSystem
{
    public class SavePointManager : MonoBehaviour
    {
        // References:
        public SaveLoadManager saveLoadManager;

        // Public
        public int firstSceneBuildIndex = 0;
        public bool drawGizmos = true;
        [HideInInspector] public List<GameObject> savePoints;
        [HideInInspector] public int previousCheckPoint = 1;


        public void Awake()
        {
            saveLoadManager = new SaveLoadManager();
            saveLoadManager.firstSceneIndex = firstSceneBuildIndex;

            if (PlayerPrefs.GetInt("loadGameOnAwake") == 1)
            {
                PlayerPrefs.SetInt("loadGameOnAwake", 0);
                saveLoadManager.LoadGame();
            }
        }

        private void Start()
        {
            if (FindObjectOfType<HubMenu>() != null)
            {
                FindObjectOfType<HubMenu>().startGameEvent += NewGame;
                FindObjectOfType<HubMenu>().continueGameEvent += Continue;
            }
        }

        public void DisableSavingOnSavePoints()
        {
            if (savePoints != null && Application.isPlaying)    // Should be called when playing (for debugging)
                for (int i = 0; i < savePoints.Count; i++)
                {
                    if (savePoints[i].GetComponent<SavePoint>() != null)
                    {
                        savePoints[i].GetComponent<SavePoint>().savingDisabled = true;
                    }
                }
        }

        public void TeleportToSavePoint(int savePointNumber)
        {
            if (savePoints.ElementAtOrDefault(savePointNumber - 1))
            {
                if (GameObject.FindGameObjectWithTag("Player") != null)
                {
                    GameObject tempPlayer = GameObject.FindGameObjectWithTag("Player");
                    tempPlayer.transform.position =
                        savePoints[savePointNumber - 1].transform.position + transform.up;

                    tempPlayer.GetComponent<Movement>().Frozen(false);
                }
            }
        }

        public void LoadToPreviousCheckpoint()
        {
            if (savePoints.ElementAtOrDefault(previousCheckPoint - 1))
            {
                if (GameObject.FindGameObjectWithTag("Player") != null)
                {
                    GameObject.FindGameObjectWithTag("Player").transform.position =
                        savePoints[previousCheckPoint - 1].transform.position + transform.up;
                }
            }
        }

        public void NewGame()
        {
            saveLoadManager?.NewGame();
        }

        public void Continue()
        {
            saveLoadManager?.ContinueGame();
        }

        public void SaveGame()
        {
            saveLoadManager?.SaveGame();
        }

        public void LoadGame()
        {
            saveLoadManager?.LoadGame();
        }

        public void NextLevel()
        {
            saveLoadManager?.NextLevel();
        }

#if UNITY_EDITOR
        public void AddSavePoint()
        {
            if (Application.isEditor)
            {
                GameObject tempSavePoint;

                if (savePoints == null)
                    savePoints = new List<GameObject>();

                tempSavePoint = new GameObject("SavePoint" + (savePoints.Count + 1));
                tempSavePoint.AddComponent<SavePoint>();
                tempSavePoint.transform.position = gameObject.transform.position;
                tempSavePoint.transform.parent = transform;
                tempSavePoint.layer = 2;

                savePoints.Add(tempSavePoint);
                tempSavePoint.GetComponent<SavePoint>().thisID = savePoints.Count;
                tempSavePoint.GetComponent<SavePoint>().thisSavePointManager = gameObject.GetComponent<SavePointManager>();
            }
        }

        private void OnDrawGizmos()
        {
            if (drawGizmos && Application.isEditor)
                if (savePoints != null)
                {
                    Gizmos.color = Color.magenta;
                    Handles.color = Color.red;

                    for (int i = 0; i < savePoints.Count; i++)
                    {
                        Gizmos.DrawWireSphere(savePoints[i].transform.position, 1.0f);
                        Handles.Label(savePoints[i].transform.position + (Vector3.up * 1.0f), "SavePoint " + (i + 1));

                        if (savePoints[i].GetComponent<Collider>() != null)
                        {
                            Gizmos.color = Color.white;
                            Collider tempCollider = savePoints[i].GetComponent<Collider>();
                            Gizmos.DrawWireCube(tempCollider.bounds.center, savePoints[i].GetComponent<Collider>().bounds.size);
                        }
                    }
                }
        }
#endif

    }

    #region Custom Inspector
#if UNITY_EDITOR
    [CustomEditor(typeof(SavePointManager))]
    public class SavePointManager_Inspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var script = target as SavePointManager;

            EditorGUILayout.Space();

            if (script.savePoints != null)
            {
                EditorGUILayout.LabelField(script.savePoints.Count.ToString() + " SavePoints Active");
            }
            else
            {
                EditorGUILayout.LabelField("0 SavePoints Active");
            }

            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("Please only create new savepoints by using the 'Add SavePoint' button. IMPORTANT: The first savepoint must be at the player start position of the level!", MessageType.Info);

            if (GUILayout.Button("Add SavePoint"))
            {
                script.AddSavePoint();
            }

            if (GUI.changed)
                EditorUtility.SetDirty(script);
        }
    }
#endif
    #endregion
}
