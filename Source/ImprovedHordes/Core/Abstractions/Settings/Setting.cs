using System;

namespace ImprovedHordes.Core.Abstractions.Settings
{
    public abstract class Setting
    {
        protected static ISettingLoader loader;

        public event EventHandler OnSettingUpdated;

        public abstract void Load(ISettingLoader loader);

        public static void SetLoader(ISettingLoader loader)
        {
            Setting.loader = loader;
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

            this.Load(loader);
        }

        public override void Load(ISettingLoader loader)
        {
            if (loader != null && loader.Load<T>(this.path, out var newValue))
                this.value = newValue;
        }
    }
}
