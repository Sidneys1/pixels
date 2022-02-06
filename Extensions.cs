#define TRACE

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using srt;

namespace pixels
{
    internal static class Extensions {
        public static void CopyToBitmap(this Backbuffer backbuffer, WriteableBitmap bmp, Int32Rect sourceRect) {
            bmp.Lock();
            bmp.WritePixels(sourceRect, backbuffer.Data, backbuffer.Stride, 0);
            bmp.Unlock();
            backbuffer.Dirty = false;
        }
    }
}
