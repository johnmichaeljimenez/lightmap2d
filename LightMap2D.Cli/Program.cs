using System.Diagnostics;
using System.Numerics;
using LightMap2D.Core;
using SkiaSharp;

var l = new Lightmap();
var data = new LightmapData()
{
	AmbientColor = SKColors.Black,
	MapSize = 12,
	GlobalIlluminationOnly = true,
	OutputSize = 512,
	Lights = new(){
		new(){
			Position = Vector2.One * 7,
			Color = SKColors.Lime,
			Radius = 8
		},
		new(){
			Position = Vector2.One * 4,
			Color = SKColors.Red,
			Radius = 8
		}
	},

	Shadows = new()
	{
		new(){
			Position = new(8, 3),
			Size = new(1, 1),
			Rotation = 35
		},
		new(){
			Position = new(5, 5),
			Size = new(1, 1)
		},
		new(){
			Position = new(5, 8),
			Size = new(3, 1)
		}
	}
};

var st = new Stopwatch();
st.Start();
l.Generate(data);
st.Stop();

Console.WriteLine($"Done at {st.ElapsedMilliseconds}ms");