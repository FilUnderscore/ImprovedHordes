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
		return worldToScreen(new Vector2i(blocks, blocks));
	}
	
	public Vector2i worldToScreen(Vector2i absoluteWorldPos)
	{
		int localX = (int)((absoluteWorldPos.x / (float)this.worldSize.x) * panelSize.x);
		int localY = panelSize.y - (int)((absoluteWorldPos.y / (float)this.worldSize.y) * panelSize.y);
		
		return new Vector2i(localX, localY);
	}
	
	private Vector3 worldToAbsoluteWorld(Vector3 worldPos)
	{
		int maxWW = (this.worldSize.x / 2);
		int maxWH = (this.worldSize.y / 2);
		
		worldPos.x += maxWW;
		worldPos.z += maxWH;
		
		worldPos.x = Math.max(0, worldPos.x);
		worldPos.z = Math.max(0, worldPos.z);
		
		return worldPos;
	}
	
	private Vector2i worldToScreen(Vector3 pos)
	{
		return worldToScreen(new Vector2i((int)pos.x, (int)pos.z));
	}
	
	public ScaledVector rescale(Vector2i pos, Vector2i markerSize)
	{
		return rescale(new Vector3(pos.x, 0, pos.y), markerSize);
	}
	
	public ScaledVector rescale(Vector3 pos, Vector2i markerSize)
	{
		pos = worldToAbsoluteWorld(pos.clone());
		Vector2i screenPos = worldToScreen(pos.clone());
		
		markerSize = markerSize.clone();
		return new ScaledVector(new Vector2i((int)pos.x, (int)pos.z), screenPos, new Vector2i(markerSize.x, markerSize.y));
	}
	
	public static class ScaledVector
	{
		private Vector2i worldPosition;
		
		public Vector2i scaledPosition;
		public Vector2i scaledSize;
		
		public ScaledVector(Vector2i worldPosition, Vector2i scaledPosition, Vector2i scaledSize)
		{
			this.worldPosition = worldPosition;
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