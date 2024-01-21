package filunderscore.improvedhordes.common.packet;

import java.io.DataInput;
import java.io.IOException;
import java.nio.charset.StandardCharsets;

import filunderscore.improvedhordes.util.Vector2i;
import filunderscore.improvedhordes.util.Vector3;

public final class PacketDataInput
{
	private final DataInput in;
	
	public PacketDataInput(DataInput in)
	{
		this.in = in;
	}
	
	public boolean readBoolean() throws IOException
	{
		return this.in.readBoolean();
	}

	public byte readByte() throws IOException
	{
		return this.in.readByte();
	}
	
	public float readFloat() throws IOException
	{
		return this.in.readFloat();
	}
	
	public int readInt() throws IOException
	{
		return this.in.readInt();
	}

	public byte[] readBytes() throws IOException
	{
		int length = this.readInt();
		byte[] bytes = new byte[length];
		
		this.in.readFully(bytes);
		
		return bytes;
	}
	
	public String readString() throws IOException
	{
		boolean valid = this.readBoolean();
		
		if(valid)
		{
			int length = this.readInt();
			byte[] b = new byte[length];
			
			for(int i = 0; i < length; i++)
			{
				b[i] = this.readByte();
			}
			
			return new String(b, StandardCharsets.UTF_8);
		}
		else
		{
			return "null";
		}
	}
	
	public Vector3 readVector3() throws IOException
	{
		return new Vector3(this.readFloat(), this.readFloat(), this.readFloat());
	}
	
	public Vector2i readVector2i() throws IOException
	{
		return new Vector2i(this.readInt(), this.readInt());
	}
	
	public interface ListCallback
	{
		void onReceived() throws IOException;
	}
	
	public void readList(ListCallback callback) throws IOException
	{
		int listSize = this.readInt();
		
		for(int i = 0; i < listSize; i++)
		{
			callback.onReceived();
		}
	}
}