using System.Numerics;
using SkiaSharp;

namespace LightMap2D.Core;

public class Lightmap
{
	public Lightmap()
	{

	}

	public void Generate(LightmapData data)
	{
		var scale = (float)data.OutputSize / data.MapSize;
		var info = new SKImageInfo(data.OutputSize, data.OutputSize,
								   SKColorType.Rgba8888, SKAlphaType.Premul);

		foreach (var i in data.Lights)
		{
			i.Position = Utils.Remap(i.Position, Vector2.Zero, data.MapSize * Vector2.One, Vector2.Zero, data.OutputSize * Vector2.One);
			i.Radius *= scale;
		}

		foreach (var i in data.Shadows)
		{
			i.Position = Utils.Remap(i.Position, Vector2.Zero, data.MapSize * Vector2.One, Vector2.Zero, data.OutputSize * Vector2.One);
			i.Size *= scale;
		}

		using (var mainSurface = SKSurface.Create(info))
		{
			var mainCanvas = mainSurface.Canvas;
			mainCanvas.Clear(SKColors.Black);

			foreach (var light in data.Lights)
			{
				using (var tempSurface = SKSurface.Create(info))
				{
					var tempCanvas = tempSurface.Canvas;
					tempCanvas.Clear(data.AmbientColor);

					using (var paint = new SKPaint())
					{
						var end = new SKColor(light.Color.Red, light.Color.Green, light.Color.Blue, 0);
						var center = new SKPoint(light.Position.X, light.Position.Y);

						float falloffPoint = 0.8f;
						var colorsWithFalloff = new SKColor[] { light.Color, end, end };
						var positions = new float[] { 0.0f, falloffPoint, 1.0f };

						paint.Shader = SKShader.CreateRadialGradient(
							center,
							light.Radius,
							colorsWithFalloff,
							positions,
							SKShaderTileMode.Clamp
						);

						tempCanvas.DrawCircle(light.Position.X, light.Position.Y, light.Radius, paint);
					}

					using (var shadowPaint = new SKPaint())
					{
						shadowPaint.Color = SKColors.Black;

						foreach (var shadow in data.Shadows)
						{
							List<Vector2> corners = Utils.GenerateRotatedRectangle(
								shadow.Position, shadow.Size, shadow.Rotation);

							SKPath shadowPath = BuildShadowPolygon(light.Position, corners,
								data.OutputSize, data.OutputSize);

							if (shadowPath != null)
								tempCanvas.DrawPath(shadowPath, shadowPaint);
						}
					}

					using (var addPaint = new SKPaint())
					{
						addPaint.BlendMode = SKBlendMode.Screen;
						mainCanvas.DrawImage(tempSurface.Snapshot(), 0, 0, addPaint);
					}
				}
			}

			using (var directImage = mainSurface.Snapshot())
			{
				mainCanvas.Clear(SKColors.Black);

				float directBlur = 5.0f;
				using (var directPaint = new SKPaint())
				{
					directPaint.ImageFilter = SKImageFilter.CreateBlur(directBlur, directBlur, SKShaderTileMode.Clamp);
					mainCanvas.DrawImage(directImage, 0, 0, directPaint);
				}

				const float downscale = 0.25f;
				int smallSize = (int)(data.OutputSize * downscale);

				var smallInfo = new SKImageInfo(smallSize, smallSize,
												SKColorType.Rgba8888, SKAlphaType.Premul);

				using (var smallSurface = SKSurface.Create(smallInfo))
				{
					var smallCanvas = smallSurface.Canvas;

					smallCanvas.DrawImage(directImage,
						new SKRect(0, 0, data.OutputSize, data.OutputSize),
						new SKRect(0, 0, smallSize, smallSize));

					float giBlur = 28.0f;
					using (var blurPaint = new SKPaint())
					{
						blurPaint.ImageFilter = SKImageFilter.CreateBlur(giBlur, giBlur, SKShaderTileMode.Clamp);
						smallCanvas.DrawImage(smallSurface.Snapshot(), 0, 0, blurPaint);
					}

					using (var upscaledGi = smallSurface.Snapshot())
					using (var giPaint = new SKPaint())
					{
						giPaint.BlendMode = SKBlendMode.Screen;
						giPaint.Color = new SKColor(255, 255, 255, 120);

						mainCanvas.DrawImage(upscaledGi,
							new SKRect(0, 0, smallSize, smallSize),
							new SKRect(0, 0, data.OutputSize, data.OutputSize),
							giPaint);
					}
				}
			}

			using (var litImage = mainSurface.Snapshot())
			{
				mainCanvas.Clear(SKColors.Black);

				mainCanvas.DrawImage(litImage, 0, 0);

				using (var shadowPaint = new SKPaint())
				{
					shadowPaint.Color = SKColors.Black;
					shadowPaint.IsAntialias = true;
					shadowPaint.Style = SKPaintStyle.Fill;

					foreach (var shadow in data.Shadows)
					{
						List<Vector2> corners = Utils.GenerateRotatedRectangle(
							shadow.Position, shadow.Size, shadow.Rotation);

						using (var path = new SKPath())
						{
							path.MoveTo(corners[0].X, corners[0].Y);
							for (int i = 1; i < corners.Count; i++)
								path.LineTo(corners[i].X, corners[i].Y);
							path.Close();

							mainCanvas.DrawPath(path, shadowPaint);
						}
					}
				}
			}

			float finalSoftness = 2.0f;
			if (finalSoftness > 0)
			{
				using (var raw = mainSurface.Snapshot())
				using (var blurPaint = new SKPaint())
				{
					blurPaint.ImageFilter = SKImageFilter.CreateBlur(finalSoftness, finalSoftness, SKShaderTileMode.Clamp);
					mainCanvas.Clear(SKColors.Black);
					mainCanvas.DrawImage(raw, 0, 0, blurPaint);
				}
			}

			using (var image = mainSurface.Snapshot())
			using (var imgData = image.Encode(SKEncodedImageFormat.Png, 100))
			using (var stream = File.OpenWrite("test.png"))
			{
				imgData.SaveTo(stream);
			}
		}
	}

	private SKPath BuildShadowPolygon(Vector2 light, List<Vector2> corners,
									  float mapWidth, float mapHeight)
	{
		int n = corners.Count;

		var angleCorner = new (float Angle, Vector2 Point)[n];
		for (int i = 0; i < n; i++)
		{
			Vector2 dir = corners[i] - light;
			float angle = MathF.Atan2(dir.Y, dir.X);
			if (angle < 0) angle += MathF.PI * 2;
			angleCorner[i] = (angle, corners[i]);
		}

		Array.Sort(angleCorner, (a, b) => a.Angle.CompareTo(b.Angle));

		float maxGap = 0;
		int gapStart = 0;
		for (int i = 0; i < n; i++)
		{
			float nextAngle = angleCorner[(i + 1) % n].Angle;
			if (i == n - 1) nextAngle += MathF.PI * 2;
			float gap = nextAngle - angleCorner[i].Angle;
			if (gap > maxGap)
			{
				maxGap = gap;
				gapStart = i;
			}
		}
		int idxA = gapStart;
		int idxB = (gapStart + 1) % n;
		Vector2 tangentA = angleCorner[idxA].Point;
		Vector2 tangentB = angleCorner[idxB].Point;


		int origIdxA = corners.IndexOf(tangentA);
		int origIdxB = corners.IndexOf(tangentB);


		var backSequence = new List<Vector2>();
		int current = origIdxA;
		while (true)
		{
			backSequence.Add(corners[current]);
			if (current == origIdxB) break;
			current = (current + 1) % n;
		}

		float farDist = MathF.Sqrt(mapWidth * mapWidth + mapHeight * mapHeight) * 2f;

		Vector2 farB = light + Vector2.Normalize(tangentB - light) * farDist;
		Vector2 farA = light + Vector2.Normalize(tangentA - light) * farDist;

		var path = new SKPath();
		path.MoveTo(backSequence[0].X, backSequence[0].Y);
		for (int i = 1; i < backSequence.Count; i++)
			path.LineTo(backSequence[i].X, backSequence[i].Y);

		path.LineTo(farB.X, farB.Y);
		path.LineTo(farA.X, farA.Y);
		path.Close();

		return path;
	}
}
