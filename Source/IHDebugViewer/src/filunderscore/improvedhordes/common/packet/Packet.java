package filunderscore.improvedhordes.common.packet;

import java.io.EOFException;
import java.io.IOException;

import filunderscore.improvedhordes.Logger;
import filunderscore.improvedhordes.world.ImprovedHordesSimulation;

public abstract class Packet
{
	private final short id;
	
	public Packet(short id)
	{
		this.id = id;
	}
	
	public final short getId()
	{
		return this.id;
	}
	
	public final void receive(ImprovedHordesSimulation simulation, PacketDataInput in)
	{
		try
		{
			this.onReceived(simulation, in);
		}
		catch(EOFException e)
		{
			Logger.error("Attempted to read more data than received.");
		}
		catch(IOException e)
		{
			Logger.error("Failed to read packet %s from server.", this.getClass().getSimpleName());
			e.printStackTrace();
		}
	}
	
	protected abstract void onReceived(ImprovedHordesSimulation simulation, PacketDataInput in) throws IOException;
}