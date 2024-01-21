package filunderscore.improvedhordes;

import java.awt.image.BufferedImage;
import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.DataInput;
import java.net.Socket;
import java.util.zip.GZIPInputStream;

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
			
			try(Socket socket = new Socket("127.0.0.1", 9000))
			{
				socket.setTcpNoDelay(true);
				
				System.out.println("Connected");

				simulation.status = ConnectionStatus.CONNECTED;
				
				DataInput in = new LittleEndianDataInputStream(socket.getInputStream());
				
				int biomesImageWidth = in.readInt();
				int biomesImageHeight = in.readInt();
				
				int biomesImageSize = in.readInt();
				System.out.println("Received compressed biomes.png image: " + biomesImageSize);
				
				byte[] biomesImageCompressedData = new byte[biomesImageSize];				
				in.readFully(biomesImageCompressedData);
				
				byte[] biomesImageData = new byte[0];
				
				try(ByteArrayInputStream bin = new ByteArrayInputStream(biomesImageCompressedData))
				{
					try(GZIPInputStream gin = new GZIPInputStream(bin))
					{
						try(ByteArrayOutputStream out = new ByteArrayOutputStream())
						{
							out.write(gin.readAllBytes());
							biomesImageData = out.toByteArray();
						}
					}
				}
				
				BufferedImage biomesImage = new BufferedImage(biomesImageWidth, biomesImageHeight, BufferedImage.TYPE_INT_RGB);
				
				for(int y = 0; y < biomesImageHeight; y++)
				{
					for(int x = 0; x < biomesImageWidth * 3; x += 3)
					{
						byte r = biomesImageData[y * (biomesImageWidth * 3) + x];
						byte g = biomesImageData[y * (biomesImageWidth * 3) + x + 1];
						byte b = biomesImageData[y * (biomesImageWidth * 3) + x + 2];
						
						int rgb = (r << 16) | (g << 8) | b;
						
						// set pixels
						biomesImage.setRGB(x / 3, y, rgb);
					}
				}
				
				simulation.biomesImage = biomesImage;
				
				while(true)
				{
					simulation.read(in);
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
}