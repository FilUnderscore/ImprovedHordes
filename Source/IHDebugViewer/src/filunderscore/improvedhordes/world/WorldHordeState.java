package filunderscore.improvedhordes.world;

import java.awt.Color;
import java.awt.Graphics;
import java.awt.image.BufferedImage;
import java.util.ArrayList;
import java.util.List;

import filunderscore.improvedhordes.gui.IHRenderer;
import filunderscore.improvedhordes.gui.IHRenderer.ScaledVector;
import filunderscore.improvedhordes.util.Vector2i;

public final class WorldHordeState
{
	private final Vector2i worldSize;
	
	private final List<PlayerSnapshot> players;
	private final List<ClusterSnapshot> clusters;
	private final List<POI> zones;
	
	private final BufferedImage biomesImage;
	
	public WorldHordeState(Vector2i worldSize, BufferedImage biomesImage)
	{
		this.worldSize = worldSize;
		
		this.players = new ArrayList<>();
		this.clusters = new ArrayList<>();
		this.zones = new ArrayList<>();
		
		this.biomesImage = biomesImage;
	}
	
	public Vector2i GetWorldSize()
	{
		return this.worldSize;
	}
	
	public List<PlayerSnapshot> getPlayers()
	{
		return this.players;
	}
	
	public List<ClusterSnapshot> getClusters()
	{
		return this.clusters;
	}
	
	public List<POI> getZones()
	{
		return this.zones;
	}
	
	public void draw(IHRenderer renderer, Graphics g)
	{
		this.drawBiomes(renderer, g);
		this.drawAxis(renderer, g);
		this.drawChunks(renderer, g);
		
		for(ClusterSnapshot cluster : getClusters())
		{
			cluster.draw(this, renderer, g);
		}
		
		for(PlayerSnapshot player : getPlayers())
		{
			player.draw(this, renderer, g);
		}
		
		for(POI zone : getZones())
		{
			zone.draw(this, renderer, g);
		}
	}
	
	private void drawBiomes(IHRenderer renderer, Graphics g)
	{
		g.drawImage(this.biomesImage, 0, 0, renderer.getPanelSize().x, renderer.getPanelSize().y, null);
	}
	
	private void drawAxis(IHRenderer renderer, Graphics g)
	{
		Vector2i rescaledAxisThickness = renderer.rescaleBlocksToScreen(32);
		
		g.setColor(Color.white);
		g.drawRect(0, 0, renderer.getPanelSize().x, renderer.getPanelSize().y);
		g.fillRect(4, renderer.getPanelSize().y / 2 - rescaledAxisThickness.x, renderer.getPanelSize().x, rescaledAxisThickness.x);
		g.fillRect(renderer.getPanelSize().x / 2 - rescaledAxisThickness.x, rescaledAxisThickness.x, rescaledAxisThickness.x, renderer.getPanelSize().y);
	}
	
	private void drawChunks(IHRenderer renderer, Graphics g)
	{
		g.setColor(new Color(0, 102, 0, 255));
		
		// Draw chunks.
		int blockSize = 100;
		Vector2i rescaledBlocks = renderer.rescaleBlocksToScreen(blockSize);
	
		for(int x = -worldSize.x / 2; x <= worldSize.x / 2; x += blockSize)
		{
			for(int y = -worldSize.y / 2; y <= worldSize.y / 2; y += blockSize)
			{
				ScaledVector rescaled = renderer.rescale(new Vector2i(x, y), new Vector2i(1, 1));
				
				if(rescaled.inView())
				{
					g.drawLine(rescaled.scaledPosition.x - rescaledBlocks.x, rescaled.scaledPosition.y, rescaled.scaledPosition.x + rescaledBlocks.x, rescaled.scaledPosition.y);
					g.drawLine(rescaled.scaledPosition.x, rescaled.scaledPosition.y - rescaledBlocks.y, rescaled.scaledPosition.x, rescaled.scaledPosition.y + rescaledBlocks.y);
				}
			}
		}
	}
	
	@Override
	public String toString()
	{
		return String.format("World [size: %s]", this.worldSize);
	}
}