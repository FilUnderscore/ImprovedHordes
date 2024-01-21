package filunderscore.improvedhordes.common.packet.game;

import java.io.IOException;

import filunderscore.improvedhordes.common.packet.Packet;
import filunderscore.improvedhordes.common.packet.PacketDataInput;
import filunderscore.improvedhordes.common.packet.Packets;
import filunderscore.improvedhordes.util.Vector3;
import filunderscore.improvedhordes.world.ClusterSnapshot;
import filunderscore.improvedhordes.world.ImprovedHordesSimulation;

public final class ClustersPacket extends Packet
{
	public ClustersPacket()
	{
		super(Packets.CLUSTERS);
	}

	@Override
	protected final void onReceived(ImprovedHordesSimulation simulation, PacketDataInput in) throws IOException
	{
		simulation.getWorld().getClusters().clear();
		
		in.readList(() ->
		{
			String type = in.readString();
			
			in.readList(() ->
			{
				Vector3 position = in.readVector3();
				float density = in.readFloat();
				
				simulation.getWorld().getClusters().add(new ClusterSnapshot(type, position, density));
			});
		});
	}
}