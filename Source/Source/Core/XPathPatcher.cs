using System;
using System.Collections;
using System.IO;
using System.Text;

namespace ImprovedHordes.Source.Core
{
    public class XPathPatcher
    {
        public static void LoadAndPatchXMLFile(string modPath, string directory, string fileName, Action<XmlFile> callback)
        {
            ThreadManager.RunCoroutineSync(LoadAndPatchFile(modPath, directory, fileName, callback));
        }

        private static IEnumerator LoadAndPatchFile(string modPath, string directory, string fileName, Action<XmlFile> callback)
        {
            Exception xmlLoadException = null;

            XmlFile file = new XmlFile(String.Format("{0}/{1}", modPath, directory), fileName, ex =>
            {
                if (ex == null)
                    return;

                xmlLoadException = ex;
            });

            while (!file.Loaded && xmlLoadException == null)
                yield return null;

            if (xmlLoadException != null)
            {
                Log.Error($"Loading XML {fileName} in {directory} failed: {xmlLoadException}");
            }
            else
            {
                MicroStopwatch msw = new MicroStopwatch(true);
                foreach (Mod loadedMod in ModManager.GetLoadedMods())
                {
                    if (loadedMod.ApiInstance is ImprovedHordesMod)
                        continue;

                    string path = loadedMod.Path + "/" + directory + "/" + fileName;

                    if (File.Exists(path))
                    {
                        try
                        {
                            string text = File.ReadAllText(path, Encoding.UTF8);
                            XmlFile patchXml;

                            try
                            {
                                patchXml = new XmlFile(text, loadedMod.Path + "/" + directory, fileName, true);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Loading XML patch file {file.Directory}/{file.Filename} from mod {loadedMod.ModInfo.Name} failed: {ex}");
                                continue;
                            }

                            XmlPatcher.PatchXml(file, patchXml, loadedMod.ModInfo.Name.ToString());

                            Log.Out("Patched XML from mod " + loadedMod.ModInfo.Name);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Patching {file.Directory}/{file.Filename} from mod {loadedMod.ModInfo.Name?.ToString()} failed: {ex}");
                        }

                        if (msw.ElapsedMicroseconds > 50L)
                        {
                            yield return null;
                            msw.ResetAndRestart();
                        }
                    }
                }

                callback(file);
            }
        }
    }
}
