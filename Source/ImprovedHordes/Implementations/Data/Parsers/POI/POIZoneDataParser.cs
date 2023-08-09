using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.POI;
using System.IO;

namespace ImprovedHordes.Implementations.Data.Parsers.POI
{
    public sealed class POIZoneDataParser : IDataParser<WorldPOIScanner.POIZone>
    {
        private readonly WorldPOIScanner worldPOIScanner;

        public POIZoneDataParser(WorldPOIScanner poiScanner)
        {
            this.worldPOIScanner = poiScanner;
        }

        public WorldPOIScanner.POIZone Load(IDataLoader loader, BinaryReader reader)
        {
            return this.worldPOIScanner.GetAllZones()[reader.ReadInt32()];
        }

        public void Save(IDataSaver saver, BinaryWriter writer, WorldPOIScanner.POIZone obj)
        {
            writer.Write(this.worldPOIScanner.GetAllZones().IndexOf(obj));
        }
    }
}
