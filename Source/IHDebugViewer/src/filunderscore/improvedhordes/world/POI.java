package filunderscore.improvedhordes.world;

import java.awt.Color;
import java.awt.Graphics;

import filunderscore.improvedhordes.gui.IHRenderer;
import filunderscore.improvedhordes.gui.IHRenderer.ScaledVector;
import filunderscore.improvedhordes.util.Vector2i;

public class POI implements Drawable
{
	public Vector2i position;
	public Vector2i size;
	public float density;
	public int count;
	public float zoneDistanceAvg;
	public float avgWeight;
	
	public POI(Vector2i position, Vector2i size, float density, int count, float zoneDistanceAvg, float avgWeight)
	{
		this.position = position;
		this.size = size;
		this.density = density;
		this.count = count;
		this.zoneDistanceAvg = zoneDistanceAvg;
		this.avgWeight = avgWeight;
	}

	@Override
	public void draw(World world, IHRenderer renderer, Graphics g) 
	{
		ScaledVector scaled = renderer.rescale(this.position, this.size);
		
		g.setColor(Color.yellow);
		
		g.drawRect((int)(scaled.scaledPosition.x), (int)(scaled.scaledPosition.y - scaled.scaledSize.y), scaled.scaledSize.x, scaled.scaledSize.y);

		g.setFont(g.getFont().deriveFont(12.0f));
		//g.drawString("Density: " + this.density + " Count: " + this.count, scaled.scaledPosition.x, scaled.scaledPosition.y - 10);
		//g.drawString("ZoneDistAvg: " + this.zoneDistanceAvg + " AvgW: " + this.avgWeight, scaled.scaledPosition.x, scaled.scaledPosition.y - 30);
	}
}