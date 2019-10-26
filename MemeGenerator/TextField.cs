using AppKit;
using CoreGraphics;
using Foundation;

namespace MemeGenerator
{
    /// Self-centering text field
    public class TextField : NSTextField
    {
        private static readonly float horizontalPadding = 16.0f;
        private NSFont defaultFont = NSFont.BoldSystemFontOfSize(36);
        private NSColor defaultTextColor = NSColor.White;

        public TextField() : base(new CGRect(0, 0, horizontalPadding, 44))
        { 
            Font = defaultFont;
            Alignment       = NSTextAlignment.Center;
            TextColor       = defaultTextColor;
            BackgroundColor = NSColor.Clear;
            DrawsBackground = false;
            Bordered        = false;
            AutoresizingMask =  NSViewResizingMask.MinXMargin |
                                NSViewResizingMask.MaxXMargin |
                                NSViewResizingMask.MinYMargin |
                                NSViewResizingMask.MaxYMargin;
            isSelected      = false;
        }

        public TextField(NSCoder coder)
        {
            throw new ModelNotImplementedException();
        }

        public struct DrawingItem
        {
            public string text;
            public NSFont font;
            public NSColor color;
            public CGPoint origin;

            public void draw()
            {
                if(font != null && color != null)
                    text.DrawAtPoint(origin, new NSDictionary(font, color));
            }
        }

        public bool isSelected {
            get { return isSelected; }
            set
            {
                isSelected = value;
                if(Layer != null)
                {
                    Layer.BorderColor = (isSelected) ? NSColor.SecondarySelectedControl.CGColor : null;
                    Layer.BorderWidth = (isSelected) ? 1.0f : 0.0f;
                }
            }
        }

        public DrawingItem drawingItem()
        {
            NSFont itemFont     = Font ?? defaultFont;
            NSColor itemColor   = TextColor ?? defaultTextColor;
            CGPoint origin      = Frame.Location; // TODO: Not sure if this is correct. was "origin"
            origin.X            += horizontalPadding * 0.5f;

            return new DrawingItem {
                text            = StringValue,
                font            = itemFont,
                color           = itemColor,
                origin          = origin
            };
        }

        /// centers the view if it has a superview
        public void centerInSuperview()
        {
            NSView superview = Superview;
            CGRect centeredFrame = Frame;
            centeredFrame.X = 0.5f * (superview.Bounds.Width - centeredFrame.Width);
            centeredFrame.Y = 0.5f * (superview.Bounds.Height - centeredFrame.Height);
            Frame = BackingAlignedRect(centeredFrame, NSAlignmentOptions.AllEdgesNearest);
        }

        /// changes the width keeping the center point fixed
        void setFrameWidth(float width)
        {
            Frame = BackingAlignedRect(Frame.Inset(((Frame.Width - width) * 0.5f), 0f), NSAlignmentOptions.AllEdgesNearest);
        }

        public bool makeFirstResponder()
        {
            return (Window != null) && Window.MakeFirstResponder(this);
        }

        #region NSTextField

        [Export("textDidChange:")]
        public override void DidChange(NSNotification notification)
        {
            NSTextView editor = Window?.FieldEditor(false, this) as NSTextView;
            if (editor != null)
            {
                System.nfloat? newWidth = editor.TextStorage?.Size.Width;
                if(newWidth != null)
                    setFrameWidth((float)newWidth + horizontalPadding);
            }
        }
        #endregion
    }
}
