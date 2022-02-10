#define TRACE

using System.Windows.Media;

namespace srt
{
    internal static class Extensions {
        /// <summary>
        /// Linear interpolation of two values.
        /// </summary>
        /// <param name="first">Value represented by 0.0</param>
        /// <param name="second">Value represented by 1.0</param>
        /// <param name="by">Interpolation value between 0.0 and 1.0</param>
        /// <returns>A new value.</returns>
        static double Lerp(double first, double second, double by) => first * (1 - by) + second * by;

        /// <summary>
        /// Linear interpolation of two colors.
        /// </summary>
        /// <param name="first">Color represented by 0.0</param>
        /// <param name="second">Color represented by 1.0</param>
        /// <param name="by">Interpolation value between 0.0 and 1.0</param>
        /// <returns>A new color.</returns>
        public static Color Lerp(this Color first, Color second, double by, bool boundsCheck = true) {
            if (boundsCheck) {
                return by switch {
                    <= 0 => first,
                    >= 1 => second,
                    _ => Color.FromRgb(
                        (byte)Lerp(first.R, second.R, by),
                        (byte)Lerp(first.G, second.G, by),
                        (byte)Lerp(first.B, second.B, by)),
                };
            }

            return Color.FromRgb(
                        (byte)Lerp(first.R, second.R, by),
                        (byte)Lerp(first.G, second.G, by),
                        (byte)Lerp(first.B, second.B, by));
        }
    }
}
