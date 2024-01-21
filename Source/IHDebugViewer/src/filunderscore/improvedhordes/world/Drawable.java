package filunderscore.improvedhordes.world;

import java.awt.Graphics;

import filunderscore.improvedhordes.gui.IHRenderer;

public interface Drawable 
{
	void draw(World world, IHRenderer renderer, Graphics g);
}