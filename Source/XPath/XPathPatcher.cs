using System;
using System.Collections;
using System.IO;
using System.Text;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes.XPath
{
    public class XPathPatcher
    {
        public static void LoadAndPatchXMLFile(Mod instance, string modPath, string directory, string fileName, Action<XmlFile> callback)
        {
            ThreadManager.RunCoroutineSync(LoadAndPatchFile(instance, modPath, directory, fileName, callback));
        }

        private static IEnumerator LoadAndPatchFile(Mod instance, string modPath, string directory, string fileName, Action<XmlFile> callback)
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
                Error($"Loading XML {fileName} in {directory} failed: {xmlLoadException}");
            }
            else
            {
                MicroStopwatch msw = new MicroStopwatch(true);
                foreach(Mod loadedMod in ModManager.GetLoadedMods())
                {
                    if (loadedMod == instance)
                        continue;

                    string path = loadedMod.Path + "/" + directory + "/" + fileName;

                    if(File.Exists(path))
                    {
                        try
                        {
                            string text = File.ReadAllText(path, Encoding.UTF8);
                            XmlFile patchXml;

                            try
                            {
                                patchXml = new XmlFile(text, loadedMod.Path + "/" + directory, fileName, true);
                            }
                            catch(Exception ex)
                            {
                                Error($"Loading XML patch file {file.Directory}/{file.Filename} from mod {loadedMod.Name} failed: {ex}");
                                continue;
                            }

                            XmlPatcher.PatchXml(file, patchXml, loadedMod.Name);

                            Log("Patched XML from mod " + loadedMod.Name);
                        }
                        catch(Exception ex)
                        {
                            Error($"Patching {file.Directory}/{file.Filename} from mod {loadedMod.Name} failed: {ex}");
                        }

                        if(msw.ElapsedMicroseconds > 50L)
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
