package filunderscore.improvedhordes;

public final class Logger
{
	public static void log(String format, Object...args)
	{
		System.out.println(String.format(format, args));
	}
	
	public static void error(String format, Object...args)
	{
		System.err.println(String.format(format, args));
	}
}