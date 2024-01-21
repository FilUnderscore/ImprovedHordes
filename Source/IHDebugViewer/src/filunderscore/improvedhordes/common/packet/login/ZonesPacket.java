package filunderscore.improvedhordes.common.packet.login;

import java.io.IOException;

import filunderscore.improvedhordes.common.packet.Packet;
import filunderscore.improvedhordes.common.packet.PacketDataInput;
import filunderscore.improvedhordes.common.packet.Packets;
import filunderscore.improvedhordes.util.Vector2i;
import filunderscore.improvedhordes.world.ImprovedHordesSimulation;
import filunderscore.improvedhordes.world.POI;

public final class ZonesPacket extends Packet
{
	public ZonesPacket()
	{
		super(Packets.ZONES);
	}

	@Override
	protected final void onReceived(ImprovedHordesSimulation simulation, PacketDataInput in) throws IOException
	{
		in.readList(() ->
		{
			Vector2i position = in.readVector2i();
			Vector2i size = in.readVector2i();
			float density = in.readFloat();
			int count = in.readInt();
			float zoneDistanceAvg = in.readFloat();
			float avgWeight = in.readFloat();
			
			simulation.getWorld().getZones().add(new POI(position, size, density, count, zoneDistanceAvg, avgWeight));
		});
	}
}