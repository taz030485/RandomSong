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
using UnityEngine.Events;
using HMUI;

namespace RandomSong
{
    public class RandomSongManager : MonoBehaviour
    {
        public static RandomSongManager Instance = null;

        StandardLevelSelectionFlowCoordinator flowController = null;
        StandardLevelSelectionNavigationController navController = null;
        StandardLevelListViewController listViewController = null;
        StandardLevelDifficultyViewController difficultyViewController;
        StandardLevelListTableView listTableView = null;
        TableView tableView = null;
        StandardLevelDetailViewController detailViewController = null;
        SongPreviewPlayer player = null;

        private Button randomButton;

        Queue<IStandardLevel> pastSongs = null;

        static int allowAfter = 10;
        static bool excludeStandard = false;
        static bool autoPlay = true;

        static Vector2 pos = new Vector2(65.0f, 74.0f);

        LevelDifficulty minDiff = LevelDifficulty.Easy;
        LevelDifficulty maxDiff = LevelDifficulty.ExpertPlus;

        const string excludeStandardSetting = "excludeStandard";
        const string autoPlaySetting = "autoPlay";
        const string minDiffSetting = "minDiff";
        const string maxDiffSetting = "maxDiff";

        IStandardLevel level = null;
        bool isShowing = false;

        public static void OnLoad()
        {
            if (Instance != null) return;
            new GameObject("Random Song Manager").AddComponent<RandomSongManager>();
        }

        public static bool isMenuScene(Scene scene)
        {
            return (scene.name == "Menu");
        }

        public static bool isGameScene(Scene scene)
        {
            return (scene.name == "StandardLevel");
        }

        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
                DontDestroyOnLoad(gameObject);
                Console.WriteLine("Random Song started.");

                pastSongs = new Queue<IStandardLevel>(20);

                excludeStandard = ModPrefs.GetBool(Plugin.PluginName, excludeStandardSetting, false, true);
                autoPlay = ModPrefs.GetBool(Plugin.PluginName, autoPlaySetting, true, true);
                minDiff = (LevelDifficulty)ModPrefs.GetInt(Plugin.PluginName, minDiffSetting, (int)LevelDifficulty.Easy, true);
                maxDiff = (LevelDifficulty)ModPrefs.GetInt(Plugin.PluginName, maxDiffSetting, (int)LevelDifficulty.ExpertPlus, true);
            }
            else
            {
                Destroy(this);
            }
        }

        public void SceneManagerOnActiveSceneChanged(Scene arg0, Scene scene)
        {
            if (isMenuScene(scene))
            {
                flowController = Resources.FindObjectsOfTypeAll<StandardLevelSelectionFlowCoordinator>().FirstOrDefault();
                navController = flowController.GetPrivateField<StandardLevelSelectionNavigationController>("_levelSelectionNavigationController");
                listViewController = flowController.GetPrivateField<StandardLevelListViewController>("_levelListViewController");
                difficultyViewController = flowController.GetPrivateField<StandardLevelDifficultyViewController>("_levelDifficultyViewController");
                listTableView = listViewController.GetPrivateField<StandardLevelListTableView>("_levelListTableView");
                tableView = listTableView.GetPrivateField<TableView>("_tableView");
                //tableView.didSelectRowEvent += didSelectRowEvent;
                detailViewController = flowController.GetPrivateField<StandardLevelDetailViewController>("_levelDetailViewController");
                player = Resources.FindObjectsOfTypeAll<SongPreviewPlayer>().FirstOrDefault();

                FixDiffOrder();
                CreatUI();
                CreateRandomButton();
            }
            else
            {
                isShowing = false;
            }
        }

        private void didSelectRowEvent(TableView arg1, int arg2)
        {
            RandomSong();
        }

        void FixDiffOrder()
        {
            if (maxDiff < minDiff)
            {
                var temp = minDiff;
                minDiff = maxDiff;
                maxDiff = temp;
                ModPrefs.SetInt(Plugin.PluginName, minDiffSetting, (int)minDiff);
                ModPrefs.SetInt(Plugin.PluginName, maxDiffSetting, (int)maxDiff);
            }
        }

        private void CreatUI()
        {
            var subMenu = SettingsUI.CreateSubMenu("Random Song");
            var min = subMenu.AddList("Min Song Difficulty", Difficulties());
            min.GetValue += delegate {
                return (float)minDiff;
            };
            min.SetValue += delegate (float value) {
                minDiff = (LevelDifficulty)value;
                ModPrefs.SetInt(Plugin.PluginName, minDiffSetting, (int)minDiff);
            };
            min.FormatValue += delegate (float value) { return LevelDifficultyMethods.Name((LevelDifficulty)value); };

            var max = subMenu.AddList("Max Song Difficulty", Difficulties());
            max.GetValue += delegate {
                return (float)maxDiff;
            };
            max.SetValue += delegate (float value) {
                maxDiff = (LevelDifficulty)value;
                ModPrefs.SetInt(Plugin.PluginName, maxDiffSetting, (int)maxDiff);
            };
            max.FormatValue += delegate (float value) { return LevelDifficultyMethods.Name((LevelDifficulty)value); };

            var exclude = subMenu.AddBool("Exclude Standard Songs");
            exclude.GetValue += delegate {
                return excludeStandard;
            };
            exclude.SetValue += delegate (bool value) {
                excludeStandard = value;
                ModPrefs.SetBool(Plugin.PluginName, excludeStandardSetting, excludeStandard);
            };

            var autoPlayMenu = subMenu.AddBool("Auto Play");
            autoPlayMenu.GetValue += delegate {
                return autoPlay;
            };
            autoPlayMenu.SetValue += delegate (bool value) {
                autoPlay = value;
                ModPrefs.SetBool(Plugin.PluginName, autoPlaySetting, autoPlay);
            };
        }

        private float[] Difficulties()
        {
            return new float[] {
                (float)LevelDifficulty.Easy,
                (float)LevelDifficulty.Normal,
                (float)LevelDifficulty.Hard,
                (float)LevelDifficulty.Expert,
                (float)LevelDifficulty.ExpertPlus
            };
        }

        private void CreateRandomButton()
        {
            if (randomButton == null)
            {
                RectTransform navRectTransform = navController.GetComponent<RectTransform>();
                randomButton = UIHelper.CreateUIButton(navRectTransform, "PlayButton");
                UIHelper.SetButtonText(randomButton, "Random");
                UIHelper.SetButtonTextSize(randomButton, 2.5f);
                (randomButton.transform as RectTransform).anchoredPosition = pos;
                (randomButton.transform as RectTransform).sizeDelta = new Vector2(20, 5f);
                randomButton.onClick.AddListener(new UnityAction(PlayRandomSong));
            }

            randomButton.interactable = false;
        }

        IEnumerator TestRandomSong()
        {
            for (int i = 0; i < 20; i++)
            {
                RandomSong();
                yield return null;
            }
        }

        private void Update()
        {
            if (randomButton != null && (isShowing != randomButton.gameObject.activeInHierarchy))
            {
                isShowing = randomButton.gameObject.activeInHierarchy;
                if (!isShowing)
                {
                    randomButton.interactable = false;
                }
                else
                {
                    allowAfter = 10;
                    RandomSong();
                }
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                if (randomButton.interactable)
                {
                    PlayRandomSong();
                }
            }

            //if (Input.GetKeyDown(KeyCode.JoystickButton0)&& Input.GetKeyDown(KeyCode.JoystickButton2))
            //{
            //    StartCoroutine(TestRandomSong());
            //}

            //if (Input.GetKeyDown((KeyCode)ConInput.Vive.RightTrackpadPress))
            //{
            //    LogComponents(mainSettingsMenu.transform, "=");
            //}
            //if (Input.GetKeyDown(KeyCode.JoystickButton9))
            //{
            //    pos.x += 1f;
            //    Console.WriteLine(pos.x);
            //}
            //if (Input.GetKeyDown(KeyCode.JoystickButton8))
            //{
            //    pos.x -= 1f;
            //    Console.WriteLine(pos.x);
            //}
            //if (Input.GetKeyDown(KeyCode.JoystickButton0))
            //{
            //    pos.y += 1f;
            //    Console.WriteLine(pos.y);
            //}
            //if (Input.GetKeyDown(KeyCode.JoystickButton2))
            //{
            //    pos.y -= 1f;
            //    Console.WriteLine(pos.y);
            //}
            //if (randomButton != null)
            //{
            //    (randomButton.transform as RectTransform).anchoredPosition = pos;
            //}
        }

        private List<IStandardLevel> SongsForDifficulty()
        {
            var levels = listViewController.GetPrivateField<IStandardLevel[]>("_levels").Where(x => x.HasDifficultyInRange(minDiff, maxDiff));

            if (excludeStandard)
            {
                levels = levels.Where(x => x.levelID.Length > 32);
            }
            return levels.ToList();
        }

        private void AddToQueue(IStandardLevel played)
        {
            pastSongs.Enqueue(played);
            int numSongs = SongsForDifficulty().Count;
            if (allowAfter > numSongs)
            {
                allowAfter = numSongs - 2;
                if (allowAfter < 0)
                {
                    allowAfter = 0;
                }
            }
            while (pastSongs.Count > allowAfter)
            {
                pastSongs.Dequeue();
            }
        }

        private void RandomSong()
        {
            level = null;
            IStandardLevel song = null;

            var levels = SongsForDifficulty();

            var vaildSongCount = levels.Count;
            if (vaildSongCount != 0)
            {
                do
                {
                    int rand = UnityEngine.Random.Range(0, vaildSongCount);
                    song = levels[rand];
                }
                while (pastSongs.Contains(song));
            }
            level = song;

            if (level != null)
            {
                randomButton.interactable = true;
            }
            else
            {
                randomButton.interactable = false;
            }
        }

        IEnumerator SelectAndLoadSong(IStandardLevel level)
        {
            AddToQueue(level);

            var diff = minDiff;
            IStandardLevelDifficultyBeatmap difficultyLevel = null;
            do
            {
                diff = (LevelDifficulty)UnityEngine.Random.Range((int)minDiff, (int)maxDiff + 1);
                difficultyLevel = level.GetDifficultyLevel(diff);
            } while (difficultyLevel == null);

            if (autoPlay)
            {
                // Fade screen away to not spoil song
                var fade = Resources.FindObjectsOfTypeAll<FadeOutOnGameEvent>().FirstOrDefault();
                fade.HandleGameEvent(0.7f);
                // Turn preview down
                player.volume = 0;
                yield return new WaitForSeconds(1.0f);
            }

            int row = listTableView.RowNumberForLevelID(level.levelID);
            tableView.SelectRow(row, true);
            tableView.ScrollToRow(row, false);

            difficultyViewController.SetDifficultyLevels(level.difficultyBeatmaps, difficultyLevel);

            var gameplayMode = detailViewController.gameplayMode;
            var gameplayOptions = detailViewController.gameplayOptions;
            detailViewController.SetContent(difficultyLevel, gameplayMode);

            if (autoPlay)
            {
                detailViewController.PlayButtonPressed();

                if (gameplayMode.IsSolo() && !gameplayOptions.validForScoreUse)
                {
                    var prompt = flowController.GetPrivateField<SimpleDialogPromptViewController>("_simpleDialogPromptViewController");
                    yield return new WaitForSeconds(0.1f);
                    flowController.HandleSimpleDialogPromptViewControllerDidFinish(prompt, true);
                }
            }
        }

        private void PlayRandomSong()
        {
            RandomSong();

            if (level != null)
            {
                StartCoroutine(SelectAndLoadSong(level));
            }
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

    public static class LevelExtenstion
    {
        public static bool HasDifficultyInRange(this IStandardLevel level, LevelDifficulty min, LevelDifficulty max)
        {
            bool hasDiff = false;
            for (int i = (int)min; i <= (int)max; i++)
            {
                if (level.GetDifficultyLevel((LevelDifficulty)i) != null)
                {
                    hasDiff = true;
                }
            }
            return hasDiff;
        }
    }
}
