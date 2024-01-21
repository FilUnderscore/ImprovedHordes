package filunderscore.improvedhordes.common.packet.login;

import java.io.IOException;

import filunderscore.improvedhordes.common.packet.Packet;
import filunderscore.improvedhordes.common.packet.PacketDataInput;
import filunderscore.improvedhordes.common.packet.Packets;
import filunderscore.improvedhordes.util.Vector2i;
import filunderscore.improvedhordes.world.ImprovedHordesSimulation;
import filunderscore.improvedhordes.world.World;

public final class InitPacket extends Packet
{
	public InitPacket()
	{
		super(Packets.INIT);
	}

	@Override
	protected final void onReceived(ImprovedHordesSimulation simulation, PacketDataInput in) throws IOException
	{
		int worldSize = in.readInt();
		int viewDistance = in.readInt();
		
		simulation.setWorld(new World(new Vector2i(worldSize, worldSize), viewDistance));
	}
}