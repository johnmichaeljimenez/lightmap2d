using SkiaSharp;
using System.Numerics;

public class LightmapData
{
	public SKColor AmbientColor = SKColors.DarkGray;
	public int MapSize = 256;
	public int OutputSize = 512;
	public List<Light> Lights;
	public List<Shadow> Shadows;
}

public class Shadow
{
	public Vector2 Position;
	public Vector2 Size;
	public float Rotation;
}

public class Light
{
	public Vector2 Position;
	public float Radius;
	public SKColor Color;
}