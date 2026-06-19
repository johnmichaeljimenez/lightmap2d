using SkiaSharp;
using System.Numerics;

namespace LightMap2D.Core;

public class LightmapData
{
	public SKColor AmbientColor { get; set; } = SKColors.DarkGray;
	public int MapSize { get; set; } = 256;
	public int OutputSize { get; set; } = 512;
	public bool GlobalIlluminationOnly { get; set; } = false;

	public float GiDownscale { get; set; } = 0.3f;
	public float GiBlur { get; set; } = 16.0f;
	public float GiIntensity { get; set; } = 0.8f;
	public SKColor GiTint { get; set; } = SKColors.White;
	public int GiPasses { get; set; } = 4;

	public List<Light> Lights { get; set; } = new();
	public List<Shadow> Shadows { get; set; } = new();
}

public class Shadow
{
	public Vector2 Position { get; set; }
	public Vector2 Size { get; set; }
	public float Rotation { get; set; }
}

public class Light
{
	public Vector2 Position { get; set; }
	public float Radius { get; set; }
	public SKColor Color { get; set; }
}