package filunderscore.improvedhordes.world;

import java.awt.Color;
import java.awt.Graphics;

import filunderscore.improvedhordes.gui.IHRenderer;
import filunderscore.improvedhordes.gui.IHRenderer.ScaledVector;
import filunderscore.improvedhordes.util.Vector2i;
import filunderscore.improvedhordes.util.Vector3;

public class PlayerSnapshot implements Drawable
{
	public Vector3 location;

	public int gamestage;
	public String biome;
	
	public PlayerSnapshot(Vector3 location, int gamestage, String biome)
	{
		this.location = location;
		this.gamestage = gamestage;
		this.biome = biome;
	}
	
	public String toString()
	{
		return String.format("Player [location: %s, gamestage: %d, biome: %s]", this.location, this.gamestage, this.biome);
	}

	@Override
	public void draw(WorldHordeState world, IHRenderer renderer, Graphics g) 
	{
		ScaledVector scaled = renderer.rescale(this.location, new Vector2i(10, 10));
		
		g.setColor(Color.yellow);
		g.fillOval((int)scaled.scaledPosition.x - scaled.scaledSize.x / 2, (int)scaled.scaledPosition.y - scaled.scaledSize.y / 2, scaled.scaledSize.x, scaled.scaledSize.y);
	}
}