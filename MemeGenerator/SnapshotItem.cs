using System;
using System.Collections.Generic;
using AppKit;
using CoreGraphics;
using Foundation;

namespace MemeGenerator
{
    /// Used to represent the content of the canvas and render a flattened image
    public class SnapshotItem : NSObject
    {
        private readonly NSImage BaseImage;
        private readonly List<TextField.DrawingItem> DrawingItems = new List<TextField.DrawingItem>();
        private CGSize PixelSize;
        private nfloat DrawingScale;

        public SnapshotItem(NSImage image, List<TextField> textFields, CGSize pixelSize, nfloat drawingScale)
        {
            BaseImage           = image;
            this.PixelSize      = pixelSize;
            this.DrawingScale   = drawingScale;
            foreach(TextField text in textFields)
                DrawingItems.Add(text.drawingItem());
        }

        private bool DrawingHandler(CGRect dstRect)
        {
            BaseImage.Draw(dstRect);
            NSAffineTransform transform = new NSAffineTransform();
            transform.Scale(DrawingScale);
            transform.Concat();
            foreach(TextField.DrawingItem item in DrawingItems)
                item.Draw();
            return true;
        }

        public NSData JpegRepresentation
        {
            get
            {
                NSImage outputImage             = NSImage.ImageWithSize(PixelSize, false, DrawingHandler);
                NSData tiffData                 = outputImage.AsTiff();
                NSBitmapImageRep bitmapImageRep = new NSBitmapImageRep(tiffData);
                return bitmapImageRep.RepresentationUsingTypeProperties(NSBitmapImageFileType.Jpeg, new NSDictionary());
            }
        }
    }
}
