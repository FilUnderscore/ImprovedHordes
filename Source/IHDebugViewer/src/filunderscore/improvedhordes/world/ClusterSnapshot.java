package filunderscore.improvedhordes.world;

import java.awt.Color;
import java.awt.Graphics;

import filunderscore.improvedhordes.gui.IHRenderer;
import filunderscore.improvedhordes.gui.IHRenderer.ScaledVector;
import filunderscore.improvedhordes.util.Vector2i;
import filunderscore.improvedhordes.util.Vector3;

public class ClusterSnapshot implements Drawable
{
	public String clusterType;
	public Vector3 location;
	public float density;
	
	public ClusterSnapshot(String clusterType, Vector3 location, float density)
	{
		this.clusterType = clusterType;
		this.location = location;
		this.density = density;
	}
	
	public String toString()
	{
		return String.format("Cluster [type: %s, location: %s, density: %f]", this.clusterType, this.location, this.density);
	}

	@Override
	public void draw(WorldHordeState world, IHRenderer renderer, Graphics g) 
	{
		ScaledVector rescaledLocation = renderer.rescale(this.location.toVXZ(), new Vector2i(30, 30));
		
		if(this.clusterType.equalsIgnoreCase("WanderingAnimalHorde"))
			g.setColor(Color.blue);
		else if(this.clusterType.equalsIgnoreCase("WanderingEnemyHorde"))
			g.setColor(Color.red);
		else if(this.clusterType.equalsIgnoreCase("ScreamerHorde"))
			g.setColor(Color.green);
		else if(this.clusterType.equalsIgnoreCase("WanderingAnimalEnemyHorde"))
			g.setColor(Color.orange);
		
		if(rescaledLocation.inView())
		{
			g.fillOval((int)(rescaledLocation.scaledPosition.x - rescaledLocation.scaledSize.x / 1.5), (int)(rescaledLocation.scaledPosition.y - rescaledLocation.scaledSize.y / 1.5), rescaledLocation.scaledSize.x, rescaledLocation.scaledSize.y);
			g.drawString(Float.toString(this.density), rescaledLocation.scaledPosition.x - rescaledLocation.scaledSize.x, rescaledLocation.scaledPosition.y - rescaledLocation.scaledSize.y);
		}
	}
}