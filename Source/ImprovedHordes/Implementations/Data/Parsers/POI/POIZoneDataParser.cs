using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.POI;
using ImprovedHordes.POI.Prefab;
using System.IO;

namespace ImprovedHordes.Implementations.Data.Parsers.POI
{
    public sealed class POIZoneDataParser : IDataParser<PrefabPOIZone>
    {
        private readonly WorldPrefabPOIScanner worldPOIScanner;

        public POIZoneDataParser(WorldPrefabPOIScanner poiScanner)
        {
            this.worldPOIScanner = poiScanner;
        }

        public PrefabPOIZone Load(IDataLoader loader, BinaryReader reader)
        {
            return this.worldPOIScanner.GetAllZones()[reader.ReadInt32()];
        }

        public void Save(IDataSaver saver, BinaryWriter writer, PrefabPOIZone obj)
        {
            writer.Write(this.worldPOIScanner.GetAllZones().IndexOf(obj));
        }
    }
}
