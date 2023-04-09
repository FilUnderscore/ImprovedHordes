using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Thread = ImprovedHordes.Source.Utils.Thread;

namespace ImprovedHordes.Source.Core.Horde.World.LOI
{
    public sealed partial class WorldLOITracker
    {
		private class LOIAreaImpactor : Thread
		{
			private readonly Queue<LocationOfInterest> locations = new Queue<LocationOfInterest>();
			private readonly List<LocationOfInterest> locationsToNotify = new List<LocationOfInterest>();

			private LOIInterestDecayer decayer;

			public LOIAreaImpactor(LOIInterestDecayer decayer) : base("IH-LOIAreaImpactor")
			{
				this.decayer = decayer;
			}

			public bool Notify(List<LocationOfInterest> locations)
			{
				bool success = Monitor.TryEnter(this.locations);

				if (success)
				{
					foreach(var locationOfInterest in locations)
						this.locations.Enqueue(locationOfInterest);

					Monitor.Exit(this.locations);
				}

				return success;
			}

			public override bool OnLoop()
			{
				Monitor.Enter(this.locations);

				if (this.locations.Count > 0)
				{
					do
					{
						LocationOfInterest location = this.locations.Dequeue();
						Dictionary<Vector2i, float> nearby = GetNearbyChunks(location.GetLocation(), 3);

						foreach (var chunk in nearby)
						{
							Vector2i chunkLocation = chunk.Key;
							float strength = chunk.Value;

							float chunkInterest = location.GetInterestLevel() * strength;
							locationsToNotify.Add(new LocationOfInterest(chunkLocation, chunkInterest));
						}
					} while (this.locations.Count > 0);

					this.decayer.Notify(locationsToNotify);
					locationsToNotify.Clear();
				}

				Monitor.Exit(this.locations);

				return true;
			}

			private Dictionary<Vector2i, float> GetNearbyChunks(Vector3 position, int radius)
			{
				int radiusSquared = radius * radius;
				Dictionary<Vector2i, float> nearbyChunks = new Dictionary<Vector2i, float>();

				Vector2i currentChunk = global::World.toChunkXZ(position);

				nearbyChunks.Add(currentChunk, 1f);

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
