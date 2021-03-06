using System;
using System.Collections.Generic;
using System.Xml;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes
{
    public class Settings
    {
        private readonly IHSettingsNode node;

        public Settings(XmlFile file)
        {
            this.node = new IHSettingsNode(file.XmlDoc.DocumentElement);
        }

        private Settings(IHSettingsNode node)
        {
            this.node = node;
        }

        public int GetInt(string name, int defaultValue)
        {
            if (int.TryParse(this.node.GetSubnode(name).GetElement().InnerText, out int value))
                return value;

            Warning("[Settings] Failed to parse {0}. Returning default value.", name);
            return defaultValue;
        }

        public float GetFloat(string name, float defaultValue)
        {
            if (float.TryParse(this.node.GetSubnode(name).GetElement().InnerText, out float value))
                return value;

            Warning("[Settings] Failed to parse {0]. Returning default value.", name);
            return defaultValue;
        }

        public int GetInt(string name, int compareTo, bool larger, int defaultValue)
        {
            int fetchedValue = GetInt(name, defaultValue);
            int value = fetchedValue;
            
            if(!larger && value < compareTo)
            {
                value = defaultValue;
            }
            else if(larger && value > compareTo)
            {
                value = defaultValue;
            }

            if(value != fetchedValue)
                Warning("[Settings] Setting {0} cannot be {1} than {2}. Current value: {3}, setting value to default value {4}.", name, larger ? "greater" : "less", compareTo, fetchedValue, defaultValue);

            return value;
        }

        public float GetFloat(string name, float compareTo, bool larger, float defaultValue)
        {
            float fetchedValue = GetFloat(name, defaultValue);
            float value = fetchedValue;

            if (!larger && value < compareTo)
            {
                value = defaultValue;
            }
            else if (larger && value > compareTo)
            {
                value = defaultValue;
            }

            if (value != fetchedValue)
                Warning("[Settings] Setting {0} cannot be {1} than {2}. Current value: {3}, setting value to default value {4}.", name, larger ? "greater" : "less", compareTo, fetchedValue, defaultValue);

            return value;
        }

        public bool GetBool(string name, bool defaultValue)
        {
            if (bool.TryParse(this.node.GetSubnode(name).GetElement().InnerText, out bool value))
                return value;
 
            Warning("[Settings] Failed to parse {0}. Returning default value.", name);
            return defaultValue;
        }

    public IHSettingsNode GetNode(string name)
        {
            return this.node.GetSubnode(name);
        }

        public Settings GetSettings(string name)
        {
            return new Settings(this.node.GetSubnode(name));
        }
    }

    public class IHSettingsNode
    {
        private readonly XmlElement element;
        private readonly Dictionary<string, IHSettingsNode> children = new Dictionary<string, IHSettingsNode>();
    
        public IHSettingsNode(XmlElement element)
        {
            this.element = element;
            
            this.ParseChildren();
        }

        private void ParseChildren()
        {
            foreach(XmlNode node in element.ChildNodes)
            {
                if(node.NodeType == XmlNodeType.Element)
                {
                    children.Add(node.Name, new IHSettingsNode((XmlElement)node));
                }
            }
        }

        public XmlElement GetElement()
        {
            return this.element;
        }

        public IHSettingsNode GetSubnode(string nodeName)
        {
            if (!children.ContainsKey(nodeName))
                throw new NullReferenceException($"Node with name {nodeName} does not exist.");

            return children[nodeName];
        }
    }
}
