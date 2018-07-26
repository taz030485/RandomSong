using System;
using System.Collections.Generic;
using System.Linq;
using IllusionPlugin;

namespace RandomSong
{
    public class Plugin : IPlugin
    {
        public string Name => "Random Song";
        public string Version => "1.0";

        private bool _init = false;

        public void OnApplicationStart()
        {
            if (_init) return;
            _init = true;

            UIHelper.OnLoad();
            RandomSongManager.OnLoad();
        }

        public void OnApplicationQuit()
        {
        }

        public void OnLevelWasLoaded(int level)
        {
        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }
    }
}
