using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace ImprovedHordes.Data.XML
{
    public sealed class XmlFileParser : XmlEntry
    {
        public XmlFileParser(XmlFile file) : base(file.XmlDoc.DocumentElement)
        {
        }
    }

    public class XmlEntry
    {
        private XmlElement self;
        private List<XmlEntry> entries;

        public XmlEntry(XmlElement self)
        {
            this.self = self;
            this.entries = new List<XmlEntry>();

            this.self.ChildNodes.ToList().ForEach(node =>
            {
                if(node.NodeType == XmlNodeType.Element) 
                {
                    entries.Add(new XmlEntry((XmlElement)node));
                }
            });
        }

        public List<XmlEntry> GetEntries(string tag)
        {
            return this.entries.Where(entry => entry.self.Name.Equals(tag)).ToList();
        }

        public bool GetAttribute(string attributeName, out string attributeValue)
        {
            if(this.self.HasAttribute(attributeName))
            {
                attributeValue = this.self.GetAttribute(attributeName);
                return true;
            }

            attributeValue = null;
            return false;
        }
    }
}
