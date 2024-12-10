using System;
using System.Linq;

namespace RPG.World.Generation
{
    public class NoiseGenerator
    {
        private readonly int[] permutation;
        private readonly Random random;

        public NoiseGenerator(int seed)
        {
            random = new Random(seed);
            // Create base permutation array
            permutation = new int[512];
            int[] p = [.. Enumerable.Range(0, 256)];

            // Fisher-Yates shuffle of base array
            for (int i = p.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (p[i], p[j]) = (p[j], p[i]);
            }

            // Duplicate the permutation array to avoid overflow
            for (int i = 0; i < 512; i++)
            {
                permutation[i] = p[i & 255];
            }
        }

        public float Generate2D(float x, float y, float scale)
        {
            float xs = x * scale;
            float ys = y * scale;

            int x0 = (int)Math.Floor(xs) & 255;
            int y0 = (int)Math.Floor(ys) & 255;

            float xf = xs - (float)Math.Floor(xs);
            float yf = ys - (float)Math.Floor(ys);

            float u = Fade(xf);
            float v = Fade(yf);

            int a = permutation[x0] + y0;
            int aa = permutation[a];
            int ab = permutation[a + 1];
            int b = permutation[x0 + 1] + y0;
            int ba = permutation[b];
            int bb = permutation[b + 1];

            float result = Lerp(v,
                Lerp(u, Grad(permutation[aa], xf, yf),
                        Grad(permutation[ba], xf - 1, yf)),
                Lerp(u, Grad(permutation[ab], xf, yf - 1),
                        Grad(permutation[bb], xf - 1, yf - 1)));

            return (result + 1) / 2; // Convert from [-1,1] to [0,1]
        }

        private static float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private static float Lerp(float t, float a, float b)
        {
            return a + t * (b - a);
        }

        private static float Grad(int hash, float x, float y)
        {
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 4 ? y : h == 12 || h == 14 ? x : 0;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }
    }
}