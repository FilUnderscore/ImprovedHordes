package filunderscore.improvedhordes.gui;

import java.awt.Color;
import java.awt.Graphics;

import javax.swing.JPanel;

import filunderscore.improvedhordes.util.Vector2i;
import filunderscore.improvedhordes.world.ImprovedHordesSimulation;
import filunderscore.improvedhordes.world.ImprovedHordesSimulation.ConnectionStatus;

public class ImprovedHordesPanel extends JPanel// implements MouseWheelListener
{
	private ImprovedHordesSimulation simulation;
	
    private Vector2i size;
    private IHRenderer renderer;
    
	public ImprovedHordesPanel(ImprovedHordesSimulation simulation, Vector2i panelSize)
	{
		super();
		
		this.simulation = simulation;
		this.renderer = new IHRenderer();
		this.setSize(panelSize);
		this.setFocusable(true);
	}
	
	public void setSize(Vector2i panelSize)
	{
		this.size = panelSize;
		this.setSize(this.size.x, this.size.y);
		
		if(this.simulation.getWorld() != null)
			this.renderer.setSizes(this.simulation.getWorld().GetWorldSize(), panelSize);
	}
	
	public void paintComponent(Graphics g)
	{
		super.paintComponent(g);
		
		if(simulation.getWorld() != null && simulation.status == ConnectionStatus.CONNECTED)
		{
			if(this.simulation.lock.tryLock())
			{
				simulation.getWorld().draw(this.renderer, g);
				
				this.simulation.lock.unlock();
			}
		}
		else
		{			
			g.setColor(Color.red);
			
			String message = "";
			
			switch(simulation.status)
			{
			case CONNECTED:
				message = "World has not been loaded yet.";
				break;
			case LOST_CONNECTION:
				message = "Connection with the server was lost.";
				break;
			case NOT_CONNECTED:
				message = "Not connected.";
				break;
			case RECONNECTING:
				message = "Reconnecting.";
				break;
			}
			
			int width = g.getFontMetrics().stringWidth(message);
			g.drawString(message, (size.x - width) / 2, size.y / 2);
		}

		this.repaint();
	}
}