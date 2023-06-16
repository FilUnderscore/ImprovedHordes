package filunderscore.improvedhordes.util;

public class Vector2i implements Cloneable
{
	public int x;
	public int y;
	
	public Vector2i(int x, int y)
	{
		this.x = x;
		this.y = y;
	}
	
	public Vector2i(float x, float y)
	{
		this.x = (int)Math.round(x);
		this.y = (int)Math.round(y);
	}
	
	public String toString()
	{
		return String.format("Vector2i [x: %d, y: %d]", this.x, this.y);
	}
	
	public Vector2i clone()
	{
		return new Vector2i(x, y);
	}
}