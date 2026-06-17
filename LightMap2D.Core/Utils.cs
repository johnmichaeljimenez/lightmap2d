using System.Numerics;

public static class Utils
{
	public static float Remap(float current, float min1, float max1, float min2, float max2)
	{
		if (max1 == min1)
			return min2;

		float t = (current - min1) / (max1 - min1);
		return min2 + t * (max2 - min2);
	}

	public static Vector2 Remap(Vector2 current, Vector2 min1, Vector2 max1, Vector2 min2, Vector2 max2)
	{
		float x = Remap(current.X, min1.X, max1.X, min2.X, max2.X);
		float y = Remap(current.Y, min1.Y, max1.Y, min2.Y, max2.Y);
		return new Vector2(x, y);
	}

	public static Vector2 RotateAround(this Vector2 input, Vector2 origin, float angleDegrees)
	{
		var angleRadians = angleDegrees * (MathF.PI / 180f);
		float cos = MathF.Cos(angleRadians);
		float sin = MathF.Sin(angleRadians);

		float dx = input.X - origin.X;
		float dy = input.Y - origin.Y;

		return new Vector2(
			(dx * cos) - (dy * sin) + origin.X,
			(dx * sin) + (dy * cos) + origin.Y
		);
	}

	public static List<Vector2> GenerateRotatedRectangle(Vector2 center, Vector2 size, float rotationDegrees)
	{
		float halfWidth = size.X / 2f;
		float halfHeight = size.Y / 2f;

		List<Vector2> corners = new List<Vector2>(4)
		{
			new Vector2(-halfWidth, -halfHeight),
			new Vector2(halfWidth, -halfHeight),
			new Vector2(halfWidth, halfHeight),
			new Vector2(-halfWidth, halfHeight)
		};

		for (int i = 0; i < corners.Count; i++)
		{
			corners[i] = corners[i].RotateAround(Vector2.Zero, rotationDegrees) + center;
		}

		return corners;
	}
}