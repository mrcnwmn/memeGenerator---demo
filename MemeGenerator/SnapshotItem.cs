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
        private CGSize PixelSize;
        private TextField.DrawingItem DrawingItems;
        private nfloat DrawingScale;

        public SnapshotItem(NSImage image, List<TextField> textFields, CGSize pixelSize, nfloat drawingScale)
        {
            BaseImage           = image;
            this.PixelSize      = pixelSize;
            this.DrawingScale   = drawingScale;
            DrawingItems        = (textFields.Count > 0) ? textFields[0].drawingItem() : new TextField.DrawingItem();
        }

        private bool DrawingHandler(CGRect dstRect)
        {
            BaseImage.Draw(dstRect);
            NSAffineTransform transform = new NSAffineTransform();
            transform.Scale(this.DrawingScale);
            transform.Concat();
            DrawingItems.draw();
            return true;
        }

        public NSData JpegRepresentation
        {
            get
            {
                NSImage outputImage = NSImage.ImageWithSize(PixelSize, false, DrawingHandler);
                NSData tiffData = outputImage.AsTiff();
                NSBitmapImageRep bitmapImageRep = new NSBitmapImageRep(tiffData);
                return bitmapImageRep.RepresentationUsingTypeProperties(NSBitmapImageFileType.Jpeg, new NSDictionary());
            }
        }
    }
}
