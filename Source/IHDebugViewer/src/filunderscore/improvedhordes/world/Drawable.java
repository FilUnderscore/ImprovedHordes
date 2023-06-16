package filunderscore.improvedhordes.world;

import java.awt.Graphics;

import filunderscore.improvedhordes.gui.IHRenderer;

public interface Drawable 
{
	void draw(WorldHordeState world, IHRenderer renderer, Graphics g);
}