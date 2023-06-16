package filunderscore.improvedhordes.world;

import java.io.DataInput;
import java.io.IOException;
import java.nio.charset.StandardCharsets;

import filunderscore.improvedhordes.gui.ImprovedHordesFrame;
import filunderscore.improvedhordes.util.Vector2i;
import filunderscore.improvedhordes.util.Vector3;

public class ImprovedHordesSimulation 
{
	public enum ConnectionStatus
	{
		NOT_CONNECTED,
		LOST_CONNECTION,
		CONNECTED,
		RECONNECTING;
	}
	
	private ImprovedHordesFrame frame;
	public ConnectionStatus status = ConnectionStatus.NOT_CONNECTED;
	public WorldHordeState world;
	
	public void setFrame(ImprovedHordesFrame frame)
	{
		this.frame = frame;
	}
	
	public void initPanel()
	{
		this.frame.setSizes();
	}
	
	public void read(DataInput in) throws IOException
	{
		int worldSize = in.readInt();

		Vector2i worldSizeV = new Vector2i(worldSize, worldSize);
		WorldHordeState world = new WorldHordeState(worldSizeV);
		
		int playerSize = in.readInt();
		
		for(int i = 0; i < playerSize; i++)
		{
			world.getPlayers().add(new PlayerSnapshot(new Vector3(in.readFloat(), in.readFloat(), in.readFloat()), in.readInt(), readString(in)));
		}
		
		int clusterSize = in.readInt();
		
		for(int i = 0; i < clusterSize; i++)
		{
			String type = readString(in);
			int count = in.readInt();
			
			for(int j = 0; j < count; j++)
			{
				world.getClusters().add(new ClusterSnapshot(type, new Vector3(in.readFloat(), in.readFloat(), in.readFloat()), in.readFloat()));
			}
		}
		
		int zoneSize = in.readInt();
		
		for(int i = 0; i < zoneSize; i++)
		{
			Vector2i position = new Vector2i(in.readInt(), in.readInt());
			Vector2i size = new Vector2i(in.readInt(), in.readInt());
			float density = in.readFloat();
			int count = in.readInt();
			float zoneDistanceAvg = in.readFloat();
			float avgWeight = in.readFloat();
			
			world.getZones().add(new POI(position, size, density, count, zoneDistanceAvg, avgWeight));
		}
		
		this.world = world;
		this.initPanel();
		
		if(this.frame != null)
			this.frame.update();
	}
	
	private String readString(DataInput in) throws IOException
	{
		boolean valid = in.readBoolean();
		
		if(valid)
		{
			int length = in.readInt();
			byte[] b = new byte[length];
			
			for(int i = 0; i < length; i++)
			{
				b[i] = in.readByte();
			}
			
			return new String(b, StandardCharsets.UTF_8);
		}
		else
		{
			return "null";
		}
	}
}