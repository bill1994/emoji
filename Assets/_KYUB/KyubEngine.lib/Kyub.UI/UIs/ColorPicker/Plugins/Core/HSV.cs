using System;
using UnityEngine;

namespace Kyub.UI
{
	[Serializable]
	public struct HSV
	{
		[SerializeField]
		public float h;

		[SerializeField]
		public float s;

		[SerializeField]
		public float v;

		[SerializeField]
		public float a;

		public static readonly HSV red = new HSV(0f, 1f, 1f, 1f);

		public static readonly HSV green = new HSV(0.33f, 1f, 1f, 1f);

		public static readonly HSV blue = new HSV(0.66f, 1f, 1f, 1f);

		public static readonly HSV cyan = new HSV(0.5f, 1f, 1f, 1f);

		public static readonly HSV magenta = new HSV(0.83f, 1f, 1f, 1f);

		public static readonly HSV yellow = new HSV(0.16f, 1f, 1f, 1f);

		public static readonly HSV white = new HSV(0f, 0f, 1f, 1f);

		public static readonly HSV gray = new HSV(0f, 0.5f, 1f, 1f);

		public static readonly HSV grey = new HSV(0f, 0.5f, 1f, 1f);

		public static readonly HSV black = new HSV(0f, 0f, 0f, 1f);

		public static readonly HSV clear = new HSV(0f, 0f, 0f, 0f);

		public HSV(float H, float S, float V)
		{
			h = H;
			s = S;
			v = V;
			a = 1f;
		}

		public HSV(float H, float S, float V, float A)
		{
			h = H;
			s = S;
			v = V;
			a = A;
		}

		public static HSV ColorToHSV(Color color)
		{
			return _ColorToHSV(color);
		}

		private static HSV _ColorToHSV(Color color)
		{
			float r = color.r;
			float g = color.g;
			float b = color.b;
			float num = (r > g) ? r : g;
			num = ((num > b) ? num : b);
			float num2 = (r < g) ? r : g;
			num2 = ((num2 < b) ? num2 : b);
			float num3 = num - num2;
			if (num3 > 0f)
			{
				if (num != r)
				{
					num3 = ((num != g) ? (4f + (r - g) / num3) : (2f + (b - r) / num3));
				}
				else
				{
					num3 = (g - b) / num3;
					if (num3 < 0f)
					{
						num3 += 6f;
					}
				}
			}
			num3 /= 6f;
			float num4 = num - num2;
			if (num != 0f)
			{
				num4 /= num;
			}
			float num5 = num;
			return new HSV(num3, num4, num5, color.a);
		}

		public static Color HSVToColor(HSV hsv)
		{
			return _HSVToColor(hsv);
		}

		private static Color _HSVToColor(HSV hsv)
		{
			float num = hsv.h;
			float num2 = hsv.s;
			float num3 = hsv.v;
			float num4 = num3;
			float num5 = num3;
			float num6 = num3;
			if (num2 > 0f)
			{
				num *= 6f;
				int num7 = (int)num;
				float num8 = num - (float)num7;
				switch (num7)
				{
				default:
					num5 *= 1f - num2 * (1f - num8);
					num6 *= 1f - num2;
					break;
				case 1:
					num4 *= 1f - num2 * num8;
					num6 *= 1f - num2;
					break;
				case 2:
					num4 *= 1f - num2;
					num6 *= 1f - num2 * (1f - num8);
					break;
				case 3:
					num4 *= 1f - num2;
					num5 *= 1f - num2 * num8;
					break;
				case 4:
					num4 *= 1f - num2 * (1f - num8);
					num5 *= 1f - num2;
					break;
				case 5:
					num5 *= 1f - num2;
					num6 *= 1f - num2 * num8;
					break;
				}
			}
			return new Color(num4, num5, num6, hsv.a);
		}

		public override string ToString()
		{
			return $"({h},{s},{v},{a})";
		}

		public override bool Equals(object Obj)
		{
			if (Obj is HSV)
			{
				return Equals((HSV)Obj);
			}
			return false;
		}

		public bool Equals(HSV hsv)
		{
			if (h == hsv.h && s == hsv.s && v == hsv.v)
			{
				return a == hsv.a;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return h.GetHashCode() ^ s.GetHashCode() ^ v.GetHashCode() ^ a.GetHashCode();
		}

		public static bool operator !=(HSV a, HSV b)
		{
			return a.Equals(b);
		}

		public static bool operator ==(HSV a, HSV b)
		{
			return !a.Equals(b);
		}

		public static implicit operator HSV(Color c)
		{
			return ColorToHSV(c);
		}

		public static implicit operator Color(HSV h)
		{
			return HSVToColor(h);
		}
	}
}
