package filunderscore.improvedhordes.util;

import java.io.DataInput;
import java.io.IOException;

public class Vector3 implements Cloneable
{
	public float x;
	public float y;
	public float z;
	
	public Vector3(float x, float y, float z)
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}
	
	public String toString()
	{
		return String.format("Vector3 [x: %.1f, y: %.1f, z: %.1f]", x, y, z);
	}
	
	public Vector3 clone()
	{
		return new Vector3(x, y, z);
	}
	
	public static Vector3 read(DataInput in) throws IOException
	{
		return new Vector3(in.readFloat(), in.readFloat(), in.readFloat());
	}
	
	public Vector2i toVXZ()
	{
		return new Vector2i(this.x, this.z);
	}
}