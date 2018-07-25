using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using VRUI;
using VRUIControls;
using TMPro;
using IllusionPlugin;

#if NewUI
using BeatSaberUI;
#endif

namespace RandomSong
{
    public class RandomSongManager : MonoBehaviour
    {
        public static RandomSongManager Instance = null;
        public const int MainScene = 1;
        public const int GameScene = 5;

        public static void OnLoad()
        {
            if (Instance != null) return;
            new GameObject("Random Song Manager").AddComponent<RandomSongManager>();
        }

        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
                DontDestroyOnLoad(gameObject);
                Console.WriteLine("Random Song started.");
            }
            else
            {
                Destroy(this);
            }
        }

        public void SceneManagerOnActiveSceneChanged(Scene arg0, Scene scene)
        {
            if (scene.buildIndex == MainScene)
            {
                CreateUI();
            }
        }

        private void CreateUI()
        {
            
        }

        public static void LogComponents(Transform t, string prefix)
        {
            Console.WriteLine(prefix + ">" + t.name);

            foreach (var comp in t.GetComponents<MonoBehaviour>())
            {
                Console.WriteLine(prefix + "-->" + comp.GetType());
            }

            foreach (Transform child in t)
            {
                LogComponents(child, prefix + "=");
            }
        }
    }
}
