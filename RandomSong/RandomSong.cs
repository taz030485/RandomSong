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
        public const int MainScene = 1;
        public const int GameScene = 5;

        MenuSceneSetupData _menuSceneSetupData = null;
        StandardLevelSelectionFlowCoordinator flowController = null;
        StandardLevelSelectionNavigationController navController = null;
        StandardLevelListViewController listViewController = null;
        StandardLevelDifficultyViewController difficultyViewController;
        StandardLevelListTableView listTableView = null;
        TableView tableView = null;
        StandardLevelDetailViewController _songDetailViewController = null;

        private Button randomButton;

        Queue<IStandardLevel> pastSongs = null;

        static int allowAfter = 10;
        static bool excludeStandard = false;

        static Vector2 pos = new Vector2(0.0f, 0.0f);

        LevelDifficulty currentDiff = LevelDifficulty.Expert;

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

                pastSongs = new Queue<IStandardLevel>(20);
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
                flowController = Resources.FindObjectsOfTypeAll<StandardLevelSelectionFlowCoordinator>().FirstOrDefault();
                navController = flowController.GetPrivateField<StandardLevelSelectionNavigationController>("_levelSelectionNavigationController");
                listViewController = flowController.GetPrivateField<StandardLevelListViewController>("_levelListViewController");
                difficultyViewController = flowController.GetPrivateField<StandardLevelDifficultyViewController>("_levelDifficultyViewController");
                listTableView = listViewController.GetPrivateField<StandardLevelListTableView>("_levelListTableView");
                tableView = listTableView.GetPrivateField<TableView>("_tableView");
                _menuSceneSetupData = flowController.GetPrivateField<MenuSceneSetupData>("_menuSceneSetupData");
                CreateRandomButton();
            }
        }

        private void CreateRandomButton()
        {
            if (randomButton == null)
            {
                //if (_songDetailViewController == null)
                //{
                //    _songDetailViewController = ReflectionUtil.GetPrivateField<StandardLevelDetailViewController>(flowController, "_levelDetailViewController");
                //}
                RectTransform detailsRectTransform = navController.GetComponent<RectTransform>();
                randomButton = UIHelper.CreateUIButton(detailsRectTransform, "PlayButton");
                UIHelper.SetButtonText(randomButton, "Random");
                UIHelper.SetButtonTextSize(randomButton, 3f);
                (randomButton.transform as RectTransform).anchoredPosition = pos;
                (randomButton.transform as RectTransform).sizeDelta = new Vector2(30f, 6f);
                randomButton.onClick.AddListener(new UnityAction(PlayRandomSong));
            }
        }

        private void Update()
        {
            //if (Input.GetKeyDown((KeyCode)ConInput.Vive.RightTrackpadPress))
            //{
            //    LogComponents(mainSettingsMenu.transform, "=");
            //}
            if (Input.GetKeyDown(KeyCode.JoystickButton9))
            {
                pos.x += 1f;
                Console.WriteLine(pos.x);
            }
            if (Input.GetKeyDown(KeyCode.JoystickButton8))
            {
                pos.x -= 1f;
                Console.WriteLine(pos.x);
            }
            if (Input.GetKeyDown(KeyCode.JoystickButton0))
            {
                pos.y += 1f;
                Console.WriteLine(pos.y);
            }
            if (Input.GetKeyDown(KeyCode.JoystickButton2))
            {
                pos.y -= 1f;
                Console.WriteLine(pos.y);
            }
            if (randomButton != null)
            {
                (randomButton.transform as RectTransform).anchoredPosition = pos;
            }
        }

        private List<IStandardLevel> SongsForDifficulty(LevelDifficulty diff)
        {
            var levels = listViewController.GetPrivateField<IStandardLevel[]>("_levels");
            return levels.Where(x => x.GetDifficultyLevel(diff) != null).ToList();
        }

        private void AddToQueue(IStandardLevel played)
        {
            pastSongs.Enqueue(played);
            int numSongs = SongsForDifficulty(currentDiff).Count;
            if (allowAfter > numSongs)
            {
                allowAfter = numSongs - 2;
                if (allowAfter < 0)
                {
                    allowAfter = 0;
                }
            }
            if (pastSongs.Count > allowAfter)
            {
                pastSongs.Dequeue();
            }
        }

        private IStandardLevel RandomSong()
        {
            var levels = SongsForDifficulty(currentDiff);

            IStandardLevel song = null;
            do
            {
                int rand = UnityEngine.Random.Range(0, levels.Count);
                song = levels[rand];
            }
            while (pastSongs.Contains(song) || (excludeStandard && song.levelID.Length < 32));

            return song;
        }

        private void PlayRandomSong()
        {
            // Is this instant?
            var fade = Resources.FindObjectsOfTypeAll<FadeOutOnGameEvent>().FirstOrDefault();
            fade.HandleGameEvent(0.0f);
            // Also turn preview down

            var _gameplayMode = _songDetailViewController.gameplayMode;
            var _gameplayOptions = _songDetailViewController.gameplayOptions;
            var level = RandomSong();
            Console.WriteLine(level.songName + " " + _gameplayMode + " " + gameplayOptions);

            int row = listTableView.RowNumberForLevelID(level.levelID);
            tableView.SelectRow(row, true);
            tableView.ScrollToRow(row, false);
            var difficultyLevel = level.GetDifficultyLevel(currentDiff);
            difficultyViewController.SetDifficultyLevels(level.difficultyBeatmaps, difficultyLevel);
            _songDetailViewController.SetContent(difficultyLevel, _gameplayMode);
            _songDetailViewController.PlayButtonPressed();
        }

        public GameplayOptions gameplayOptions
        {
            get
            {
                return PersistentSingleton<GameDataModel>.instance.gameDynamicData.GetCurrentPlayerDynamicData().gameplayOptions;
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
}
