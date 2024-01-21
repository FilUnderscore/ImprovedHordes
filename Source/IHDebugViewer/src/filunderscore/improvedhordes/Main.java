package filunderscore.improvedhordes;

import java.io.ByteArrayInputStream;
import java.io.DataInput;
import java.net.Socket;

import com.google.common.io.LittleEndianDataInputStream;

import filunderscore.improvedhordes.common.packet.Packet;
import filunderscore.improvedhordes.common.packet.PacketDataInput;
import filunderscore.improvedhordes.common.packet.Packets;
import filunderscore.improvedhordes.gui.ImprovedHordesFrame;
import filunderscore.improvedhordes.util.Vector2i;
import filunderscore.improvedhordes.world.ImprovedHordesSimulation;
import filunderscore.improvedhordes.world.ImprovedHordesSimulation.ConnectionStatus;

public class Main 
{
	private static final Vector2i SIZE = new Vector2i(1000, 1000);
	
	public static void main(String[] args)
	{
		Packets.register();
		
		ImprovedHordesSimulation simulation = new ImprovedHordesSimulation();
		ImprovedHordesFrame frame = new ImprovedHordesFrame(SIZE, simulation);
		
		simulation.setFrame(frame);

		new Thread(new Runnable()
		{
			@Override
			public void run()
			{
				while(true)
				{
					if(simulation.status != ConnectionStatus.RECONNECTING)
					{
						try 
						{
							Thread.sleep(1000);
						} 
						catch (InterruptedException e) 
						{
							e.printStackTrace();
						}
						
						continue;
					}
					
					try(Socket socket = new Socket("127.0.0.1", 9000))
					{
						Logger.log("Connected to %s", socket.getInetAddress().getHostAddress());
						socket.setTcpNoDelay(true);

						simulation.status = ConnectionStatus.CONNECTED;
						
						DataInput in = new LittleEndianDataInputStream(socket.getInputStream());
						
						while(true)
						{
							short packetId = in.readShort();					
							int packetLength = in.readInt();

							if(simulation.lock.tryLock())
							{
								byte[] packetData = new byte[packetLength];
	
								if(packetLength > 0)
								{
									in.readFully(packetData);
								}
								
								if(Packets.INSTANCES.containsKey(packetId))
								{
									Packet packet = Packets.INSTANCES.get(packetId);
									
									PacketDataInput packetIn = new PacketDataInput(new LittleEndianDataInputStream(new ByteArrayInputStream(packetData)));
									packet.receive(simulation, packetIn);
								}
								else
								{
									Logger.error("Received unrecognized packet (ID %d) with length %s. Discarding.", packetId, packetLength);
								}

								simulation.lock.unlock();
							}
							else
							{
								in.skipBytes(packetLength);
							}
						}
					}
					catch(Exception e)
					{
						e.printStackTrace();
						
						simulation.status = ConnectionStatus.LOST_CONNECTION;
						frame.update();
					}
				}
			}
		}).start();
	}
}