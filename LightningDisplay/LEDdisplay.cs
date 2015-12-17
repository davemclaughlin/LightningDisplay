using System;
using Microsoft.SPOT;
using GHI.Glide;
using GHI.Glide.Display;
using GHI.Glide.UI;

namespace Axon.LEDDisplay
{
    /// <summary>
    /// Create a seven segment display in the Glide image supplied
    /// Original Code:  Dave McLaughlin, Dec 2015
    /// Company:        Axon Instruments, Jakarta, Indonesia
    /// 
    /// Free to use in non-commercial and commercial code with the exception below for commercial use.
    /// 
    /// For commercial use, please note use of the code in any manuals or documents. No other restrictions on use.
    /// 
    /// </summary>
    class LEDdisplay
    {
        private Image destinationImage;
        private Bitmap ledDigitsBitmap;     // Array of image bitmaps
        private Bitmap ledDecimalBitmap;    // Decimal point. Must be same height as digits but can be slimmer
        private int numofDigits;            // Number of digits (before decimal point if want to use floating point)
        private int decimalPlaces;          // Number of decimal places for floating point. USE 0 for integer display
        private int ledWidth;               // Calculated width of the LED digits
        private int ledHeight;              
        private int decWidth;               // Width of the LED decimal point
        private String format;              // String format for the number

        /// <summary>
        /// Create an LED display using your own LED bitmaps
        /// </summary>
        /// <param name="destination">Image to draw the LED on. Size this to suit. Must be correct or creates error</param>
        /// <param name="ledDigits">Sequential bitmap of LED digits 0 to 9 and blank</param>
        /// <param name="ledDecimal">Bitmap for the decimal point. Must be same height as digits but can be narrower. 1/4 is good size to use</param>
        /// <param name="numOfDigits">Number of digits (before decimal point if decimal places)</param>
        /// <param name="decimalPlaces">Number of decimal places otherwise ZERO for integer value</param>
        /// 
        public LEDdisplay(Image destination, Bitmap ledDigits, Bitmap ledDecimal, int numOfDigits, int decimalPlaces)
        {
            int width;

            destinationImage = destination;
            ledDigitsBitmap = ledDigits;
            ledDecimalBitmap = ledDecimal;
            this.numofDigits = numOfDigits;
            this.decimalPlaces = decimalPlaces;

            format = "F" + this.decimalPlaces.ToString();

            ledWidth = ledDigitsBitmap.Width / 12;
            ledHeight = ledDigitsBitmap.Height;
            decWidth = ledDecimalBitmap.Width;
            //
            // Check if the width and height of the image are suitable
            //
            if(this.decimalPlaces > 0)  // Floating point?
            {
                width = (this.numofDigits * ledWidth) + (this.decimalPlaces * ledWidth) + decWidth;

                if(destinationImage.Width < width)
                {
                    throw new NotSupportedException("Destination is too narrow. Must be " + destinationImage.Width.ToString() + " and is " + width.ToString());
                }
            }
            else
            {
                width = (this.numofDigits * ledWidth);

                if (destinationImage.Width < width)
                {
                    throw new NotSupportedException("Destination is too narrow. Must be " + destinationImage.Width.ToString() + " and is " + width.ToString());
                }
            }
            if (destinationImage.Height < ledHeight)
            {
                throw new NotSupportedException("Destination height too little. Must be " + destinationImage.Height.ToString());
            }
            if (destinationImage.Width < (this.numofDigits * ledWidth))
            InitDisplay();
        }

        /// <summary>
        /// Initialises the display - internal function
        /// </summary>
        /// 
        private void InitDisplay()
        {
            if(decimalPlaces > 0)
            {
                DrawLED(0.0);
            }
            else
            {
                DrawLED(0);
            }
        }

        /// <summary>
        /// Draw an integer or floating point number in decimal places
        /// Creating with decimal places as ZERO creates an integer display
        /// </summary>
        /// <param name="value">Value to display</param>
        /// 
        public void DrawLED(double value)
        {
            String text = value.ToString(format);
            int start = (numofDigits + decimalPlaces + (decimalPlaces > 0 ? 1 : 0)) - text.Length;
            int pos = (start * ledWidth);

            for (int index = 0; index < text.Length; index++)
            {
                if (text.Substring(index, 1).Equals("."))
                {
                    DrawDigit(pos, ".");

                    pos += decWidth;
                }
                else
                {
                    DrawDigit(pos, text.Substring(index, 1));

                    pos += ledWidth;
                }
            }
            //
            // Now draw the leading blanks
            //
            pos = 0;
            for (int index = 0; index < start; index++)
            {
                DrawDigit(pos, " ");

                pos += ledWidth;
            }
            destinationImage.Invalidate();
        }

        /// <summary>
        /// Draws the digit on the image
        /// </summary>
        /// <param name="index">Start point to draw the digit</param>
        /// <param name="digit">Which digit to draw</param>
        /// 
        void DrawDigit(int index, String digit)
        {
            switch (digit)
            {
                case "0":
                    destinationImage.Bitmap.DrawImage(index, 0, ledDigitsBitmap, 0, 0, ledWidth, ledHeight);
                    break;
                case "1":
                    destinationImage.Bitmap.DrawImage(index, 0, ledDigitsBitmap, 1 * ledWidth, 0, ledWidth, ledHeight);
                    break;
                case "2":
                    destinationImage.Bitmap.DrawImage(index, 0, ledDigitsBitmap, 2 * ledWidth, 0, ledWidth, ledHeight);
                    break;
                case "3":
                    destinationImage.Bitmap.DrawImage(index, 0, ledDigitsBitmap, 3 * ledWidth, 0, ledWidth, ledHeight);
                    break;
                case "4":
                    destinationImage.Bitmap.DrawImage(index, 0, ledDigitsBitmap, 4 * ledWidth, 0, ledWidth, ledHeight);
                    break;
                case "5":
                    destinationImage.Bitmap.DrawImage(index, 0, ledDigitsBitmap, 5 * ledWidth, 0, ledWidth, ledHeight);
                    break;
                case "6":
                    destinationImage.Bitmap.DrawImage(index, 0, ledDigitsBitmap, 6 * ledWidth, 0, ledWidth, ledHeight);
                    break;
                case "7":
                    destinationImage.Bitmap.DrawImage(index, 0, ledDigitsBitmap, 7 * ledWidth, 0, ledWidth, ledHeight);
                    break;
                case "8":
                    destinationImage.Bitmap.DrawImage(index, 0, ledDigitsBitmap, 8 * ledWidth, 0, ledWidth, ledHeight);
                    break;
                case "9":
                    destinationImage.Bitmap.DrawImage(index, 0, ledDigitsBitmap, 9 * ledWidth, 0, ledWidth, ledHeight);
                    break;
                case " ":
                    destinationImage.Bitmap.DrawImage(index, 0, ledDigitsBitmap, 10 * ledWidth, 0, ledWidth, ledHeight);
                    break;
                case "-":
                    destinationImage.Bitmap.DrawImage(index, 0, ledDigitsBitmap, 11 * ledWidth, 0, ledWidth, ledHeight);
                    break;
                case ".":
                    destinationImage.Bitmap.DrawImage(index, 0, ledDecimalBitmap, 0, 0, ledWidth, ledHeight);
                    break;
                default: // Use BLANK for all other values in the string
                    destinationImage.Bitmap.DrawImage(index, 0, ledDigitsBitmap, 10 * ledWidth, 0, ledWidth, ledHeight);
                    break;
            }
        }

    }

}
