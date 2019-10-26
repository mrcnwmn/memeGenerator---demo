using AppKit;
using Foundation;

namespace MemeGenerator
{
    public partial class AppDelegate : NSApplicationDelegate
    { 
        public AppDelegate()
        {
        }

        [Export("applicationDidFinishLaunching:")]
        public override void DidFinishLaunching(NSNotification notification)
        {
            ImageCanvasController imageCanvasController = new ImageCanvasController();
            window.ContentViewController = imageCanvasController;
            window.TitleVisibility = NSWindowTitleVisibility.Hidden;
            window.MakeKeyAndOrderFront(null);
            statusToolbarItem.View = imageCanvasController.imageLabel;
        }

        [Export("applicationWillTerminate:")]
        public override void WillTerminate(NSNotification notification)
        {
         
        }
    }
}
