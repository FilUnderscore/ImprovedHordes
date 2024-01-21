package filunderscore.improvedhordes.common.packet.login;

import java.awt.image.BufferedImage;
import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.util.zip.GZIPInputStream;

import filunderscore.improvedhordes.common.packet.Packet;
import filunderscore.improvedhordes.common.packet.PacketDataInput;
import filunderscore.improvedhordes.common.packet.Packets;
import filunderscore.improvedhordes.world.ImprovedHordesSimulation;

public final class BiomesPacket extends Packet
{
	public BiomesPacket()
	{
		super(Packets.BIOMES);
	}

	@Override
	protected final void onReceived(ImprovedHordesSimulation simulation, PacketDataInput in) throws IOException
	{
		int biomesImageWidth = in.readInt();
		int biomesImageHeight = in.readInt();
		
		byte[] biomesImageCompressedData = in.readBytes();
		System.out.println("Received compressed biomes.png image: " + biomesImageCompressedData.length);
		
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
		
		simulation.getWorld().setBiomesImage(biomesImage);
	}
}