using System;
using System.Collections.Generic;

namespace ImprovedHordes.Core.Abstractions.Settings
{
    public abstract class Setting
    {
        private static readonly List<Setting> instances = new List<Setting>();

        public event EventHandler OnSettingUpdated;

        public Setting()
        {
            instances.Add(this);
        }

        public abstract void Load(ISettingLoader loader);

        public static void LoadAll(ISettingLoader loader)
        {
            foreach(var setting in instances)
            {
                setting.Load(loader);

                if (setting.OnSettingUpdated != null)
                    setting.OnSettingUpdated(setting, EventArgs.Empty);
            }
        }
    }

    public sealed class Setting<T> : Setting
    {
        private readonly string path;
        private T value;

        public T Value
        {
            get
            {
                return this.value;
            }
        }

        public Setting(string path, T defaultValue) : base()
        {
            this.path = path;
            this.value = defaultValue;
        }

        public override void Load(ISettingLoader loader)
        {
            if (loader.Load<T>(this.path, out var newValue))
                this.value = newValue;
        }
    }
}
