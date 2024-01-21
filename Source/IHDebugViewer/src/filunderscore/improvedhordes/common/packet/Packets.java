package filunderscore.improvedhordes.common.packet;

import java.lang.reflect.InvocationTargetException;
import java.util.HashMap;
import java.util.Map;

import org.reflections.Reflections;

import filunderscore.improvedhordes.Logger;

public final class Packets
{
	public static final Map<Short, Packet> INSTANCES = new HashMap<Short, Packet>();
	
	public static final short INIT = 0;
	
	// one time packets sent on login
	public static final short BIOMES = 1;
	public static final short ZONES = 2;
	public static final short HEAT = 3;

	// continuous packets sent
	public static final short PLAYERS = 4;
	public static final short CLUSTERS = 5;
	public static final short EVENT = 6;
	
	public static void register()
	{
		INSTANCES.clear();
		
		Reflections reflections = new Reflections("filunderscore.improvedhordes.common.packet");
		
		reflections.getSubTypesOf(Packet.class).forEach(packetClass ->
		{
			try 
			{
				Packet packet = (Packet)packetClass.getConstructor().newInstance(new Object[0]);
				INSTANCES.put(packet.getId(), packet);

				Logger.log("Loaded packet %s with ID %d", packet.getClass().getSimpleName(), packet.getId());
			}
			catch (	InstantiationException |
					IllegalAccessException |
					IllegalArgumentException |
					InvocationTargetException |
					NoSuchMethodException |
					SecurityException e)
			{
				e.printStackTrace();
			}
		});
	}
}