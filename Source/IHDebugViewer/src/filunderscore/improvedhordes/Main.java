package filunderscore.improvedhordes;

import java.io.DataInput;
import java.net.Socket;

import com.google.common.io.LittleEndianDataInputStream;

import filunderscore.improvedhordes.gui.ImprovedHordesFrame;
import filunderscore.improvedhordes.util.Vector2i;
import filunderscore.improvedhordes.world.ImprovedHordesSimulation;
import filunderscore.improvedhordes.world.ImprovedHordesSimulation.ConnectionStatus;

public class Main 
{
	private static final Vector2i SIZE = new Vector2i(1000, 1000);
	
	public static void main(String[] args) throws Exception
	{
		ImprovedHordesSimulation simulation = new ImprovedHordesSimulation();
		ImprovedHordesFrame frame = new ImprovedHordesFrame(SIZE, simulation);
		
		simulation.setFrame(frame);

		while(true)
		{
			if(simulation.status != ConnectionStatus.RECONNECTING)
			{
				Thread.sleep(1000);
				continue;
			}
			
			try
			{
				Socket socket = new Socket("127.0.0.1", 9000);
				System.out.println("Connected");

				simulation.status = ConnectionStatus.CONNECTED;
				
				DataInput in = new LittleEndianDataInputStream(socket.getInputStream());
				
				while(true)
				{
					simulation.read(in);
				}
			}
			catch(Exception e)
			{
				simulation.status = ConnectionStatus.LOST_CONNECTION;
				frame.update();
			}
		}
	}
}