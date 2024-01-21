package filunderscore.improvedhordes.common.packet.game;

import java.io.IOException;

import filunderscore.improvedhordes.common.packet.Packet;
import filunderscore.improvedhordes.common.packet.PacketDataInput;
import filunderscore.improvedhordes.common.packet.Packets;
import filunderscore.improvedhordes.util.Vector3;
import filunderscore.improvedhordes.world.ImprovedHordesSimulation;
import filunderscore.improvedhordes.world.PlayerSnapshot;

public final class PlayersPacket extends Packet
{
	public PlayersPacket()
	{
		super(Packets.PLAYERS);
	}

	@Override
	protected final void onReceived(ImprovedHordesSimulation simulation, PacketDataInput in) throws IOException
	{
		simulation.getWorld().getPlayers().clear();
		
		in.readList(() ->
		{
			in.readList(() ->
			{
				Vector3 position = in.readVector3();
				int gamestage = in.readInt();
				String biome = in.readString();
				
				simulation.getWorld().getPlayers().add(new PlayerSnapshot(position, gamestage, biome));
			});
		});
	}
}