package filunderscore.improvedhordes.gui;

import filunderscore.improvedhordes.util.Vector2i;
import filunderscore.improvedhordes.util.Vector3;

public class IHRenderer 
{
	private Vector2i worldSize;
	private Vector2i panelSize;
	
	public IHRenderer()
	{
	}
	
	public void setSizes(Vector2i worldSize, Vector2i panelSize)
	{
		this.worldSize = worldSize;
		this.panelSize = panelSize;
	}
	
	public Vector2i rescaleBlocksToScreen(int blocks)
	{
		return worldToScreen(new Vector2i(blocks, blocks), false);
	}
	
	public Vector2i worldToScreen(Vector2i absoluteWorldPos, boolean flip)
	{
		int localX = (int)((absoluteWorldPos.x / (float)this.worldSize.x) * panelSize.x);
		int localY = (int)((absoluteWorldPos.y / (float)this.worldSize.y) * panelSize.y);
		
		if(flip)
			localY = panelSize.y - localY;
		
		return new Vector2i(localX, localY);
	}
	
	private Vector2i worldToAbsoluteWorld(Vector2i worldPos)
	{
		int maxWW = (this.worldSize.x / 2);
		int maxWH = (this.worldSize.y / 2);
		
		worldPos.x += maxWW;
		worldPos.y += maxWH;
		
		worldPos.x = Math.max(0, worldPos.x);
		worldPos.y = Math.max(0, worldPos.y);
		
		return worldPos;
	}
	
	private Vector2i worldToScreen(Vector3 pos)
	{
		return worldToScreen(new Vector2i((int)pos.x, (int)pos.z), true);
	}
	
	public ScaledVector rescale(Vector2i pos, Vector2i markerSize)
	{
		pos = worldToAbsoluteWorld(pos.clone());
		pos = worldToScreen(pos.clone(), true);
		
		markerSize = worldToScreen(markerSize.clone(), false);
		
		return new ScaledVector(pos, markerSize);
	}
	
	public static class ScaledVector
	{
		public Vector2i scaledPosition;
		public Vector2i scaledSize;
		
		public ScaledVector(Vector2i scaledPosition, Vector2i scaledSize)
		{
			this.scaledPosition = scaledPosition;
			this.scaledSize = scaledSize;
		}
		
		public boolean inView()
		{
			return true;
		}
	}
	
	private class Rect
	{
		private Vector2i bottomLeft, topRight;
		
		public Rect(Vector2i bottomLeft, Vector2i topRight)
		{
			this.bottomLeft = bottomLeft;
			this.topRight = topRight;
		}
		
		public boolean inRect(Vector2i pos)
		{
			return pos.x >= this.bottomLeft.x && pos.x <= this.topRight.x && pos.y >= this.bottomLeft.y && pos.y <= this.topRight.y;
		}
	}
	
	private Vector2i screenToWorld(Vector2i screenPos)
	{
		int localX = (int)(((float)screenPos.x / panelSize.x) * this.worldSize.x);
		int localY = (int)(((float)screenPos.y / panelSize.y) * this.worldSize.y);
		
		return new Vector2i(localX, this.worldSize.y - localY);
	}
	
	public Vector2i getPanelSize()
	{
		return this.panelSize;
	}
}