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
	public void draw(World world, IHRenderer renderer, Graphics g) 
	{
		ScaledVector scaled = renderer.rescale(this.location.toVXZ(), new Vector2i(50, 50));
		
		g.setColor(Color.yellow);
		g.fillOval((int)(scaled.scaledPosition.x - scaled.scaledSize.x / 1.5), (int)(scaled.scaledPosition.y - scaled.scaledSize.y / 1.5), scaled.scaledSize.x, scaled.scaledSize.y);
	}
}