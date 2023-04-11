using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Thread = ImprovedHordes.Source.Utils.Thread;

namespace ImprovedHordes.Source.Core.Horde.World.LOI
{
    public sealed partial class WorldLOITracker
    {
		private struct LOIArea
		{
			public LocationOfInterest locationOfInterest;
			public bool origin;

			public LOIArea(LocationOfInterest locationOfInterest, bool origin)
			{
				this.locationOfInterest = locationOfInterest;
				this.origin = origin;
			}
		}

		private class LOIAreaImpactor : Thread
		{
			private readonly List<LocationOfInterest> locations = new List<LocationOfInterest>();
			private readonly object locationsLock = new object();

			private readonly ConcurrentBag<LOIArea> locationsToNotify = new ConcurrentBag<LOIArea>();

			private LOIInterestDecayer decayer;

			public LOIAreaImpactor(LOIInterestDecayer decayer) : base("IH-LOIAreaImpactor")
			{
				this.decayer = decayer;
			}

			public bool Notify(List<LocationOfInterest> locations)
			{
				bool success = Monitor.TryEnter(locationsLock);

				if (success)
				{
					this.locations.AddRange(locations);

					Monitor.Exit(locationsLock);
				}

				return success;
			}

			public override bool OnLoop()
			{
				Monitor.Enter(locationsLock);

				if (this.locations.Count > 0)
				{
					Parallel.ForEach(this.locations, location =>
					{
                        Dictionary<Vector2i, float> nearby = GetNearbyChunks(location.GetLocation(), 3);

                        foreach (var chunk in nearby)
                        {
                            Vector2i chunkLocation = chunk.Key;
                            float strength = chunk.Value;

                            float chunkInterest = location.GetInterestLevel() * strength;
                            locationsToNotify.Add(new LOIArea(new LocationOfInterest(chunkLocation, chunkInterest, strength), false));
                        }

						locationsToNotify.Add(new LOIArea(location, true));
                    });

					this.locations.Clear();
					this.decayer.Notify(locationsToNotify);
				}

				Monitor.Exit(locationsLock);

				return true;
			}

			private Dictionary<Vector2i, float> GetNearbyChunks(Vector3 position, int radius)
			{
				int radiusSquared = radius * radius;
				Dictionary<Vector2i, float> nearbyChunks = new Dictionary<Vector2i, float>();

				Vector2i currentChunk = global::World.toChunkXZ(position);

				for (int x = 1; x <= radiusSquared; x++)
				{
					float xDivRad = (float)(x / radius) / (float)radius;
					float strengthX = 1f - xDivRad;

					for (int y = 1; y <= radiusSquared; y++)
					{
						float yDivRad = (float)(y / radius) / (float)radius;
						float strengthY = 1f - yDivRad;

						float strength = (strengthX + strengthY) / 2f;

						nearbyChunks.Add(new Vector2i(currentChunk.x + x, currentChunk.y + y), strength);
						nearbyChunks.Add(new Vector2i(currentChunk.x - x, currentChunk.y - y), strength);
						nearbyChunks.Add(new Vector2i(currentChunk.x + x, currentChunk.y - y), strength);
						nearbyChunks.Add(new Vector2i(currentChunk.x - x, currentChunk.y + y), strength);
					}

					nearbyChunks.Add(new Vector2i(currentChunk.x + x, currentChunk.y), strengthX);
					nearbyChunks.Add(new Vector2i(currentChunk.x - x, currentChunk.y), strengthX);
					nearbyChunks.Add(new Vector2i(currentChunk.x, currentChunk.y + x), strengthX);
					nearbyChunks.Add(new Vector2i(currentChunk.x, currentChunk.y - x), strengthX);
				}

				return nearbyChunks;
			}
		}

	}
}
