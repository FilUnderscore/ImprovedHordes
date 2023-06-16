package filunderscore.improvedhordes.util;

public class Vector2 implements Cloneable
{
	public float x, y;
	
	public Vector2(float x, float y)
	{
		this.x = x;
		this.y = y;
	}
	
	public Vector2 clone()
	{
		return new Vector2(x, y);
	}
	
	public String toString()
	{
		return String.format("Vector2 [x: %.1f, y: %.1f]", this.x, this.y);
	}
}