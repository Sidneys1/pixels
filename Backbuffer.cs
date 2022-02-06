#define TRACE

using System;
using System.Windows.Media;

namespace srt
{
    public class Backbuffer {
        public readonly int Width, Height, BitsPerPixel, Stride;

        public byte[] Data;

        public bool Dirty {get;set;} = false;

        public Backbuffer(int width, int height) {
            Width = width;
            Height = height;
            BitsPerPixel = 3;
            Stride = BitsPerPixel * width;

            Data = new byte[Stride * height];
        }

        public void SetPixel(int x, int y, byte r, byte g, byte b) {
            Dirty = true;
            var off = (y * Stride) + (x * 3);
            lock (Data) {
                Data[off] = r;
                Data[off + 1] = g;
                Data[off + 2] = b;
            }
        }

        public void SetPixel(int x, int y, Color color) {
            Dirty = true;
            var off = (y * Stride) + (x * 3);
            lock (Data) {
                Data[off] = color.R;
                Data[off + 1] = color.G;
                Data[off + 2] = color.B;
            }
        }

        public void DrawLine(int sx, int sy, int ex, int ey, Color color) {
            sx = Math.Clamp(sx, 0, Width);
            ex = Math.Clamp(ex, 0, Width);
            sy = Math.Clamp(sy, 0, Height);
            ey = Math.Clamp(ey, 0, Height);
            float step = 0.01f;
            for (float i = 0; i < 1; i += step)
            {
                float ax = sx + ((float)(ex - sx) * i);
                float ay = sy + ((float)(ey - sy) * i);
                SetPixel((int)ax, (int)ay, color.R, color.G, color.B);
            }
        }
    }
}
