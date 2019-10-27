using System;
using AppKit;
using CoreGraphics;
using Foundation;

namespace MemeGenerator
{
    /// Used to represent the content of the canvas and render a flattened image
    public class SnapshotItem : NSObject
    {
        private readonly NSImage baseImage;
        private CGSize pixelSize;
        private TextField.DrawingItem drawingItems;
        private nfloat drawingScale;

        public SnapshotItem(NSImage baseImage, CGSize pixelSize, TextField.DrawingItem drawingItems, nfloat drawingScale)
        {
            this.baseImage = baseImage;
            this.pixelSize = pixelSize;
            this.drawingItems = drawingItems;
            this.drawingScale = drawingScale;
        }

        private bool DrawingHandler(CGRect dstRect)
        {
            baseImage.Draw(dstRect);
            NSAffineTransform transform = new NSAffineTransform();
            transform.Scale(this.drawingScale);
            transform.Concat();
            drawingItems.draw();
            return true;
        }

        public NSData JpegRepresentation
        {
            get
            {
                NSImage outputImage = NSImage.ImageWithSize(pixelSize, false, DrawingHandler);
                NSData tiffData = outputImage.AsTiff();
                NSBitmapImageRep bitmapImageRep = new NSBitmapImageRep(tiffData);
                return bitmapImageRep.RepresentationUsingTypeProperties(NSBitmapImageFileType.Jpeg, new NSDictionary());
            }
        }
    }
}
