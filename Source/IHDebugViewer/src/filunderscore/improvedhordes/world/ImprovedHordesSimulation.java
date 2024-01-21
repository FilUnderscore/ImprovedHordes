package filunderscore.improvedhordes.world;

import java.util.concurrent.locks.ReentrantLock;

import filunderscore.improvedhordes.gui.ImprovedHordesFrame;

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
	private World world;
	public ReentrantLock lock = new ReentrantLock();
	
	public void setFrame(ImprovedHordesFrame frame)
	{
		this.frame = frame;
	}
	
	public void setWorld(World world)
	{
		this.world = world;
		this.frame.setSizes();
	}
	
	public World getWorld()
	{
		return this.world;
	}
	
	public void update()
	{
		if(this.frame == null)
		{
			return;
		}
		
		this.frame.update();
	}
}