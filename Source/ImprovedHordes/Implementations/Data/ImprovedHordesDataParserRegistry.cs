using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.World.Event;
using ImprovedHordes.Core.World.Horde;
using ImprovedHordes.Core.World.Horde.Characteristics;
using ImprovedHordes.Core.World.Horde.Cluster;
using ImprovedHordes.Core.World.Horde.Spawn;
using ImprovedHordes.Implementations.Data.Parsers;
using ImprovedHordes.Implementations.Data.Parsers.Horde;
using ImprovedHordes.Implementations.Data.Parsers.POI;
using ImprovedHordes.POI;
using ImprovedHordes.Screamer;
using ImprovedHordes.Wandering.Enemy.Wilderness;
using ImprovedHordes.Wandering.Enemy.Zone;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ImprovedHordes.Implementations.Data
{
    public sealed class ImprovedHordesDataParserRegistry : IDataParserRegistry
    {
        private readonly Dictionary<Type, IDataParser> parsers = new Dictionary<Type, IDataParser>();

        public ImprovedHordesDataParserRegistry(IRandomFactory<IWorldRandom> randomFactory, WorldPOIScanner poiScanner, WorldEventReporter worldEventReporter, global::World world)
        {
            // Default data parser for types.
            this.RegisterDataParser<Type>(new TypeDataParser());
            this.RegisterDataParser<float>(new FloatDataParser());
            this.RegisterDataParser<uint>(new UIntDataParser());
            this.RegisterDataParser<ulong>(new ULongDataParser());
            this.RegisterDataParser<ushort>(new UShortDataParser());
            this.RegisterDataParser<Vector2i>(new Vector2iDataParser());
            this.RegisterDataParser<Vector3>(new Vector3DataParser());

            this.RegisterDataParser<HordeSpawnParams>(new HordeSpawnParamsParser());
            this.RegisterDataParser<BiomeDefinition>(new BiomeDefinitionDataParser(world));
            this.RegisterDataParser<HordeSpawnData>(new HordeSpawnDataParser());

            this.RegisterDataParser<HordeCluster>(new HordeClusterDataParser());
            this.RegisterDataParser<WorldHorde>(new WorldHordeDataParser(randomFactory));

            this.RegisterDataParser<List<HordeCluster>>(new ListDataParser<HordeCluster>());
            this.RegisterDataParser<List<WorldHorde>>(new ListDataParser<WorldHorde>());

            this.RegisterDataParser<WalkSpeedHordeCharacteristic>(new ParameterizedConstructorRuntimeDataParser<WalkSpeedHordeCharacteristic>((loader) => new WalkSpeedHordeCharacteristic(loader.Load<float>(), loader.Load<float>()), (characteristic, saver) => characteristic.Save(saver)));
            this.RegisterDataParser<SensitivityHordeCharacteristic>(new ParameterizedConstructorRuntimeDataParser<SensitivityHordeCharacteristic>((loader) => new SensitivityHordeCharacteristic(loader.Load<float>()), (characteristic, saver) => characteristic.Save(saver)));

            this.RegisterDataParser<List<IHordeCharacteristic>>(new ListDataParser<IHordeCharacteristic>());

            this.RegisterDataParser<WorldPOIScanner.POIZone>(new POIZoneDataParser(poiScanner));

            this.RegisterDataParser<WorldZoneWanderingEnemyAICommandGenerator>(new ParameterizedConstructorRuntimeDataParser<WorldZoneWanderingEnemyAICommandGenerator>((loader) => new WorldZoneWanderingEnemyAICommandGenerator(poiScanner, loader.Load<WorldPOIScanner.POIZone>())));
            this.RegisterDataParser<WorldWildernessWanderingEnemyAICommandGenerator>(new ParameterizedConstructorRuntimeDataParser<WorldWildernessWanderingEnemyAICommandGenerator>((loader) => new WorldWildernessWanderingEnemyAICommandGenerator(poiScanner)));
            this.RegisterDataParser<WorldZoneScreamerAICommandGenerator>(new ParameterizedConstructorRuntimeDataParser<WorldZoneScreamerAICommandGenerator>((loader) => new WorldZoneScreamerAICommandGenerator(loader.Load<WorldPOIScanner.POIZone>()), (screamerCommandGenerator, saver) => saver.Save<WorldPOIScanner.POIZone>(screamerCommandGenerator.GetState().GetPOIZone())));

            this.RegisterDataParser<ScreamerEntityAICommandGenerator>(new ParameterizedConstructorRuntimeDataParser<ScreamerEntityAICommandGenerator>((loader) => new ScreamerEntityAICommandGenerator(worldEventReporter)));

            this.RegisterDataParser<Dictionary<Vector2i, ulong>>(new DictionaryTypeParser<Vector2i, ulong>());
            this.RegisterDataParser<Dictionary<WorldPOIScanner.POIZone, ulong>>(new DictionaryTypeParser<WorldPOIScanner.POIZone, ulong>());
        }

        public IDataParser<T> GetDataParser<T>()
        {
            if(!this.parsers.TryGetValue(typeof(T), out var dataParser) || !(dataParser is IDataParser<T> typeDataParser))
            {
                return null;
            }

            return typeDataParser;
        }

        public IRuntimeDataParser GetRuntimeDataParser(Type type)
        {
            if (!this.parsers.TryGetValue(type, out var dataParser) || !(dataParser is IRuntimeDataParser runtimeDataParser))
                return null;

            return runtimeDataParser;
        }

        public void RegisterDataParser<T>(IDataParser dataParser)
        {
            this.parsers.Add(typeof(T), dataParser);
        }
    }
}
